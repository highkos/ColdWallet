using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ColdWallet.AccountBalances;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Text.RegularExpressions;

namespace UniversalColdWallet
{
    public class AccountBalance
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _apiKeys;
        private readonly USDT_BSCCAccountBalance _usdtBSCBalance;
        private readonly USDT_TRC20AccountBalance _usdtTrcBalance;
        private readonly TRX_TRC20AccountBalance _trxTrcBalance;
        private readonly ILogger<USDT_BSCCAccountBalance>? _logger;
        private const int MaxRetries = 3;

        // Updated Ethereum RPC endpoints as requested
        private readonly List<string> _ethRpcEndpoints = new List<string>
        {
            "https://ethereum.publicnode.com",
            "https://eth.llamarpc.com",
            "https://ethereum.blockpi.network/v1/rpc/public"
        };
        private int _currentEthEndpointIndex = 0;

        public AccountBalance(ILogger<USDT_BSCCAccountBalance>? logger = null)
        {
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = logger ?? factory.CreateLogger<USDT_BSCCAccountBalance>();
            _httpClient = new HttpClient();
            
            // Initialize API keys
            _apiKeys = new Dictionary<string, string>
            {
                { "BSCSCAN", "8IBNZTXZWPTR6V4S4AKJWA5BAMPXZU4IHI" },
                { "BLOCKCYPHER", "" },
                { "TRONGRID", "0df1dc46-e032-43ff-8a15-c9e732b4afad" }
            };

            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Initialize USDT balance checkers with logger
            _usdtBSCBalance = new USDT_BSCCAccountBalance(_httpClient, _apiKeys["BSCSCAN"], _logger);
            _usdtTrcBalance = new USDT_TRC20AccountBalance(_httpClient);
            _trxTrcBalance = new TRX_TRC20AccountBalance(_httpClient);
        }

        public async Task<decimal> GetBalanceAsync(string coinSymbol, string address)
        {
            try
            {
                switch (coinSymbol.ToUpper())
                {
                    case "BTC":
                        throw new NotImplementedException("Bitcoin balance check not implemented");
                    case "ETH":
                        return await GetEthBalanceAsync(address);
                    case "BSC":
                    case "BNB_BSC":
                        throw new NotImplementedException("BSC balance check not implemented");
                    case "USDT":
                    case "USDT_BEP20":
                        return await GetUSDT_BSCBalanceAsync(address, "BSC");
                    case "BSC_USDT":
                    case "USDT_TRC20":
                        return await GetUSDTTRC20BalanceAsync(address, "TRC20");
                    case "TRX_TRC20":
                        return await GetTrxTRC20BalanceAsync(address);
                    case "TRON_USDT":
                    default:
                        throw new NotSupportedException($"Balance check for {coinSymbol} is not implemented yet.");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"API request failed for {coinSymbol}: {ex.Message}");
            }
        }

        private async Task<decimal> GetUSDT_BSCBalanceAsync(string address, string network = "BSC")
        {
            try
            {
                if (network.ToUpper() == "TRC20" || network.ToUpper() == "TRON")
                {
                    return await _usdtTrcBalance.GetBalanceAsync(address);
                }
                else
                {
                    return await _usdtBSCBalance.GetBalanceAsync(address);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting USDT balance on {network}: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetUSDTTRC20BalanceAsync(string address, string network = "TRC20")
        {
            try
            {
                if (network.ToUpper() == "TRC20" || network.ToUpper() == "TRON")
                {
                    return await _usdtTrcBalance.GetBalanceAsync(address);
                }
                else
                {
                    return await _usdtBSCBalance.GetBalanceAsync(address);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting USDT balance on {network}: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetTrxTRC20BalanceAsync(string address)
        {
            try
            {
                return await _trxTrcBalance.GetTrxBalanceAsync(address);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting TRX balance: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current ETH balance for an address using direct RPC calls
        /// </summary>
        /// <param name="address">The Ethereum address to check</param>
        /// <returns>The balance in ETH as a decimal</returns>
        public async Task<decimal> GetEthBalanceAsync(string address)
        {
            // Validate the address
            if (!ValidateEthereumAddress(address))
                throw new ArgumentException("Invalid Ethereum address format", nameof(address));

            // Use direct RPC calls to get balance
            int attempt = 0;
            Exception? lastException = null;
            
            while (attempt < MaxRetries)
            {
                try
                {
                    var currentEndpoint = _ethRpcEndpoints[_currentEthEndpointIndex];
                    Console.WriteLine($"Using Ethereum RPC endpoint: {currentEndpoint}");

                    // Create JSON-RPC request for eth_getBalance
                    var requestContent = new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("method", "eth_getBalance"),
                        new JProperty("params", new JArray(address, "latest")),
                        new JProperty("id", 1)
                    );

                    var content = new StringContent(requestContent.ToString(), System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(currentEndpoint, content);
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var resultHex = jsonResponse["result"]?.ToString();

                    if (!string.IsNullOrEmpty(resultHex))
                    {
                        // Convert hex to decimal (remove "0x" prefix and parse)
                        if (resultHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            resultHex = resultHex.Substring(2);

                        if (System.Numerics.BigInteger.TryParse(resultHex, System.Globalization.NumberStyles.HexNumber, null, out var wei))
                        {
                            decimal ethBalance = (decimal)wei / 1_000_000_000_000_000_000m; // 18 decimals
                            Console.WriteLine($"ETH Balance: {ethBalance} ETH (from RPC)");
                            return ethBalance;
                        }
                    }

                    throw new Exception("Invalid response format from Ethereum RPC");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    // Try next endpoint
                    _currentEthEndpointIndex = (_currentEthEndpointIndex + 1) % _ethRpcEndpoints.Count;
                    
                    if (attempt < MaxRetries)
                    {
                        Console.WriteLine($"RPC request failed: {ex.Message}. Retrying ({attempt}/{MaxRetries})...");
                        await Task.Delay(1000); // Wait a bit before retrying
                    }
                }
            }
            
            throw new Exception($"Failed to get ETH balance after {MaxRetries} attempts: {lastException?.Message}", lastException);
        }

        private bool ValidateEthereumAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // Basic Ethereum address format validation (0x followed by 40 hex characters)
            return Regex.IsMatch(address, @"^0x[0-9a-fA-F]{40}$");
        }

        public async Task UpdateWalletBalancesAsync(UniversalColdWallet wallet)
        {
            var balances = new Dictionary<string, Dictionary<int, decimal>>();
            var export = wallet.ExportWallet();

            foreach (var coinAddresses in export.Addresses)
            {
                var coinSymbol = coinAddresses.Key;
                var addressBalances = new Dictionary<int, decimal>();

                foreach (var addressInfo in coinAddresses.Value)
                {
                    try
                    {
                        var balance = await GetBalanceAsync(coinSymbol, addressInfo.Address);
                        addressBalances[addressInfo.Index] = balance;
                        Console.WriteLine($"Successfully updated {coinSymbol} balance for address {addressInfo.Address}: {balance}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting balance for {coinSymbol} address {addressInfo.Address}: {ex.Message}");
                    }
                }

                if (addressBalances.Count > 0)
                {
                    balances[coinSymbol] = addressBalances;
                }
            }

            wallet.UpdateAllBalances(balances);
        }
    }
}