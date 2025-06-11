using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CurrencyExchangeService
{
    // Simple in-memory repository to replace the database-backed one
    public class ExchangeRateRepository
    {
        private readonly Dictionary<string, CachedExchangeRate> _cachedRates = new Dictionary<string, CachedExchangeRate>();
        private readonly Dictionary<string, string> _currencyInfo = new Dictionary<string, string>();
        private readonly List<ExchangeRateHistoryRecord> _historyRecords = new List<ExchangeRateHistoryRecord>();
        
        // Cache expiration time in hours
        private const int CACHE_EXPIRATION_HOURS = 24;
        
        public ExchangeRateRepository()
        {
            // Initialize with some common currencies
            InitializeCurrencyInfo(new Dictionary<string, string>
            {
                { "EUR", "Euro" },
                { "USD", "US Dollar" },
                { "GBP", "British Pound" },
                { "CHF", "Swiss Franc" },
                { "JPY", "Japanese Yen" }
            });
        }
        
        public void InitializeCurrencyInfo(Dictionary<string, string> currencies)
        {
            foreach (var currency in currencies)
            {
                if (!_currencyInfo.ContainsKey(currency.Key))
                {
                    _currencyInfo[currency.Key] = currency.Value;
                }
            }
        }
        
        public CachedExchangeRate GetCachedRate(string currencyCode)
        {
            if (_cachedRates.TryGetValue(currencyCode, out var cachedRate))
            {
                // Check if cache is still valid
                if ((DateTime.Now - cachedRate.FetchDate).TotalHours < CACHE_EXPIRATION_HOURS)
                {
                    return cachedRate;
                }
            }
            
            return null;
        }
        
        public void SaveExchangeRate(string currencyCode, double rate, DateTime effectiveDate, string tableNumber)
        {
            // Update cache
            _cachedRates[currencyCode] = new CachedExchangeRate
            {
                CurrencyCode = currencyCode,
                Rate = rate,
                FetchDate = DateTime.Now,
                EffectiveDate = effectiveDate,
                TableNumber = tableNumber
            };
            
            // Add to history
            _historyRecords.Add(new ExchangeRateHistoryRecord
            {
                CurrencyCode = currencyCode,
                Rate = rate,
                Date = effectiveDate,
                TableNumber = tableNumber
            });
            
            // Add currency to info if not exists
            if (!_currencyInfo.ContainsKey(currencyCode))
            {
                _currencyInfo[currencyCode] = currencyCode;
            }
        }
        
        public List<CurrencyInfo> GetAllCurrencies()
        {
            return _currencyInfo.Select(c => new CurrencyInfo
            {
                CurrencyCode = c.Key,
                CurrencyName = c.Value,
                IsCommon = true
            }).ToList();
        }
        
        public List<ExchangeRateHistory> GetExchangeRateHistory(string currencyCode, int days)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            return _historyRecords
                .Where(r => r.CurrencyCode == currencyCode && r.Date >= startDate)
                .OrderByDescending(r => r.Date)
                .Select(r => new ExchangeRateHistory
                {
                    CurrencyCode = r.CurrencyCode,
                    Rate = r.Rate,
                    Date = r.Date,
                    TableNumber = r.TableNumber
                })
                .ToList();
        }
        
        public void LogQuery(string currencyCode, bool wasFromCache, string clientInfo)
        {
            // In-memory implementation doesn't persist logs
            Console.WriteLine($"[LOG] Query for {currencyCode} (FromCache: {wasFromCache}) by {clientInfo}");
        }
    }
    
    public class CachedExchangeRate
    {
        public string CurrencyCode { get; set; }
        public double Rate { get; set; }
        public DateTime FetchDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string TableNumber { get; set; }
    }
    
    public class ExchangeRateHistoryRecord
    {
        public string CurrencyCode { get; set; }
        public double Rate { get; set; }
        public DateTime Date { get; set; }
        public string TableNumber { get; set; }
    }
    
    [DataContract]
    public class CurrencyInfo
    {
        [DataMember]
        public string CurrencyCode { get; set; }
        
        [DataMember]
        public string CurrencyName { get; set; }
        
        [DataMember]
        public bool IsCommon { get; set; }
    }
    
    [DataContract]
    public class ExchangeRateHistory
    {
        [DataMember]
        public string CurrencyCode { get; set; }
        
        [DataMember]
        public double Rate { get; set; }
        
        [DataMember]
        public DateTime Date { get; set; }
        
        [DataMember]
        public string TableNumber { get; set; }
    }
}
