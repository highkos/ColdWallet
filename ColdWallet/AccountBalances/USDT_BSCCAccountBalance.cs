using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ColdWallet.AccountBalances
{
    public class USDT_BSCCAccountBalance : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<USDT_BSCCAccountBalance> _logger;
        private bool _disposed = false;

        private const string USDT_BSC_CONTRACT = "0x55d398326f99059fF775485246999027B3197955";
        private const string BASE_URL = "https://api.bscscan.com/api";
        private const decimal USDT_DECIMALS = 1_000_000_000_000_000_000m;

        public USDT_BSCCAccountBalance(HttpClient httpClient, string apiKey, ILogger<USDT_BSCCAccountBalance> logger)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _logger = logger;
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(USDT_BSCCAccountBalance));

            try
            {
                var url = $"{BASE_URL}?module=account&action=tokenbalance&contractaddress={USDT_BSC_CONTRACT}&address={address}&tag=latest&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                
                var status = json["status"]?.ToString();
                var result = json["result"]?.ToString();

                if (status == "1" && decimal.TryParse(result, out decimal rawBalance))
                {
                    return rawBalance / USDT_DECIMALS;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting USDT balance for {Address}", address);
                return 0;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}