using System;
using System.Net.Http;
using System.ServiceModel;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace CurrencyExchangeService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private const string NBP_API_URL = "http://api.nbp.pl/api/exchangerates/rates/a/";

        public CurrencyExchangeService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout to 30 seconds
        }

        public double GetExchangeRate(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                LogError($"Invalid currency code: {currencyCode}");
                throw new FaultException<ArgumentException>(
                    new ArgumentException("Currency code cannot be null or empty"), 
                    new FaultReason("Invalid currency code provided"));
            }

            LogInfo($"Getting exchange rate for currency: {currencyCode}");

            try
            {
                // Make a synchronous call to the NBP API
                string apiUrl = $"{NBP_API_URL}{currencyCode.ToLower()}/?format=json";
                LogInfo($"Calling NBP API: {apiUrl}");
                
                var response = _httpClient.GetStringAsync(apiUrl).GetAwaiter().GetResult();
                var exchangeRateData = JsonConvert.DeserializeObject<ExchangeRateResponse>(response);
                
                if (exchangeRateData != null && exchangeRateData.Rates != null && exchangeRateData.Rates.Any())
                {
                    double rate = exchangeRateData.Rates.First().Mid;
                    LogInfo($"Exchange rate for {currencyCode}: {rate} PLN");
                    return rate;
                }
                
                LogError($"Failed to parse exchange rate data for {currencyCode}");
                throw new FaultException(new FaultReason($"Failed to get exchange rate for {currencyCode}"));
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP request error for {currencyCode}: {ex.Message}");
                throw new FaultException(new FaultReason($"Error connecting to NBP API: {ex.Message}"));
            }
            catch (JsonException ex)
            {
                LogError($"JSON parsing error for {currencyCode}: {ex.Message}");
                throw new FaultException(new FaultReason($"Error parsing NBP API response: {ex.Message}"));
            }
            catch (FaultException)
            {
                // Re-throw FaultExceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error for {currencyCode}: {ex.Message}");
                throw new FaultException(new FaultReason($"Unexpected error: {ex.Message}"));
            }
        }
        
        private void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
            Trace.TraceInformation(message);
        }
        
        private void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            Trace.TraceError(message);
        }
    }

    public class ExchangeRateResponse
    {
        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("rates")]
        public List<Rate> Rates { get; set; }
    }

    public class Rate
    {
        [JsonProperty("no")]
        public string No { get; set; }

        [JsonProperty("effectiveDate")]
        public DateTime EffectiveDate { get; set; }

        [JsonProperty("mid")]
        public double Mid { get; set; }
    }
}
