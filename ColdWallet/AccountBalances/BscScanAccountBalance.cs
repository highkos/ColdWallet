using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ColdWallet.AccountBalances
{
    internal class BscScanAccountBalance
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public BscScanAccountBalance(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            try
            {
                var url = $"https://api.bscscan.com/api" +
                         $"?module=account" +
                         $"&action=balance" +
                         $"&address={address}" +
                         $"&tag=latest" +
                         $"&apikey={_apiKey}";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                Console.WriteLine($"BSCScan API Response: {response}");
                
                var status = json["status"]?.ToString();
                var message = json["message"]?.ToString();
                var result = json["result"]?.ToString();

                if (status == "1")
                {
                    if (decimal.TryParse(result, out decimal balance))
                    {
                        var bnbBalance = balance / 1_000_000_000_000_000_000m;
                        Console.WriteLine($"BNB Balance: {bnbBalance} BNB (Raw: {balance} Wei)");
                        return bnbBalance;
                    }
                    throw new Exception($"Invalid balance format. Raw value: {result}");
                }
                else if (status == "0")
                {
                    if (message?.ToLower().Contains("invalid api key") == true)
                    {
                        throw new Exception("Invalid BSCScan API key");
                    }
                    else if (message?.ToLower().Contains("rate limit") == true)
                    {
                        throw new Exception("BSCScan API rate limit exceeded");
                    }
                }
                
                throw new Exception($"BSCScan API error - Status: {status}, Message: {message ?? "No message"}, Result: {result ?? "No result"}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"BSCScan API request failed: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex.Message.Contains("rate limit"))
            {
                await Task.Delay(2000);
                return await GetBalanceAsync(address);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing BNB balance: {ex.Message}", ex);
            }
        }
    }
}
