using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ColdWallet.AccountBalances
{
    public class TRX_TRC20AccountBalance
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Default wallet address
        /// </summary>
        public string WalletAddress { get; set; } = "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF"; // Default TRX wallet address

        public TRX_TRC20AccountBalance()
        {
            _httpClient = new HttpClient();
        }

        public TRX_TRC20AccountBalance(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the TRX balance for a specified wallet address
        /// </summary>
        /// <param name="address">The TRX wallet address</param>
        /// <returns>Balance in TRX</returns>
        public async Task<decimal> GetTrxBalanceAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));

            try
            {
                Console.WriteLine($"Fetching TRX balance for address: {address}");
                
                // TronGrid API
                string apiUrl = $"https://api.trongrid.io/v1/accounts/{address}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet");

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API error: {response.StatusCode}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonResponse);

                // Check if data array exists and has elements
                if (json["data"] is JArray dataArray && dataArray.Count > 0)
                {
                    JToken? firstItem = dataArray.FirstOrDefault();
                    if (firstItem != null)
                    {
                        // Balance is in Sun, convert to TRX by dividing by 1,000,000
                        long balanceInSun = firstItem["balance"]?.Value<long>() ?? 0;
                        var balance = balanceInSun / 1_000_000m; // Convert Sun to TRX
                        
                        Console.WriteLine($"TRX balance found: {balance} TRX");
                        return balance;
                    }
                    else
                    {
                        Console.WriteLine("Uyarı: Cüzdan için veri bulunamadı. Hesap yeni oluşturulmuş olabilir.");
                    }
                }
                else
                {
                    Console.WriteLine("Uyarı: API'den beklenmeyen yanıt format. Veri dizi içermiyor.");
                }

                // Alternative approach - check with Tronscan API
                Console.WriteLine("Trying alternative API...");
                return await GetTrxBalanceFromTronscanAsync(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTrxBalanceAsync: {ex.Message}");
                throw new Exception($"TRX balance sorgulama hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alternative method to get TRX balance using Tronscan API
        /// </summary>
        private async Task<decimal> GetTrxBalanceFromTronscanAsync(string address)
        {
            try
            {
                string url = $"https://apilist.tronscan.org/api/account?address={address}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var balance = json["balance"]?.ToString();
                if (!string.IsNullOrEmpty(balance) && decimal.TryParse(balance, out decimal result))
                {
                    var trxBalance = result / 1_000_000m; // Convert from Sun to TRX
                    Console.WriteLine($"TRX balance (alternative): {trxBalance} TRX");
                    return trxBalance;
                }

                Console.WriteLine($"Could not parse TRX balance: {balance}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting TRX balance via alternative API: {ex.Message}");
                return 0; // Return 0 as a default value rather than throw exception
            }
        }

        /// <summary>
        /// Gets the TRX balance using the default wallet address
        /// </summary>
        /// <returns>Balance in TRX</returns>
        public async Task<decimal> GetTrxBalanceAsync()
        {
            return await GetTrxBalanceAsync(WalletAddress);
        }

        /// <summary>
        /// Gets all TRC20 token balances for a given address
        /// </summary>
        /// <param name="address">The TRX wallet address</param>
        /// <returns>Dictionary with token symbols and balances</returns>
        public async Task<Dictionary<string, decimal>> GetTokenBalancesAsync(string address)
        {
            var tokens = new Dictionary<string, decimal>();

            try
            {
                var url = $"https://apilist.tronscanapi.com/api/account/tokens?address={address}&limit=20&start=0";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["data"] is JArray tokensArray)
                {
                    foreach (var token in tokensArray)
                    {
                        var tokenAbbr = token["tokenAbbr"]?.ToString();
                        var tokenDecimalToken = token["tokenDecimal"];
                        var tokenDecimal = tokenDecimalToken != null ? tokenDecimalToken.Value<int>() : 0;
                        var balanceStr = token["balance"]?.ToString();

                        if (!string.IsNullOrEmpty(tokenAbbr) && !string.IsNullOrEmpty(balanceStr) &&
                            decimal.TryParse(balanceStr, out decimal balance))
                        {
                            // Convert based on token decimal places
                            decimal divisor = (decimal)Math.Pow(10, tokenDecimal);
                            var actualBalance = balance / divisor;
                            
                            tokens[tokenAbbr] = actualBalance;
                            Console.WriteLine($"Found token: {tokenAbbr} with balance: {actualBalance}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting token balances: {ex.Message}");
            }

            return tokens;
        }

        /// <summary>
        /// Gets the complete account information including TRX and token balances
        /// </summary>
        /// <param name="address">The TRX wallet address</param>
        /// <returns>A dictionary containing balance information</returns>
        public async Task<Dictionary<string, decimal>> GetCompleteAccountInfoAsync(string address)
        {
            var results = new Dictionary<string, decimal>();
            
            try
            {
                // Get TRX balance
                var trxBalance = await GetTrxBalanceAsync(address);
                results["TRX"] = trxBalance;
                
                // Get token balances
                var tokens = await GetTokenBalancesAsync(address);
                
                // Add tokens to result
                foreach (var token in tokens)
                {
                    results[token.Key] = token.Value;
                }
                
                Console.WriteLine($"Complete account info retrieved: {string.Join(", ", results.Keys)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting complete account info: {ex.Message}");
            }
            
            return results;
        }
    }
}