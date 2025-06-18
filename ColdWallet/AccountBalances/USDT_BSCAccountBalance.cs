using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColdWallet.AccountBalances
{
    //    internal class USDT_BSCCAccountBalance
    //    {
    //        private readonly HttpClient _httpClient;
    //        private readonly string _apiKey;
    //        private const string USDT_BSC_CONTRACT = "0x55d398326f99059fF775485246999027B3197955";

    //        public USDT_BSCCAccountBalance(HttpClient httpClient, string apiKey)
    //        {
    //            _httpClient = httpClient;
    //            _apiKey = apiKey;
    //        }

    //        public async Task<decimal> GetBalanceAsync(string address)
    //        {
    //            try
    //            {
    //                var url = $"https://api.bscscan.com/api" +
    //                         $"?module=account" +
    //                         $"&action=tokenbalance" +
    //                         $"&contractaddress={USDT_BSC_CONTRACT}" +
    //                         $"&address={address}" +
    //                         $"&tag=latest" +
    //                         $"&apikey={_apiKey}";

    //                _httpClient.DefaultRequestHeaders.Clear();
    //                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    //                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet");

    //                var response = await _httpClient.GetStringAsync(url);
    //                var json = JObject.Parse(response);

    //                Console.WriteLine($"BSCScan API Response: {response}");

    //                var status = json["status"]?.ToString();
    //                var message = json["message"]?.ToString();
    //                var result = json["result"]?.ToString();

    //                if (status == "1")
    //                {
    //                    if (decimal.TryParse(result, out decimal balance))
    //                    {
    //                        var usdtBalance = balance / 1_000_000_000_000_000_000m; // BSC USDT uses 18 decimals
    //                        Console.WriteLine($"USDT Balance: {usdtBalance} USDT (Raw: {balance})");
    //                        return usdtBalance;
    //                    }
    //                    throw new Exception($"Invalid balance format. Raw value: {result}");
    //                }
    //                else if (status == "0")
    //                {
    //                    if (message?.ToLower().Contains("invalid api key") == true)
    //                    {
    //                        throw new Exception("Invalid BSCScan API key");
    //                    }
    //                    else if (message?.ToLower().Contains("rate limit") == true)
    //                    {
    //                        throw new Exception("BSCScan API rate limit exceeded");
    //                    }
    //                }

    //                throw new Exception($"BSCScan API error - Status: {status}, Message: {message ?? "No message"}, Result: {result ?? "No result"}");
    //            }
    //            catch (HttpRequestException ex)
    //            {
    //                throw new Exception($"BSCScan API request failed: {ex.Message}", ex);
    //            }
    //            catch (Exception ex) when (ex.Message.Contains("rate limit"))
    //            {
    //                await Task.Delay(2000);
    //                return await GetBalanceAsync(address);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw new Exception($"Error processing USDT balance: {ex.Message}", ex);
    //            }
    //        }
    //    }
    //}
}