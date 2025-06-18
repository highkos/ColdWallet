using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Linq;

namespace ColdWallet.AccountBalances
{
    // Common Models
    public class WalletBalance
    {
        [JsonPropertyName("network")]
        public required string Network { get; set; }

        [JsonPropertyName("tokenName")]
        public required string TokenName { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("rawBalance")]
        public string? RawBalance { get; set; }

        [JsonPropertyName("address")]
        public required string Address { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    // Ethereum API Models
    public class EtherscanResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }

    // BSC API Models
    public class BscScanResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }

    // Tron API Models
    public class TronGridResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public TronGridData[]? Data { get; set; }
    }

    public class TronGridData
    {
        [JsonPropertyName("balance")]
        public string? Balance { get; set; }

        [JsonPropertyName("tokenId")]
        public string? TokenId { get; set; }

        [JsonPropertyName("tokenName")]
        public string? TokenName { get; set; }

        [JsonPropertyName("tokenAbbr")]
        public string? TokenAbbr { get; set; }

        [JsonPropertyName("tokenDecimal")]
        public int TokenDecimal { get; set; }
    }

    public class MultiChainUsdtChecker
    {
        private readonly HttpClient _httpClient;

        // Contract Addresses
        private const string USDT_ETHEREUM_CONTRACT = "0xdAC17F958D2ee523a2206206994597C13D831ec7";
        private const string USDT_BSC_CONTRACT = "0x55d398326f99059fF775485246999027B3197955";
        private const string USDT_TRON_CONTRACT = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";

        // API Endpoints
        private const string ETHERSCAN_API = "https://api.etherscan.io/api";
        private const string BSCSCAN_API = "https://api.bscscan.com/api";
        private const string TRONGRID_API = "https://api.trongrid.io";

        // API Keys - ÖNEMLİ: Bu anahtarları environment variables'dan alın
        private readonly string _etherscanApiKey;
        private readonly string _bscscanApiKey;
        private readonly string _trongridApiKey;

        public MultiChainUsdtChecker(HttpClient httpClient, string etherscanApiKey, string bscscanApiKey, string trongridApiKey)
        {
            _httpClient = httpClient;
            _etherscanApiKey = etherscanApiKey;
            _bscscanApiKey = bscscanApiKey;
            _trongridApiKey = trongridApiKey;
        }

        private bool IsValidEthereumAddress(string address)
        {
            return !string.IsNullOrEmpty(address) &&
                   address.Length == 42 &&
                   address.StartsWith("0x") &&
                   System.Text.RegularExpressions.Regex.IsMatch(address, "^0x[0-9a-fA-F]{40}$");
        }

        private bool IsValidTronAddress(string address)
        {
            return !string.IsNullOrEmpty(address) &&
                   address.Length == 34 &&
                   address.StartsWith("T") &&
                   System.Text.RegularExpressions.Regex.IsMatch(address, "^T[1-9A-HJ-NP-Za-km-z]{33}$");
        }

        private Dictionary<string, string> GetWalletAddresses()
        {
            var addresses = new Dictionary<string, string>();

            Console.WriteLine("Enter your wallet addresses (press Enter to skip):");
            Console.WriteLine();

            // Ethereum Address
            Console.Write("Ethereum address (0x...): ");
            string? ethAddress = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(ethAddress) && IsValidEthereumAddress(ethAddress))
            {
                addresses["ethereum"] = ethAddress;
                Console.WriteLine("✅ Ethereum address added");
            }
            else if (!string.IsNullOrEmpty(ethAddress))
            {
                Console.WriteLine("⚠️ Invalid Ethereum address format");
            }

            // BSC Address (same validation as Ethereum since BSC uses same address format)
            Console.Write("BSC address (0x...): ");
            string? bscAddress = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(bscAddress) && IsValidEthereumAddress(bscAddress))
            {
                addresses["bsc"] = bscAddress;
                Console.WriteLine("✅ BSC address added");
            }
            else if (!string.IsNullOrEmpty(bscAddress))
            {
                Console.WriteLine("⚠️ Invalid BSC address format");
            }

            // Tron Address
            Console.Write("Tron address (T...): ");
            string? tronAddress = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(tronAddress) && IsValidTronAddress(tronAddress))
            {
                addresses["tron"] = tronAddress;
                Console.WriteLine("✅ Tron address added");
            }
            else if (!string.IsNullOrEmpty(tronAddress))
            {
                Console.WriteLine("⚠️ Invalid Tron address format");
            }

            return addresses;
        }

        public async Task<WalletBalance> CheckEthereumETHAsync(string address)
        {
            try
            {
                string url = $"{ETHERSCAN_API}?module=account&action=balance" +
                             $"&address={address}" +
                             $"&tag=latest" +
                             $"&apikey={_etherscanApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<EtherscanResponse>(response);

                if (result?.Status == "1" && !string.IsNullOrEmpty(result.Result))
                {
                    if (decimal.TryParse(result.Result, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal rawBalance))
                    {
                        decimal ethBalance = rawBalance / 1_000_000_000_000_000_000m; // 18 decimals

                        return new WalletBalance
                        {
                            Network = "Ethereum",
                            TokenName = "ETH",
                            Balance = ethBalance,
                            RawBalance = result.Result,
                            Address = address,
                            Success = true
                        };
                    }
                }
                return new WalletBalance
                {
                    Network = "Ethereum",
                    TokenName = "ETH",
                    Balance = 0,
                    Address = address,
                    Success = true,
                    Error = result?.Message ?? "Balance not found or parse error."
                };
            }
            catch (Exception ex)
            {
                return new WalletBalance
                {
                    Network = "Ethereum",
                    TokenName = "ETH",
                    Balance = 0,
                    Address = address,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<WalletBalance> CheckEthereumUSDTAsync(string address)
        {
            try
            {
                string url = $"{ETHERSCAN_API}?module=account&action=tokenbalance" +
                             $"&contractaddress={USDT_ETHEREUM_CONTRACT}" +
                             $"&address={address}" +
                             $"&tag=latest" +
                             $"&apikey={_etherscanApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<EtherscanResponse>(response);

                if (result?.Status == "1" && !string.IsNullOrEmpty(result.Result))
                {
                    if (decimal.TryParse(result.Result, out decimal rawBalance))
                    {
                        decimal usdtBalance = rawBalance / 1_000_000m; // USDT ERC20 has 6 decimals

                        return new WalletBalance
                        {
                            Network = "Ethereum (ERC20)",
                            TokenName = "USDT",
                            Balance = usdtBalance,
                            RawBalance = result.Result,
                            Address = address,
                            Success = true
                        };
                    }
                }

                return new WalletBalance
                {
                    Network = "Ethereum (ERC20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new WalletBalance
                {
                    Network = "Ethereum (ERC20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        // DÜZELTME: BSC USDT decimal değeri
        public async Task<WalletBalance> CheckBscUSDTAsync(string address)
        {
            try
            {
                string url = $"{BSCSCAN_API}?module=account&action=tokenbalance" +
                             $"&contractaddress={USDT_BSC_CONTRACT}" +
                             $"&address={address}" +
                             $"&tag=latest" +
                             $"&apikey={_bscscanApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<BscScanResponse>(response);

                if (result?.Status == "1" && !string.IsNullOrEmpty(result.Result))
                {
                    if (decimal.TryParse(result.Result, out decimal rawBalance))
                    {
                        // DÜZELTME: BSC USDT 18 decimal kullanır, 6 değil
                        decimal usdtBalance = rawBalance / 1_000_000_000_000_000_000m; // 18 decimals

                        return new WalletBalance
                        {
                            Network = "Binance Smart Chain (BEP20)",
                            TokenName = "USDT",
                            Balance = usdtBalance,
                            RawBalance = result.Result,
                            Address = address,
                            Success = true
                        };
                    }
                }

                return new WalletBalance
                {
                    Network = "Binance Smart Chain (BEP20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new WalletBalance
                {
                    Network = "Binance Smart Chain (BEP20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        // DÜZELTME: Tron API endpoint ve logic
        public async Task<WalletBalance> CheckTronUSDTAsync(string address)
        {
            try
            {
                // Tron Grid API için daha doğru endpoint
                string url = $"{TRONGRID_API}/v1/accounts/{address}";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                if (!string.IsNullOrEmpty(_trongridApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", _trongridApiKey);
                }

                var response = await _httpClient.GetStringAsync(url);

                // TronGrid'den gelen response'u parse et
                var accountInfo = JsonSerializer.Deserialize<JsonElement>(response);

                // TRC20 token balance kontrolü
                if (accountInfo.TryGetProperty("trc20", out var trc20Tokens))
                {
                    foreach (var token in trc20Tokens.EnumerateArray())
                    {
                        if (token.TryGetProperty(USDT_TRON_CONTRACT, out var usdtBalance))
                        {
                            if (decimal.TryParse(usdtBalance.GetString(), out decimal balance))
                            {
                                decimal formattedBalance = balance / 1_000_000m; // USDT TRC20 has 6 decimals

                                return new WalletBalance
                                {
                                    Network = "Tron (TRC20)",
                                    TokenName = "USDT",
                                    Balance = formattedBalance,
                                    RawBalance = balance.ToString(),
                                    Address = address,
                                    Success = true
                                };
                            }
                        }
                    }
                }

                return new WalletBalance
                {
                    Network = "Tron (TRC20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new WalletBalance
                {
                    Network = "Tron (TRC20)",
                    TokenName = "USDT",
                    Balance = 0,
                    Address = address,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<List<WalletBalance>> GetAllBalancesAsync(Dictionary<string, string> addresses)
        {
            var results = new List<WalletBalance>();

            if (addresses.TryGetValue("ethereum", out string? ethAddress))
            {
                Console.WriteLine("🚀 Checking Ethereum (ETH) balance...");
                results.Add(await CheckEthereumETHAsync(ethAddress));
                Console.WriteLine("🚀 Checking Ethereum (ERC20) USDT balance...");
                results.Add(await CheckEthereumUSDTAsync(ethAddress));
            }
            if (addresses.TryGetValue("bsc", out string? bscAddress))
            {
                Console.WriteLine("🚀 Checking Binance Smart Chain (BEP20) USDT balance...");
                results.Add(await CheckBscUSDTAsync(bscAddress));
            }
            if (addresses.TryGetValue("tron", out string? tronAddress))
            {
                Console.WriteLine("🚀 Checking Tron (TRC20) USDT balance...");
                results.Add(await CheckTronUSDTAsync(tronAddress));
            }
            return results;
        }

        // DÜZELTME: static async Task Main metodunu düzelt
        //public static async Task Main(string[] args)
        //{
        //    // API Keys'leri environment variables'dan al
        //    string etherscanApiKey = Environment.GetEnvironmentVariable("ETHERSCAN_API_KEY") ?? "N6DV7IRM4GEU3WB56ZVWVB1I6GXYDBIIAX";
        //    string bscscanApiKey = Environment.GetEnvironmentVariable("BSCSCAN_API_KEY") ?? "8IBNZTXZWPTR6V4S4AKJWA5BAMPXZU4IHI";
        //    string trongridApiKey = Environment.GetEnvironmentVariable("TRONGRID_API_KEY") ?? "0df1dc46-e032-43ff-8a15-c9e732b4afad";

        //    using var httpClient = new HttpClient();
        //    var checker = new MultiChainUsdtChecker(httpClient, etherscanApiKey, bscscanApiKey, trongridApiKey);

        //    var addresses = checker.GetWalletAddresses();
        //    if (!addresses.Any())
        //    {
        //        Console.WriteLine("No valid addresses provided. Exiting.");
        //        return;
        //    }

        //    Console.WriteLine("\nConnecting to APIs and fetching balances...");
        //    var results = await checker.GetAllBalancesAsync(addresses);

        //    DisplayResults(results);
        //}

        private static void DisplayResults(List<WalletBalance> results)
        {
            Console.WriteLine("📊 Crypto Balance Summary");
            Console.WriteLine("======================");

            decimal totalUsdtBalance = 0;
            decimal totalEthBalance = 0;

            foreach (var result in results.OrderBy(r => r.Network).ThenBy(r => r.TokenName))
            {
                Console.WriteLine($"\n🌐 {result.Network}");
                Console.WriteLine($"   Address: {result.Address}");

                if (result.Success)
                {
                    Console.WriteLine($"   💰 {result.TokenName} Balance: {result.Balance:F8}");
                    if (!string.IsNullOrEmpty(result.RawBalance))
                    {
                        Console.WriteLine($"   📝 Raw Balance: {result.RawBalance}");
                    }
                    if (result.TokenName == "USDT")
                    {
                        totalUsdtBalance += result.Balance;
                    }
                    else if (result.TokenName == "ETH")
                    {
                        totalEthBalance += result.Balance;
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Error: {result.Error}");
                }
            }

            Console.WriteLine($"\n--- Summary ---");
            Console.WriteLine($"💎 TOTAL USDT (across all chains): {totalUsdtBalance:F6} USDT");
            Console.WriteLine($"♦️ TOTAL ETH (Ethereum): {totalEthBalance:F8} ETH");

            if (results.Count > 1)
            {
                Console.WriteLine("\n📈 Distribution:");
                var groupedResults = results.Where(r => r.Success && r.Balance > 0)
                                            .GroupBy(r => new { r.Network, r.TokenName })
                                            .Select(g => new
                                            {
                                                g.Key.Network,
                                                g.Key.TokenName,
                                                Balance = g.Sum(x => x.Balance)
                                            })
                                            .OrderBy(x => x.Network)
                                            .ToList();

                foreach (var group in groupedResults)
                {
                    decimal overallTotal = 0;
                    if (group.TokenName == "USDT") overallTotal = totalUsdtBalance;
                    else if (group.TokenName == "ETH") overallTotal = totalEthBalance;

                    decimal percentage = overallTotal > 0 ? (group.Balance / overallTotal) * 100 : 0;
                    Console.WriteLine($"   {group.Network} - {group.TokenName}: {percentage:F1}% ({group.Balance:F8})");
                }
            }
        }
    }
}

// Program.cs (ayrı dosya olarak)
/*
using ColdWallet.AccountBalances;

await MultiChainUsdtChecker.Main(args);
*/