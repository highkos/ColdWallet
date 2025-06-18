using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ColdWallet.AccountBalances
{
    public class USDT_TRCAccountBalance
    {
        private readonly HttpClient _httpClient;
        private const string TRONSCAN_API = "https://apilist.tronscanapi.com/api/account/tokens";

        public USDT_TRCAccountBalance(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));

            try
            {
                var url = $"{TRONSCAN_API}?address={address}&limit=20&start=0";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data["data"] is JArray tokens)
                {
                    foreach (var token in tokens)
                    {
                        var tokenAbbr = token["tokenAbbr"]?.ToString();
                        if (tokenAbbr == "USDT")
                        {
                            var balanceStr = token["balance"]?.ToString();
                            if (decimal.TryParse(balanceStr, out decimal balance))
                            {
                                // USDT TRC20 has 6 decimal places
                                return balance / 1_000_000m;
                            }
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting USDT balance: {ex.Message}");
                return 0;
            }
        }
    }
}