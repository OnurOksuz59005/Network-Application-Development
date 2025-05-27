using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CurrencyExchangeClient
{
    // Import the service contract
    [ServiceContract]
    public interface ICurrencyExchangeService
    {
        [OperationContract]
        double GetExchangeRate(string currencyCode);
    }

    class Program
    {
        private static readonly Dictionary<string, string> CommonCurrencies = new Dictionary<string, string>
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

        // Direct API access as fallback
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string NBP_API_URL = "http://api.nbp.pl/api/exchangerates/rates/a/";
        private static bool _usingDirectApi = false;

        static void Main(string[] args)
        {
            PrintHeader();

            ICurrencyExchangeService client = null;
            ChannelFactory<ICurrencyExchangeService> channelFactory = null;
            bool serviceConnected = TryConnectToService(out client, out channelFactory);
            
            if (!serviceConnected)
            {
                // If WCF service connection fails, use direct API access
                Console.WriteLine("\nFalling back to direct NBP API access...");
                _usingDirectApi = true;
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            }

            try
            {
                RunMainLoop(client);
            }
            finally
            {
                // Clean up WCF channel and factory
                if (channelFactory != null && channelFactory.State != CommunicationState.Closed)
                {
                    try { channelFactory.Close(); } catch { channelFactory.Abort(); }
                }
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static bool TryConnectToService(out ICurrencyExchangeService client, out ChannelFactory<ICurrencyExchangeService> channelFactory)
        {
            client = null;
            channelFactory = null;

            try
            {
                Console.WriteLine("Connecting to Currency Exchange Service...");
                
                // Create TCP binding explicitly
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                binding.MaxReceivedMessageSize = 2000000;
                binding.OpenTimeout = TimeSpan.FromSeconds(5);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(10);
                binding.SendTimeout = TimeSpan.FromSeconds(10);
                
                // Create endpoint address - IMPORTANT: Using net.tcp protocol to match NetTcpBinding
                EndpointAddress endpoint = new EndpointAddress("net.tcp://localhost:8733/CurrencyExchangeService/");
                
                // Create channel factory
                channelFactory = new ChannelFactory<ICurrencyExchangeService>(binding, endpoint);
                client = channelFactory.CreateChannel();
                
                // Enable reliable session behavior for the channel
                IClientChannel channel = client as IClientChannel;
                if (channel != null)
                {
                    channel.OperationTimeout = TimeSpan.FromSeconds(30);
                }
                
                // Test connection
                Console.WriteLine("Testing service connection...");
                double testRate = client.GetExchangeRate("EUR");
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Connected successfully to WCF service!");
                Console.ResetColor();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Service connection failed: {ex.Message}");
                Console.WriteLine("Make sure the WCF service is running.");
                Console.ResetColor();
                
                // Clean up if connection failed
                if (channelFactory != null && channelFactory.State != CommunicationState.Closed)
                {
                    try { channelFactory.Close(); } catch { channelFactory.Abort(); }
                }
                
                return false;
            }
        }

        private static void RunMainLoop(ICurrencyExchangeService wcfClient)
        {
            while (true)
            {
                ShowMenu();
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "exit" || input.ToLower() == "q")
                    break;

                if (input.ToLower() == "list" || input == "?")
                {
                    ShowAvailableCurrencies();
                    continue;
                }

                string currencyCode = input.ToUpper();
                
                try
                {
                    double rate;
                    
                    if (_usingDirectApi)
                    {
                        // Use direct API access
                        rate = GetExchangeRateFromApi(currencyCode).GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Use WCF service
                        rate = wcfClient.GetExchangeRate(currencyCode);
                    }
                    
                    DisplayExchangeRate(currencyCode, rate);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        
        private static async Task<double> GetExchangeRateFromApi(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                throw new ArgumentException("Currency code cannot be null or empty");
            }

            try
            {
                string apiUrl = $"{NBP_API_URL}{currencyCode.ToLower()}/?format=json";
                Console.WriteLine($"Fetching data from: {apiUrl}");
                
                string response = await _httpClient.GetStringAsync(apiUrl);
                var exchangeRateData = JsonConvert.DeserializeObject<ExchangeRateResponse>(response);
                
                if (exchangeRateData != null && exchangeRateData.Rates != null && exchangeRateData.Rates.Count > 0)
                {
                    return exchangeRateData.Rates[0].Mid;
                }
                
                throw new Exception($"Failed to parse exchange rate data for {currencyCode}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error connecting to NBP API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error parsing NBP API response: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }
        
        private static void DisplayExchangeRate(string currencyCode, double rate)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nExchange rate for {currencyCode}:\n1 {currencyCode} = {rate:F4} PLN");
            Console.ResetColor();
            
            // Show conversion examples
            Console.WriteLine("Examples:");
            Console.WriteLine($"10 {currencyCode} = {10 * rate:F2} PLN");
            Console.WriteLine($"100 {currencyCode} = {100 * rate:F2} PLN");
            Console.WriteLine($"1000 PLN = {1000 / rate:F2} {currencyCode}");
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine("║      CURRENCY EXCHANGE CLIENT      ║");
            Console.WriteLine("╚════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void ShowMenu()
        {
            Console.WriteLine("\nEnter a currency code (e.g., EUR, USD, GBP)");
            Console.WriteLine("Or type 'list' to see available currencies");
            Console.WriteLine("Or type 'exit' to quit");
            Console.Write(">> ");
        }

        private static void ShowAvailableCurrencies()
        {
            Console.WriteLine("\nCommon Currency Codes:\n");
            Console.WriteLine("┌──────┬────────────────────┐");
            Console.WriteLine("│ Code │ Currency           │");
            Console.WriteLine("├──────┼────────────────────┤");
            
            foreach (var currency in CommonCurrencies)
            {
                Console.WriteLine($"│ {currency.Key,-4} │ {currency.Value,-18} │");
            }
            
            Console.WriteLine("└──────┴────────────────────┘");
            Console.WriteLine("\nNote: You can query any currency code supported by the NBP API");
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
