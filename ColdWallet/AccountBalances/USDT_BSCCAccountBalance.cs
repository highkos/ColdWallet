using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ColdWallet.AccountBalances
{
    /// <summary>
    /// Service for retrieving USDT token balances on Binance Smart Chain (BSC) network
    /// </summary>
    public interface IUsdtBscAccountBalance
    {
        Task<decimal> GetBalanceAsync(string address, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Production-ready implementation for fetching USDT balances from BSCScan API
    /// </summary>
    public class USDT_BSCCAccountBalance : IUsdtBscAccountBalance, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<USDT_BSCCAccountBalance> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private bool _disposed = false;

        private const string USDT_BSC_CONTRACT = "0x55d398326f99059fF775485246999027B3197955";
        private const string BASE_URL = "https://api.bscscan.com/api";
        private const decimal USDT_DECIMALS = 1_000_000_000_000_000_000m; // 18 decimals
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RATE_LIMIT_DELAY_MS = 2000;
        private const int REQUEST_TIMEOUT_SECONDS = 30;

        public USDT_BSCCAccountBalance(HttpClient httpClient, string apiKey, ILogger<USDT_BSCCAccountBalance> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ConfigureHttpClient();
            _retryPolicy = CreateRetryPolicy();
        }

        public async Task<decimal> GetBalanceAsync(string address, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address cannot be null or empty", nameof(address));
            }

            if (!IsValidBscAddress(address))
            {
                throw new ArgumentException("Invalid BSC address format", nameof(address));
            }

            try
            {
                var url = BuildApiUrl(address);
                _logger.LogDebug("Requesting USDT balance for address: {Address}", address);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var httpResponse = await _httpClient.GetAsync(url, cancellationToken);
                    return httpResponse;
                });

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("BSCScan API Response: {Response}", responseContent);

                return await ProcessApiResponseAsync(responseContent, address);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Operation was cancelled for address: {Address}", address);
                throw;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Request timeout for address: {Address}", address);
                throw new TimeoutException($"Request timeout while fetching balance for address: {address}", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for address: {Address}", address);
                throw new InvalidOperationException($"BSCScan API request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching balance for address: {Address}", address);
                throw;
            }
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Universal Cold Wallet/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS);
        }

        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: MAX_RETRY_ATTEMPTS,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(RATE_LIMIT_DELAY_MS * retryAttempt),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry attempt {RetryCount} after {Delay}ms due to: {Reason}",
                            retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        private string BuildApiUrl(string address)
        {
            return $"{BASE_URL}" +
                   $"?module=account" +
                   $"&action=tokenbalance" +
                   $"&contractaddress={USDT_BSC_CONTRACT}" +
                   $"&address={address}" +
                   $"&tag=latest" +
                   $"&apikey={_apiKey}";
        }

        private Task<decimal> ProcessApiResponseAsync(string responseContent, string address)
        {
            JObject json;
            try
            {
                json = JObject.Parse(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse API response for address: {Address}", address);
                throw new InvalidOperationException("Invalid JSON response from BSCScan API", ex);
            }

            var status = json["status"]?.ToString();
            var message = json["message"]?.ToString();
            var result = json["result"]?.ToString();

            if (status == "1")
            {
                if (decimal.TryParse(result, out decimal rawBalance))
                {
                    var usdtBalance = rawBalance / USDT_DECIMALS;
                    _logger.LogInformation("USDT Balance for {Address}: {Balance} USDT", address, usdtBalance);
                    return Task.FromResult(usdtBalance);
                }

                _logger.LogError("Invalid balance format for address: {Address}. Raw value: {RawValue}", address, result);
                throw new InvalidOperationException($"Invalid balance format. Raw value: {result}");
            }

            // Handle API errors
            if (status == "0")
            {
                var errorMessage = HandleApiError(message ?? "Unknown error");
                _logger.LogError("BSCScan API error for address: {Address} - {Error}", address, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var genericError = $"BSCScan API error - Status: {status}, Message: {message ?? "No message"}, Result: {result ?? "No result"}";
            _logger.LogError("Generic API error for address: {Address} - {Error}", address, genericError);
            throw new InvalidOperationException(genericError);
        }

        private string HandleApiError(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Unknown API error";

            var lowerMessage = message.ToLower();

            if (lowerMessage.Contains("invalid api key"))
                return "Invalid BSCScan API key. Please check your API key configuration.";

            if (lowerMessage.Contains("rate limit"))
                return "BSCScan API rate limit exceeded. Please try again later.";

            if (lowerMessage.Contains("invalid address"))
                return "Invalid wallet address format.";

            return $"BSCScan API error: {message}";
        }

        private static bool IsValidBscAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // BSC addresses are 42 characters long and start with 0x
            if (address.Length != 42 || !address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if the rest contains only hexadecimal characters
            for (int i = 2; i < address.Length; i++)
            {
                char c = address[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }

            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(USDT_BSCCAccountBalance));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    // Extension methods for better error handling
    public static class UsdtBalanceExtensions
    {
        public static async Task<decimal?> TryGetBalanceAsync(this USDT_BSCCAccountBalance balanceService,
            string address, CancellationToken cancellationToken = default)
        {
            try
            {
                return await balanceService.GetBalanceAsync(address, cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}