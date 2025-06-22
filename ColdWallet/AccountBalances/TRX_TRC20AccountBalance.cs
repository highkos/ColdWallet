using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ColdWallet.AccountBalances
{
    public class TRX_TRC20AccountBalance
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Default wallet address
        /// </summary>
        public string WalletAddress { get; set; } = "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF"; // Buraya TRX cüzdan adresinizi yazın

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
        public async Task<decimal> GetTrxBalance(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));

            try
            {
                // TronGrid API
                string apiUrl = $"https://api.trongrid.io/v1/accounts/{address}";

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API error: {response.StatusCode}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonResponse);

                // Check if data array exists and has elements
                if (json["data"] != null && json["data"].Type == JTokenType.Array)
                {
                    var dataArray = (JArray)json["data"];
                    if (dataArray.Count > 0 && dataArray[0] != null)
                    {
                        // Balance is in Sun, convert to TRX by dividing by 1,000,000
                        long balanceInSun = dataArray[0]["balance"]?.Value<long>() ?? 0;
                        return balanceInSun / 1_000_000m; // Convert Sun to TRX
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

                // Alternative approach for debugging
                Console.WriteLine($"API Yanıtı: {jsonResponse}");

                return 0; // No balance found
            }
            catch (Exception ex)
            {
                throw new Exception($"TRX balance sorgulama hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the TRX balance using the default wallet address
        /// </summary>
        /// <returns>Balance in TRX</returns>
        public async Task<decimal> GetTrxBalance()
        {
            return await GetTrxBalance(WalletAddress);
        }
    }
}