using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ColdWallet.AccountBalances;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Text;

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

        // Bitcoin API endpoints
        private readonly List<string> _btcApiEndpoints = new List<string>
        {
            "https://api.blockcypher.com/v1/btc/main/addrs/{0}/balance",
            "https://blockchain.info/balance?active={0}&cors=true",
            "https://blockstream.info/api/address/{0}"
        };

        // Updated Ethereum RPC endpoints as requested
        private readonly List<string> _ethRpcEndpoints = new List<string>
        {
            "https://ethereum.publicnode.com",
            "https://eth.llamarpc.com",
            "https://ethereum.blockpi.network/v1/rpc/public"
        };
        private int _currentEthEndpointIndex = 0;

        // Bitcoin balance response model for multiple addresses
        public class BitcoinAddressBalance
        {
            public string AddressType { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public decimal Balance { get; set; }
            public string Error { get; set; } = string.Empty;
        }

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
                        // For BTC, we need to handle the case where address is actually in the form of index 
                        // and we need to query all addresses for that index
                        if (int.TryParse(address, out int accountIndex))
                        {
                            var wallet = new UniversalColdWallet(); // Temporary wallet just to get addresses
                            var balances = await GetAllBtcAddressTypesBalancesAsync(accountIndex, wallet);
                            // Return the sum of all address balances
                            return balances.Sum(b => b.Balance);
                        }
                        else
                        {
                            // If it's an actual address, just get balance for that specific address
                            return await GetBtcBalanceAsync(address);
                        }
                    case "ETH":
                        return await GetEthBalanceAsync(address);
                    case "LTC":
                        // For LTC, handle all address types for the account index
                        if (int.TryParse(address, out int ltcAccountIndex))
                        {
                            var wallet = new UniversalColdWallet(); // Temporary wallet just to get addresses
                            var ltcBalances = await GetAllLtcAddressTypesBalancesAsync(ltcAccountIndex, wallet);
                            // Return the sum of all address balances
                            return ltcBalances.Sum(b => b.Balance);
                        }
                        else
                        {
                            // If it's an actual address, just get balance for that specific address
                            return await GetLtcBalanceAsync(address);
                        }
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

        public async Task<List<BitcoinAddressBalance>> GetAllBtcAddressTypesBalancesAsync(int accountIndex, UniversalColdWallet wallet)
        {
            var result = new List<BitcoinAddressBalance>();
            
            // Get all address types for this account index
            var addressTypes = wallet.GetBitcoinAddressTypes(accountIndex);
            
            foreach (var addressEntry in addressTypes)
            {
                var addressType = addressEntry.Key;
                var address = addressEntry.Value;
                
                try
                {
                    var balance = await GetBtcBalanceAsync(address);
                    result.Add(new BitcoinAddressBalance
                    {
                        AddressType = addressType,
                        Address = address,
                        Balance = balance
                    });
                    
                    _logger?.LogDebug($"Retrieved balance for {addressType}: {address} = {balance} BTC");
                }
                catch (Exception ex)
                {
                    result.Add(new BitcoinAddressBalance
                    {
                        AddressType = addressType,
                        Address = address,
                        Balance = 0,
                        Error = ex.Message
                    });
                    
                    _logger?.LogDebug($"Error getting balance for {addressType}: {address} - {ex.Message}");
                }
            }
            
            return result;
        }

        public async Task<List<BitcoinAddressBalance>> GetAllLtcAddressTypesBalancesAsync(int accountIndex, UniversalColdWallet wallet)
        {
            var result = new List<BitcoinAddressBalance>();
            
            // Get all Litecoin address types for this account index
            var ltcAddressTypes = await GetLitecoinAddressTypesAsync(wallet, accountIndex);
            
            foreach (var addressEntry in ltcAddressTypes)
            {
                var addressType = addressEntry.Key;
                var address = addressEntry.Value;
                
                try
                {
                    var balance = await GetLtcBalanceAsync(address);
                    result.Add(new BitcoinAddressBalance
                    {
                        AddressType = addressType,
                        Address = address,
                        Balance = balance
                    });
                    
                    _logger?.LogDebug($"Retrieved LTC balance for {addressType}: {address} = {balance} LTC");
                }
                catch (Exception ex)
                {
                    result.Add(new BitcoinAddressBalance
                    {
                        AddressType = addressType,
                        Address = address,
                        Balance = 0,
                        Error = ex.Message
                    });
                    
                    _logger?.LogDebug($"Error getting LTC balance for {addressType}: {address} - {ex.Message}");
                }
            }
            
            return result;
        }

        private async Task<Dictionary<string, string>> GetLitecoinAddressTypesAsync(UniversalColdWallet wallet, int accountIndex)
        {
            var coinInfo = new SupportedCoins().GetCoinInfo("LTC");
            var mnemonic = new NBitcoin.Mnemonic(wallet.GetMnemonic());
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(coinInfo.DerivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);
            var pubKey = derivedKey.PrivateKey.PubKey;

            var pubKeyHash = pubKey.Hash.ToBytes();
            var nativeBech32 = GenerateLitecoinBech32Address(pubKeyHash);

            return new Dictionary<string, string>
            {
                { "Legacy (P2PKH)", pubKey.GetAddress(NBitcoin.ScriptPubKeyType.Legacy, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString() },
                { "Nested SegWit (P2SH-P2WPKH)", pubKey.GetAddress(NBitcoin.ScriptPubKeyType.SegwitP2SH, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString() },
                { "Native SegWit (Bech32, P2WPKH)", nativeBech32 }
            };
        }

        private string GenerateLitecoinBech32Address(byte[] pubKeyHash)
        {
            if (pubKeyHash.Length != 20)
                throw new ArgumentException("Public key hash must be 20 bytes", nameof(pubKeyHash));

            const string hrp = "ltc";
            const int witnessVersion = 0;

            var converted = ConvertBitsToBase32(pubKeyHash, 8, 5, true);

            var spec = new List<int> { witnessVersion };
            spec.AddRange(converted);

            var checksum = CalculateBech32Checksum(hrp, spec);

            var combined = spec.Concat(checksum).ToArray();

            const string charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
            var result = new StringBuilder(hrp + "1");

            foreach (var value in combined)
            {
                if (value < 0 || value >= charset.Length)
                    throw new InvalidOperationException($"Invalid character value: {value}");
                result.Append(charset[value]);
            }

            return result.ToString();
        }

        private List<int> ConvertBitsToBase32(byte[] data, int fromBits, int toBits, bool pad)
        {
            var result = new List<int>();
            int acc = 0;
            int bits = 0;
            int maxv = (1 << toBits) - 1;
            int maxAcc = (1 << (fromBits + toBits - 1)) - 1;

            foreach (byte value in data)
            {
                if (value < 0 || (value >> fromBits) != 0)
                    throw new ArgumentException("Invalid input data for base conversion");

                acc = ((acc << fromBits) | value) & maxAcc;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((acc >> bits) & maxv);
                }
            }

            if (pad)
            {
                if (bits > 0)
                    result.Add((acc << (toBits - bits)) & maxv);
            }
            else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
            {
                throw new ArgumentException("Invalid padding bits");
            }

            return result;
        }

        private List<int> CalculateBech32Checksum(string hrp, List<int> data)
        {
            var values = new List<int>();

            foreach (char c in hrp)
                values.Add(c >> 5);

            values.Add(0);

            foreach (char c in hrp)
                values.Add(c & 31);

            values.AddRange(data);

            values.AddRange(Enumerable.Repeat(0, 6));

            uint polymod = CalculateBech32Polymod(values) ^ 1;

            var checksum = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                checksum.Add((int)((polymod >> (5 * (5 - i))) & 31));
            }

            return checksum;
        }

        private uint CalculateBech32Polymod(List<int> values)
        {
            uint[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            uint chk = 1;

            foreach (int value in values)
            {
                uint top = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ (uint)value;

                for (int i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) == 1)
                        chk ^= generator[i];
                }
            }

            return chk;
        }

        public async Task<decimal> GetBtcBalanceAsync(string address)
        {
            // Validate the address using our enhanced address verification
            if (!ValidateBitcoinAddress(address))
                throw new ArgumentException("Invalid Bitcoin address format", nameof(address));

            Exception? lastException = null;
            
            // Try each API endpoint
            foreach (var apiEndpointTemplate in _btcApiEndpoints)
            {
                string apiEndpoint = string.Format(apiEndpointTemplate, address);
                _logger?.LogDebug($"Trying BTC API endpoint for {address}: {apiEndpoint}");
                
                int attempt = 0;
                while (attempt < MaxRetries)
                {
                    try
                    {
                        decimal balance = await TryGetBtcBalanceFromEndpoint(apiEndpoint, address);
                        _logger?.LogDebug($"Successfully retrieved BTC balance from {apiEndpoint}: {balance} BTC");
                        return balance;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        attempt++;
                        
                        if (attempt < MaxRetries)
                        {
                            _logger?.LogDebug($"BTC balance request failed for {apiEndpoint}: {ex.Message}. Retrying ({attempt}/{MaxRetries})...");
                            await Task.Delay(1000);
                        }
                        else
                        {
                            _logger?.LogDebug($"Failed to get BTC balance from {apiEndpoint} after {MaxRetries} attempts");
                        }
                    }
                }
            }
            
            // If all endpoints failed, throw the last exception
            throw new Exception($"Failed to get Bitcoin balance after trying all endpoints.", lastException);
        }

        private async Task<decimal> TryGetBtcBalanceFromEndpoint(string url, string address)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet/1.0");

            // Add API key for BlockCypher if available
            if (url.Contains("blockcypher.com") && !string.IsNullOrEmpty(_apiKeys["BLOCKCYPHER"]))
            {
                if (url.Contains("?"))
                    url += $"&token={_apiKeys["BLOCKCYPHER"]}";
                else
                    url += $"?token={_apiKeys["BLOCKCYPHER"]}";
            }

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            // Different APIs return data in different formats
            if (url.Contains("blockcypher.com"))
            {
                // BlockCypher format
                var balance = json["final_balance"]?.ToString();
                if (!string.IsNullOrEmpty(balance) && decimal.TryParse(balance, out decimal result))
                {
                    // Convert from satoshi to BTC
                    return result / 100_000_000m;
                }
            }
            else if (url.Contains("blockchain.info"))
            {
                // Blockchain.info format
                var addressData = json[address];
                if (addressData != null)
                {
                    var balance = addressData["final_balance"]?.ToString();
                    if (!string.IsNullOrEmpty(balance) && decimal.TryParse(balance, out decimal result))
                    {
                        // Convert from satoshi to BTC
                        return result / 100_000_000m;
                    }
                }
            }
            else if (url.Contains("blockstream.info"))
            {
                // Blockstream.info format (returns chain_stats and mempool_stats)
                var chainStats = json["chain_stats"];
                if (chainStats != null)
                {
                    var funded = chainStats["funded_txo_sum"]?.ToObject<decimal>() ?? 0;
                    var spent = chainStats["spent_txo_sum"]?.ToObject<decimal>() ?? 0;
                    var balance = funded - spent;
                    
                    // Convert from satoshi to BTC
                    return balance / 100_000_000m;
                }
            }

            _logger?.LogDebug($"Failed to parse API response: {response}");
            throw new Exception("Failed to parse Bitcoin balance from API response");
        }

        private bool ValidateBitcoinAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // Basic validation patterns for different Bitcoin address formats
            
            // Legacy addresses (P2PKH) start with 1
            bool isLegacy = Regex.IsMatch(address, @"^[1][a-km-zA-HJ-NP-Z1-9]{25,34}$");
            
            // Nested SegWit addresses (P2SH) start with 3
            bool isNestedSegWit = Regex.IsMatch(address, @"^[3][a-km-zA-HJ-NP-Z1-9]{25,34}$");
            
            // Native SegWit addresses (Bech32) start with bc1
            bool isNativeSegWit = Regex.IsMatch(address, @"^bc1[a-z0-9]{25,90}$");
            
            return isLegacy || isNestedSegWit || isNativeSegWit;
        }

        private bool ValidateLitecoinAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // Basic validation patterns for different Litecoin address formats
            
            // Legacy addresses (P2PKH) start with L
            bool isLegacy = Regex.IsMatch(address, @"^[L][a-km-zA-HJ-NP-Z1-9]{25,34}$");
            
            // Nested SegWit addresses (P2SH) start with M
            bool isNestedSegWit = Regex.IsMatch(address, @"^[M][a-km-zA-HJ-NP-Z1-9]{25,34}$");
            
            // Native SegWit addresses (Bech32) start with ltc1
            bool isNativeSegWit = Regex.IsMatch(address, @"^ltc1[a-z0-9]{25,90}$");
            
            return isLegacy || isNestedSegWit || isNativeSegWit;
        }

        public async Task<decimal> GetLtcBalanceAsync(string address)
        {
            // Validate the address using Litecoin address verification
            if (!ValidateLitecoinAddress(address))
                throw new ArgumentException("Invalid Litecoin address format", nameof(address));

            Exception? lastException = null;
            
            // Litecoin API endpoints
            var ltcApiEndpoints = new List<string>
            {
                "https://api.blockcypher.com/v1/ltc/main/addrs/{0}/balance",
                "https://blockstream.info/litecoin/api/address/{0}"
            };
            
            // Try each API endpoint
            foreach (var apiEndpointTemplate in ltcApiEndpoints)
            {
                string apiEndpoint = string.Format(apiEndpointTemplate, address);
                _logger?.LogDebug($"Trying LTC API endpoint for {address}: {apiEndpoint}");
                
                int attempt = 0;
                while (attempt < MaxRetries)
                {
                    try
                    {
                        decimal balance = await TryGetLtcBalanceFromEndpoint(apiEndpoint, address);
                        _logger?.LogDebug($"Successfully retrieved LTC balance from {apiEndpoint}: {balance} LTC");
                        return balance;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        attempt++;
                        
                        if (attempt < MaxRetries)
                        {
                            _logger?.LogDebug($"LTC balance request failed for {apiEndpoint}: {ex.Message}. Retrying ({attempt}/{MaxRetries})...");
                            await Task.Delay(1000);
                        }
                        else
                        {
                            _logger?.LogDebug($"Failed to get LTC balance from {apiEndpoint} after {MaxRetries} attempts");
                        }
                    }
                }
            }
            
            // If all endpoints failed, throw the last exception
            throw new Exception($"Failed to get Litecoin balance after trying all endpoints.", lastException);
        }

        private async Task<decimal> TryGetLtcBalanceFromEndpoint(string url, string address)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet/1.0");

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            // Different APIs return data in different formats
            if (url.Contains("blockcypher.com"))
            {
                // BlockCypher format
                var balance = json["final_balance"]?.ToString();
                if (!string.IsNullOrEmpty(balance) && decimal.TryParse(balance, out decimal result))
                {
                    // Convert from litoshi to LTC (same as satoshi to BTC)
                    return result / 100_000_000m;
                }
            }
            else if (url.Contains("blockstream.info"))
            {
                // Blockstream.info format (returns chain_stats and mempool_stats)
                var chainStats = json["chain_stats"];
                if (chainStats != null)
                {
                    var funded = chainStats["funded_txo_sum"]?.ToObject<decimal>() ?? 0;
                    var spent = chainStats["spent_txo_sum"]?.ToObject<decimal>() ?? 0;
                    var balance = funded - spent;
                    
                    // Convert from litoshi to LTC
                    return balance / 100_000_000m;
                }
            }

            _logger?.LogDebug($"Failed to parse LTC API response: {response}");
            throw new Exception("Failed to parse Litecoin balance from API response");
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
                        // Special handling for BTC to handle all address types
                        if (coinSymbol == "BTC")
                        {
                            var allBtcBalances = await GetAllBtcAddressTypesBalancesAsync(addressInfo.Index, wallet);
                            decimal totalBtcBalance = allBtcBalances.Sum(b => b.Balance);
                            addressBalances[addressInfo.Index] = totalBtcBalance;
                            
                            // Store individual address type balances
                            if (export.BtcAddressTypeBalances == null)
                                export.BtcAddressTypeBalances = new Dictionary<string, Dictionary<string, decimal>>();
                            
                            var addressTypeBalances = new Dictionary<string, decimal>();
                            foreach (var addrBalance in allBtcBalances)
                            {
                                addressTypeBalances[addrBalance.AddressType] = addrBalance.Balance;
                                
                                // Also store in the AddressInfo
                                var address = export.Addresses["BTC"].FirstOrDefault(a => a.Index == addressInfo.Index);
                                if (address != null)
                                {
                                    if (address.AddressTypeBalances == null)
                                        address.AddressTypeBalances = new Dictionary<string, decimal>();
                                    
                                    address.AddressTypeBalances[addrBalance.AddressType] = addrBalance.Balance;
                                }
                            }
                            
                            // Store by index in export
                            export.BtcAddressTypeBalances[addressInfo.Index.ToString()] = addressTypeBalances;
                            
                            // Print detailed balance info
                            _logger?.LogDebug($"BTC Address Index {addressInfo.Index} Balances:");
                            foreach (var addrBalance in allBtcBalances)
                            {
                                _logger?.LogDebug($"  {addrBalance.AddressType}: {addrBalance.Address}");
                                _logger?.LogDebug($"    Balance: {addrBalance.Balance} BTC");
                                if (!string.IsNullOrEmpty(addrBalance.Error))
                                {
                                    _logger?.LogDebug($"    Error: {addrBalance.Error}");
                                }
                            }
                            _logger?.LogDebug($"  Total BTC Balance: {totalBtcBalance}");
                        }
                        // Special handling for LTC to handle all address types
                        else if (coinSymbol == "LTC")
                        {
                            var allLtcBalances = await GetAllLtcAddressTypesBalancesAsync(addressInfo.Index, wallet);
                            decimal totalLtcBalance = allLtcBalances.Sum(b => b.Balance);
                            addressBalances[addressInfo.Index] = totalLtcBalance;
                            
                            // Store individual address type balances in AddressInfo
                            var address = export.Addresses["LTC"].FirstOrDefault(a => a.Index == addressInfo.Index);
                            if (address != null)
                            {
                                if (address.AddressTypeBalances == null)
                                    address.AddressTypeBalances = new Dictionary<string, decimal>();
                                
                                foreach (var addrBalance in allLtcBalances)
                                {
                                    address.AddressTypeBalances[addrBalance.AddressType] = addrBalance.Balance;
                                }
                            }
                            
                            // Print detailed balance info
                            _logger?.LogDebug($"LTC Address Index {addressInfo.Index} Balances:");
                            foreach (var addrBalance in allLtcBalances)
                            {
                                _logger?.LogDebug($"  {addrBalance.AddressType}: {addrBalance.Address}");
                                _logger?.LogDebug($"    Balance: {addrBalance.Balance} LTC");
                                if (!string.IsNullOrEmpty(addrBalance.Error))
                                {
                                    _logger?.LogDebug($"    Error: {addrBalance.Error}");
                                }
                            }
                            _logger?.LogDebug($"  Total LTC Balance: {totalLtcBalance}");
                        }
                        else
                        {
                            var balance = await GetBalanceAsync(coinSymbol, addressInfo.Address);
                            addressBalances[addressInfo.Index] = balance;
                            _logger?.LogDebug($"Successfully updated {coinSymbol} balance for address {addressInfo.Address}: {balance}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug($"Error getting balance for {coinSymbol} address {addressInfo.Address}: {ex.Message}");
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