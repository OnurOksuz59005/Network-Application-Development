using System;
using System.IO;
using System.Net.Http;
using System.ServiceModel;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace CurrencyExchangeService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ExchangeRateRepository _repository;
        private const string NBP_API_URL = "http://api.nbp.pl/api/exchangerates/rates/a/";

        public CurrencyExchangeService()
        {
            // Set DataDirectory to a folder in AppData that we can write to
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CurrencyExchangeService");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            // Set the DataDirectory for Entity Framework
            AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);
            LogInfo($"Database directory set to: {appDataPath}");
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout to 30 seconds
            
            // Initialize repository
            _repository = new ExchangeRateRepository();
            
            // Initialize common currencies in the database
            var commonCurrencies = new Dictionary<string, string>
            {
                { "EUR", "Euro" },
                { "USD", "US Dollar" },
                { "GBP", "British Pound" },
                { "CHF", "Swiss Franc" },
                { "JPY", "Japanese Yen" },
                { "CAD", "Canadian Dollar" },
                { "AUD", "Australian Dollar" },
                { "CZK", "Czech Koruna" },
                { "SEK", "Swedish Krona" },
                { "NOK", "Norwegian Krone" }
            };
            
            _repository.InitializeCurrencyInfo(commonCurrencies);
        }
        
        public List<Models.CurrencyInfo> GetAvailableCurrencies()
        {
            try
            {
                LogInfo("Getting available currencies from repository");
                var currencies = _repository.GetAllCurrencies();
                
                // Convert from repository type to service interface type
                var result = new List<Models.CurrencyInfo>();
                foreach (var currency in currencies)
                {
                    result.Add(new Models.CurrencyInfo
                    {
                        CurrencyCode = currency.CurrencyCode,
                        CurrencyName = currency.CurrencyName
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error getting available currencies: {ex.Message}");
                throw new FaultException(new FaultReason($"Error retrieving currency list: {ex.Message}"));
            }
        }
        
        public List<Models.ExchangeRateHistory> GetExchangeRateHistory(string currencyCode, int days)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                LogError("Invalid currency code for history");
                throw new FaultException(new FaultReason("Currency code cannot be null or empty"));
            }
            
            // Validate that the currency code is not 'HISTORY' itself
            if (currencyCode.Equals("HISTORY", StringComparison.OrdinalIgnoreCase))
            {
                LogError("'HISTORY' is not a valid currency code");
                throw new FaultException(new FaultReason("'HISTORY' is not a valid currency code. Please use a valid 3-letter currency code like EUR, USD, etc."));
            }
            
            if (days < 1 || days > 30)
            {
                LogError($"Invalid number of days: {days}");
                throw new FaultException(new FaultReason("Number of days must be between 1 and 30"));
            }
            
            try
            {
                LogInfo($"Service: Getting exchange rate history for {currencyCode} for {days} days");
                
                // First check if we have history in our repository
                var history = _repository.GetExchangeRateHistory(currencyCode, days);
                LogInfo($"Service: Repository returned {history.Count} records initially");
                
                // If we don't have enough history, fetch it from the API
                if (history.Count < days)
                {
                    LogInfo($"Service: Fetching historical data for {currencyCode} for the last {days} days");
                    FetchHistoricalRates(currencyCode, days).Wait();
                    
                    // Get the history again after fetching from API
                    history = _repository.GetExchangeRateHistory(currencyCode, days);
                    LogInfo($"Service: Repository returned {history.Count} records after API fetch");
                }
                
                // Log what we're returning to the client
                LogInfo($"Service: Returning {history.Count} historical records to client");
                
                // Create a new list with explicit conversion to ensure WCF serialization works correctly
                var result = new List<Models.ExchangeRateHistory>();
                foreach (var item in history)
                {
                    LogInfo($"Service: Historical rate: {item.Date:yyyy-MM-dd} - {item.Rate} PLN");
                    // Make sure we're creating a new instance with proper values
                    var historyItem = new Models.ExchangeRateHistory
                    {
                        Date = item.Date,
                        Rate = item.Rate,
                        TableNumber = item.TableNumber ?? "N/A" // Ensure TableNumber is never null
                    };
                    result.Add(historyItem);
                    
                    // Log the actual object we're adding to the result
                    LogInfo($"Service: Added to result: Date={historyItem.Date:yyyy-MM-dd}, Rate={historyItem.Rate}, Table={historyItem.TableNumber}");
                }
                
                LogInfo($"Service: Final result count: {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error getting exchange rate history for {currencyCode}: {ex.Message}");
                throw new FaultException(new FaultReason($"Error retrieving exchange rate history: {ex.Message}"));
            }
        }
        
        private async Task FetchHistoricalRates(string currencyCode, int days)
        {
            try
            {
                // Validate currency code to prevent using commands as currency codes
                if (string.IsNullOrEmpty(currencyCode) || currencyCode.Equals("HISTORY", StringComparison.OrdinalIgnoreCase))
                {
                    LogError($"Invalid currency code for historical data: {currencyCode}");
                    throw new ArgumentException($"Invalid currency code: {currencyCode}");
                }
                
                // Construct the correct URL for historical data
                // The NBP API format for historical data is: /exchangerates/rates/a/{code}/last/{topCount}/
                // Make sure we're using the correct endpoint for historical data
                string apiUrl = $"http://api.nbp.pl/api/exchangerates/rates/a/{currencyCode.ToLower()}/last/{days}/?format=json";
                LogInfo($"Fetching historical data from: {apiUrl}");
                
                try
                {
                    string response = await _httpClient.GetStringAsync(apiUrl);
                    LogInfo($"Received response from API: {response.Substring(0, Math.Min(100, response.Length))}...");
                    
                    var exchangeRateData = JsonConvert.DeserializeObject<ExchangeRateResponse>(response);
                    
                    if (exchangeRateData != null && exchangeRateData.Rates != null && exchangeRateData.Rates.Count > 0)
                    {
                        LogInfo($"Parsed {exchangeRateData.Rates.Count} rates from API response");
                        
                        foreach (var rate in exchangeRateData.Rates)
                        {
                            LogInfo($"Saving rate for {currencyCode} on {rate.EffectiveDate}: {rate.Mid}");
                            _repository.SaveExchangeRate(
                                currencyCode,
                                rate.Mid,
                                rate.EffectiveDate,
                                rate.No);
                        }
                        
                        LogInfo($"Successfully saved {exchangeRateData.Rates.Count} historical rates for {currencyCode}");
                    }
                    else
                    {
                        LogError($"No rates found in API response for {currencyCode}");
                    }
                }
                catch (HttpRequestException hex)
                {
                    LogError($"HTTP request error fetching historical data for {currencyCode}: {hex.Message}");
                    throw;
                }
                catch (JsonException jex)
                {
                    LogError($"JSON parsing error for {currencyCode}: {jex.Message}");
                    throw;
                }
            }
            catch (HttpRequestException ex)
            {
                LogError($"HTTP error fetching historical data for {currencyCode}: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                LogError($"Error parsing historical data for {currencyCode}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogError($"Error fetching historical data for {currencyCode}: {ex.Message}");
                throw;
            }
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
                // First check if we have a valid cached rate in the database
                // Get the latest exchange rate from our repository
                var rates = _repository.GetExchangeRateHistory(currencyCode, 1);
                
                // If we have at least one rate in history, use it
                if (rates.Count > 0)
                {
                    LogInfo($"Using cached exchange rate for {currencyCode} from {rates[0].Date}: {rates[0].Rate} PLN");
                    return rates[0].Rate;
                }
                // No cached rate found, continue to API call
                
                // If no valid cached rate, make a call to the NBP API
                string apiUrl = $"{NBP_API_URL}{currencyCode.ToLower()}/?format=json";
                LogInfo($"No valid cached data. Calling NBP API: {apiUrl}");
                
                var response = _httpClient.GetStringAsync(apiUrl).GetAwaiter().GetResult();
                var exchangeRateData = JsonConvert.DeserializeObject<ExchangeRateResponse>(response);
                
                if (exchangeRateData != null && exchangeRateData.Rates != null && exchangeRateData.Rates.Any())
                {
                    var rateData = exchangeRateData.Rates.First();
                    double rate = rateData.Mid;
                    
                    // Save the new rate to the database
                    _repository.SaveExchangeRate(
                        currencyCode, 
                        rate, 
                        rateData.EffectiveDate, 
                        rateData.No);
                    
                    LogInfo($"Exchange rate for {currencyCode}: {rate} PLN (saved to database)");
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
