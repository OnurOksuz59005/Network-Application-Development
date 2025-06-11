using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;

namespace CurrencyExchangeService.Models
{
    public class ExchangeRateRepository
    {
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24); // Cache for 24 hours by default
        
        public ExchangeRate GetLatestExchangeRate(string currencyCode)
        {
            using (var context = new CurrencyExchangeContext())
            {
                // Find the most recent exchange rate for the currency
                var latestRate = context.ExchangeRates
                    .Where(r => r.CurrencyCode == currencyCode.ToUpper())
                    .OrderByDescending(r => r.FetchDate)
                    .FirstOrDefault();
                
                // Check if rate exists and is not expired
                if (latestRate != null && DateTime.Now.Subtract(latestRate.FetchDate) <= _cacheExpiration)
                {
                    // Log that we're using a cached value
                    Console.WriteLine($"Using cached exchange rate for {currencyCode}");
                    return latestRate;
                }
                
                return null; // No valid cached rate found
            }
        }
        
        public void SaveExchangeRate(string currencyCode, double rate, DateTime effectiveDate, string tableNumber)
        {
            try
            {
                using (var context = new CurrencyExchangeContext())
                {
                    // Check if we already have this exact rate in the database to avoid duplicates
                    var existingRate = context.ExchangeRates
                        .FirstOrDefault(r => r.CurrencyCode == currencyCode.ToUpper() && 
                                        r.EffectiveDate == effectiveDate &&
                                        r.TableNumber == tableNumber);
                    
                    if (existingRate != null)
                    {
                        Console.WriteLine($"Rate for {currencyCode} on {effectiveDate:yyyy-MM-dd} already exists in database. Skipping.");
                        return;
                    }
                    
                    var exchangeRate = new ExchangeRate
                    {
                        CurrencyCode = currencyCode.ToUpper(),
                        Rate = rate,
                        FetchDate = DateTime.Now,
                        EffectiveDate = effectiveDate,
                        TableNumber = tableNumber
                    };
                    
                    Console.WriteLine($"Adding new rate to database: {currencyCode} on {effectiveDate:yyyy-MM-dd} = {rate} PLN");
                    context.ExchangeRates.Add(exchangeRate);
                    
                    // Save changes and report success
                    int rowsAffected = context.SaveChanges();
                    Console.WriteLine($"Database save completed. Rows affected: {rowsAffected}");
                    
                    // Verify the save by retrieving the record
                    var savedRate = context.ExchangeRates
                        .FirstOrDefault(r => r.CurrencyCode == currencyCode.ToUpper() && 
                                        r.EffectiveDate == effectiveDate &&
                                        r.TableNumber == tableNumber);
                    
                    if (savedRate != null)
                    {
                        Console.WriteLine($"Verified: Rate for {currencyCode} on {effectiveDate:yyyy-MM-dd} saved successfully with ID {savedRate.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Could not verify saved rate for {currencyCode} on {effectiveDate:yyyy-MM-dd}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving exchange rate: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            
            // Log that we've saved a new exchange rate
            Console.WriteLine($"Saved new exchange rate for {currencyCode}");
        }
        
        public void InitializeCurrencyInfo(Dictionary<string, string> currencies)
        {
            using (var context = new CurrencyExchangeContext())
            {
                // Only add currencies if the table is empty
                if (!context.CurrencyInfo.Any())
                {
                    foreach (var currency in currencies)
                    {
                        context.CurrencyInfo.Add(new CurrencyInfo
                        {
                            CurrencyCode = currency.Key,
                            CurrencyName = currency.Value,
                            IsCommon = true
                        });
                    }
                    
                    context.SaveChanges();
                }
            }
        }
        
        // Helper method to log exchange rate queries to the database
        private void SaveQueryLog(string currencyCode, bool wasFromCache)
        {
            try
            {
                using (var context = new CurrencyExchangeContext())
                {
                    var log = new QueryLog
                    {
                        CurrencyCode = currencyCode.ToUpper(),
                        QueryTime = DateTime.Now,
                        WasFromCache = wasFromCache,
                        ClientInfo = System.ServiceModel.OperationContext.Current?.Channel?.RemoteAddress?.ToString() ?? "Unknown"
                    };
                    
                    context.QueryLogs.Add(log);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Logging should not break the application if it fails
                Console.WriteLine($"Error saving query log: {ex.Message}");
            }
        }
        
        public List<CurrencyInfo> GetAllCurrencies()
        {
            using (var context = new CurrencyExchangeContext())
            {
                return context.CurrencyInfo.OrderBy(c => c.CurrencyCode).ToList();
            }
        }
        
        public List<QueryLog> GetRecentQueries(int count = 100)
        {
            using (var context = new CurrencyExchangeContext())
            {
                return context.QueryLogs
                    .OrderByDescending(q => q.QueryTime)
                    .Take(count)
                    .ToList();
            }
        }
        
        public List<ExchangeRateHistory> GetExchangeRateHistory(string currencyCode, int days)
        {
            using (var context = new CurrencyExchangeContext())
            {
                // Log what we're trying to retrieve
                Console.WriteLine($"Retrieving exchange rate history for {currencyCode} from database");
                Console.WriteLine($"Current date: {DateTime.Now}");
                
                // Get ALL rates for this currency to debug the issue
                var allRates = context.ExchangeRates
                    .Where(r => r.CurrencyCode == currencyCode.ToUpper())
                    .ToList();
                
                Console.WriteLine($"Total rates in database for {currencyCode}: {allRates.Count}");
                
                // Log all rates to see what's in the database
                foreach (var rate in allRates)
                {
                    Console.WriteLine($"Database rate: {currencyCode} on {rate.EffectiveDate:yyyy-MM-dd} = {rate.Rate} PLN (fetched on {rate.FetchDate:yyyy-MM-dd HH:mm:ss})");
                }
                
                // Get all rates without date filtering
                var rates = context.ExchangeRates
                    .Where(r => r.CurrencyCode == currencyCode.ToUpper())
                    .OrderByDescending(r => r.FetchDate) // Order by when we fetched it, not the effective date
                    .Take(days) // Limit to the requested number of days
                    .OrderBy(r => r.EffectiveDate) // Then reorder by effective date for display
                    .ToList();
                
                Console.WriteLine($"Found {rates.Count} historical rates in database for {currencyCode}");
                
                // Convert to the DTO format expected by the client
                var result = rates.Select(r => new ExchangeRateHistory
                {
                    Date = r.EffectiveDate,
                    Rate = r.Rate,
                    TableNumber = r.TableNumber
                }).ToList();
                
                return result;
            }
        }
    }
}
