using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using Newtonsoft.Json;
using System.Linq;

namespace CurrencyExchangeClient
{
    // Import the service contract
    [ServiceContract(Namespace = "http://tempuri.org/")]
    public interface ICurrencyExchangeService
    {
        [OperationContract]
        double GetExchangeRate(string currencyCode);
        
        [OperationContract]
        List<Models.CurrencyInfo> GetAvailableCurrencies();
        
        [OperationContract]
        List<Models.ExchangeRateHistory> GetExchangeRateHistory(string currencyCode, int days);
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
                
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                binding.MaxReceivedMessageSize = 2000000;
                binding.OpenTimeout = TimeSpan.FromSeconds(5);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(10);
                binding.SendTimeout = TimeSpan.FromSeconds(10);
                
                EndpointAddress endpoint = new EndpointAddress("net.tcp://localhost:8733/CurrencyExchangeService/");
                
                channelFactory = new ChannelFactory<ICurrencyExchangeService>(binding, endpoint);
                client = channelFactory.CreateChannel();
                
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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\nEnter currency code (or 'list' for available currencies, 'history' for rate history, 'exit' to quit): ");
                Console.ResetColor();
                
                string input = Console.ReadLine().Trim().ToUpper();
                
                if (input == "EXIT")
                {
                    break;
                }
                else if (input == "LIST")
                {
                    if (!_usingDirectApi)
                    {
                        try
                        {
                            var currencies = wcfClient.GetAvailableCurrencies();
                            DisplayAvailableCurrencies(currencies);
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error getting currencies from service: {ex.Message}");
                            Console.ResetColor();
                            ShowAvailableCurrencies(); // Fall back to local list
                        }
                    }
                    else
                    {
                        ShowAvailableCurrencies(); // Use local list
                    }
                    continue;
                }
                else if (input == "HISTORY")
                {
                    GetExchangeRateHistory(wcfClient);
                    continue;
                }
                
                // Handle currency code input
                try
                {
                    double rate;
                    
                    if (_usingDirectApi)
                    {
                        // Use direct API access
                        rate = GetExchangeRateFromApi(input).GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Use WCF service
                        rate = wcfClient.GetExchangeRate(input);
                    }
                    
                    DisplayExchangeRate(input, rate);
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

        private static void GetExchangeRateHistory(ICurrencyExchangeService client)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nEnter currency code for history: ");
            Console.ResetColor();
            string currencyCode = Console.ReadLine().Trim().ToUpper();
            
            // Validate currency code
            if (string.IsNullOrEmpty(currencyCode))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Currency code cannot be empty.");
                Console.ResetColor();
                return;
            }
            
            // Check if the user entered "HISTORY" as the currency code
            if (currencyCode == "HISTORY")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("'HISTORY' is not a valid currency code. Please enter a valid currency code like EUR, USD, etc.");
                Console.ResetColor();
                return;
            }
            
            // Validate that the currency code is a valid ISO currency code (3 letters)
            if (currencyCode.Length != 3 || !currencyCode.All(char.IsLetter))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please enter a valid 3-letter currency code (e.g., EUR, USD, GBP).");
                Console.ResetColor();
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Enter number of days (1-30): ");
            Console.ResetColor();
            
            if (!int.TryParse(Console.ReadLine().Trim(), out int days) || days < 1 || days > 30)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid number of days. Please enter a number between 1 and 30.");
                Console.ResetColor();
                return;
            }
            
            try
            {
                if (_usingDirectApi)
                {
                    // Direct API access for history not implemented in this version
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Exchange rate history is only available when connected to the WCF service.");
                    Console.ResetColor();
                    return;
                }
                
                Console.WriteLine($"Requesting exchange rate history for {currencyCode} for {days} days...");
                
                List<Models.ExchangeRateHistory> history = null;
                try 
                {
                    IClientChannel channel = client as IClientChannel;
                    if (channel != null && channel.State != CommunicationState.Opened)
                    {
                        Console.WriteLine("Warning: Channel is not in Open state. Current state: " + channel.State);
                    }
                    
                    try
                    {
                        var binding = new NetTcpBinding();
                        binding.MaxReceivedMessageSize = 2147483647; // Max size
                        binding.ReaderQuotas.MaxArrayLength = 2147483647;
                        binding.ReaderQuotas.MaxStringContentLength = 2147483647;
                        binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
                        binding.ReaderQuotas.MaxDepth = 64;
                        binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
                        
                        var endpoint = new EndpointAddress("net.tcp://localhost:8733/CurrencyExchangeService/");
                        var factory = new ChannelFactory<ICurrencyExchangeService>(binding, endpoint);
                        var freshClient = factory.CreateChannel();
                        
                        Console.WriteLine("Created fresh client channel for history request");
                        
                        history = freshClient.GetExchangeRateHistory(currencyCode, days);
                        Console.WriteLine($"Received response from service with {(history != null ? history.Count : 0)} records");
                        
                        if (history != null && history.Count > 0)
                        {
                            Console.WriteLine("History items received from service:");
                            foreach (var item in history)
                            {
                                Console.WriteLine($"Date={item.Date:yyyy-MM-dd}, Rate={item.Rate}, Table={item.TableNumber}");
                            }
                        }
                        
                        ((ICommunicationObject)freshClient).Close();
                    }
                    catch (FaultException fex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Service fault in fresh channel: {fex.Message}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error with fresh channel: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        Console.ResetColor();
                    }
                }
                catch (FaultException fex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Service fault: {fex.Message}");
                    Console.ResetColor();
                    return;
                }
                catch (TimeoutException tex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Timeout error: {tex.Message}");
                    Console.ResetColor();
                    return;
                }
                catch (CommunicationException cex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Communication error: {cex.Message}");
                    Console.ResetColor();
                    return;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Service call error: {ex.Message}");
                    Console.ResetColor();
                    return;
                }
                
                if (history == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"No historical data returned for {currencyCode} (null response).");
                    Console.ResetColor();
                    return;
                }
                
                if (history.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"No historical data found for {currencyCode} (empty list).");
                    Console.ResetColor();
                    return;
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nExchange rate history for {currencyCode} (last {days} days):");
                Console.ResetColor();
                
                Console.WriteLine("{0,-12} {1,-10} {2}", "Date", "Rate (PLN)", "Table");
                Console.WriteLine(new string('-', 40));
                
                List<Models.ExchangeRateHistory> historyItems = new List<Models.ExchangeRateHistory>();
                
                foreach (var item in history)
                {
                    historyItems.Add(new Models.ExchangeRateHistory
                    {
                        Date = item.Date,
                        Rate = item.Rate,
                        TableNumber = item.TableNumber
                    });
                }
                
                historyItems.Sort(delegate(Models.ExchangeRateHistory x, Models.ExchangeRateHistory y) {
                    return y.Date.CompareTo(x.Date); // Descending order
                });
                // Display the sorted history
                foreach (var rate in historyItems)
                {
                    Console.WriteLine("{0,-12} {1,-10:F4} {2}", 
                        rate.Date.ToString("yyyy-MM-dd"), 
                        rate.Rate, 
                        rate.TableNumber);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error retrieving exchange rate history: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.ResetColor();
            }
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
            Console.WriteLine("\nAvailable currencies:");
            Console.WriteLine("{0,-5} {1}", "Code", "Name");
            Console.WriteLine(new string('-', 30));
            
            foreach (var currency in CommonCurrencies)
            {
                Console.WriteLine("{0,-5} {1}", currency.Key, currency.Value);
            }
            
            Console.WriteLine("\nNote: You can also try other ISO 4217 currency codes.");
            Console.WriteLine("The NBP API supports most major world currencies.");
        }

        private static void DisplayAvailableCurrencies(List<Models.CurrencyInfo> currencies)
        {
            Console.WriteLine("\nAvailable currencies:");
            Console.WriteLine("{0,-5} {1}", "Code", "Name");
            Console.WriteLine(new string('-', 30));
            
            if (currencies != null && currencies.Any())
            {
                // Display currencies from the database
                foreach (var currency in currencies.OrderBy(c => c.CurrencyCode))
                {
                    Console.WriteLine("{0,-5} {1}", currency.CurrencyCode, currency.CurrencyName);
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nCurrency information retrieved from database.");
                Console.ResetColor();
            }
            else
            {
                // Fall back to local list
                ShowAvailableCurrencies();
            }
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
