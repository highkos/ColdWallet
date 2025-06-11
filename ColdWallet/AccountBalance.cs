using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UniversalColdWallet
{
    public class AccountBalance
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _apiKeys;

        public AccountBalance()
        {
            _httpClient = new HttpClient();
            _apiKeys = new Dictionary<string, string>
            {
                { "ETHERSCAN", Environment.GetEnvironmentVariable("ETHERSCAN_API_KEY") ?? "" },
                // BSCScan API anahtarýný doðrudan string olarak kullan
                { "BSCSCAN", "8IBNZTXZWPTR6V4S4AKJWA5BAMPXZU4IHI" },
                { "BLOCKCYPHER", Environment.GetEnvironmentVariable("BLOCKCYPHER_API_KEY") ?? "" },
                { "SOLSCAN", Environment.GetEnvironmentVariable("SOLSCAN_API_KEY") ?? "" }
            };
            
            // HttpClient timeout süresini artýr
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<decimal> GetBalanceAsync(string coinSymbol, string address)
        {
            try
            {
                switch (coinSymbol.ToUpper())
                {
                    case "ETH":
                        return await GetEthereumBalanceAsync(address);
                    case "BTC":
                        return await GetBitcoinBalanceAsync(address);
                    case "BSC":
                    case "BNB":
                    case "BNB_BSC": // Added support for BNB_BSC
                        return await GetBinanceSmartChainBalanceAsync(address);
                    case "SOL":
                        return await GetSolanaBalanceAsync(address);
                    case "TRX":
                        return await GetTronBalanceAsync(address);
                    case "ADA":
                        return await GetCardanoBalanceAsync(address);
                    case "XRP":
                        return await GetRippleBalanceAsync(address);
                    case "DOGE":
                        return await GetDogeBalanceAsync(address);
                    case "LTC":
                        return await GetLitecoinBalanceAsync(address);
                    default:
                        throw new NotSupportedException($"Balance check for {coinSymbol} is not implemented yet.");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"API request failed for {coinSymbol}: {ex.Message}");
            }
        }

        private async Task<decimal> GetEthereumBalanceAsync(string address)
        {
            var apiKey = _apiKeys["ETHERSCAN"];
            var url = $"https://api.etherscan.io/api?module=account&action=balance&address={address}&tag=latest&apikey={apiKey}";
            
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            
            if (json["status"]?.ToString() == "1")
            {
                var balance = json["result"]?.ToString();
                if (decimal.TryParse(balance, out decimal result))
                {
                    return result / 1_000_000_000_000_000_000m; // Convert from Wei to ETH
                }
            }
            
            throw new Exception($"Failed to get ETH balance: {json["message"]}");
        }

        private async Task<decimal> GetBitcoinBalanceAsync(string address)
        {
            var url = $"https://api.blockcypher.com/v1/btc/main/addrs/{address}/balance";
            if (!string.IsNullOrEmpty(_apiKeys["BLOCKCYPHER"]))
            {
                url += $"?token={_apiKeys["BLOCKCYPHER"]}";
            }
            
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            
            var balance = json["final_balance"]?.ToString();
            if (decimal.TryParse(balance, out decimal result))
            {
                return result / 100_000_000m; // Convert from Satoshi to BTC
            }
            
            throw new Exception("Failed to get BTC balance");
        }

        private async Task<decimal> GetBinanceSmartChainBalanceAsync(string address)
        {
            try
            {
                var apiKey = _apiKeys["BSCSCAN"];
                // API URL'sini düzelt ve gerekli parametreleri ekle
                var url = $"https://api.bscscan.com/api" +
                         $"?module=account" +
                         $"&action=balance" +
                         $"&address={address}" +
                         $"&tag=latest" +
                         $"&apikey={apiKey}";

                // HttpClient'ý temiz bir state ile kullan
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                // API yanýt detaylarýný logla
                Console.WriteLine($"BSCScan API Response: {response}");
                
                var status = json["status"]?.ToString();
                var message = json["message"]?.ToString();
                var result = json["result"]?.ToString();

                // Status kontrolü
                if (status == "1")
                {
                    if (decimal.TryParse(result, out decimal balance))
                    {
                        // Wei'den BNB'ye çevir (18 decimal)
                        var bnbBalance = balance / 1_000_000_000_000_000_000m;
                        Console.WriteLine($"BNB Balance: {bnbBalance} BNB (Raw: {balance} Wei)");
                        return bnbBalance;
                    }
                    throw new Exception($"Invalid balance format. Raw value: {result}");
                }
                else if (status == "0")
                {
                    // API özel hata durumlarý
                    if (message?.ToLower().Contains("invalid api key") == true)
                    {
                        throw new Exception("Invalid BSCScan API key");
                    }
                    else if (message?.ToLower().Contains("rate limit") == true)
                    {
                        throw new Exception("BSCScan API rate limit exceeded");
                    }
                }
                
                // Genel hata durumu
                throw new Exception($"BSCScan API error - Status: {status}, Message: {message ?? "No message"}, Result: {result ?? "No result"}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"BSCScan API request failed: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex.Message.Contains("rate limit"))
            {
                // Rate limit durumunda biraz bekleyip tekrar dene
                await Task.Delay(2000); // 2 saniye bekle
                return await GetBinanceSmartChainBalanceAsync(address);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing BNB balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetSolanaBalanceAsync(string address)
        {
            try
            {
                var url = $"https://public-api.solscan.io/account/{address}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var lamports = json["lamports"]?.ToString();
                if (decimal.TryParse(lamports, out decimal balance))
                {
                    return balance / 1_000_000_000m; // Convert from Lamports to SOL
                }
                
                throw new Exception($"Invalid SOL balance format: {lamports}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting SOL balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetTronBalanceAsync(string address)
        {
            try
            {
                var url = $"https://apilist.tronscan.org/api/account?address={address}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var balance = json["balance"]?.ToString();
                if (decimal.TryParse(balance, out decimal result))
                {
                    return result / 1_000_000m; // Convert from Sun to TRX
                }
                
                throw new Exception($"Invalid TRX balance format: {balance}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting TRX balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetRippleBalanceAsync(string address)
        {
            try
            {
                var url = $"https://api.xrpscan.com/api/v1/account/{address}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var balance = json["xrpBalance"]?.ToString();
                if (decimal.TryParse(balance, out decimal result))
                {
                    return result;
                }
                
                throw new Exception($"Invalid XRP balance format: {balance}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting XRP balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetCardanoBalanceAsync(string address)
        {
            try
            {
                var url = $"https://cardano-mainnet.blockfrost.io/api/v0/addresses/{address}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("project_id", _apiKeys["BLOCKFROST"]);
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var amount = json["amount"]?[0]?["quantity"]?.ToString();
                if (decimal.TryParse(amount, out decimal balance))
                {
                    return balance / 1_000_000m; // Convert from Lovelace to ADA
                }
                
                throw new Exception($"Invalid ADA balance format: {amount}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting ADA balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetDogeBalanceAsync(string address)
        {
            try
            {
                var url = $"https://api.blockcypher.com/v1/doge/main/addrs/{address}/balance";
                if (!string.IsNullOrEmpty(_apiKeys["BLOCKCYPHER"]))
                {
                    url += $"?token={_apiKeys["BLOCKCYPHER"]}";
                }
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var balance = json["final_balance"]?.ToString();
                if (decimal.TryParse(balance, out decimal result))
                {
                    return result / 100_000_000m; // Convert from Koinu to DOGE
                }
                
                throw new Exception($"Invalid DOGE balance format: {balance}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting DOGE balance: {ex.Message}", ex);
            }
        }

        private async Task<decimal> GetLitecoinBalanceAsync(string address)
        {
            try
            {
                var url = $"https://api.blockcypher.com/v1/ltc/main/addrs/{address}/balance";
                if (!string.IsNullOrEmpty(_apiKeys["BLOCKCYPHER"]))
                {
                    url += $"?token={_apiKeys["BLOCKCYPHER"]}";
                }
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var balance = json["final_balance"]?.ToString();
                if (decimal.TryParse(balance, out decimal result))
                {
                    return result / 100_000_000m; // Convert from Litoshi to LTC
                }
                
                throw new Exception($"Invalid LTC balance format: {balance}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting LTC balance: {ex.Message}", ex);
            }
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