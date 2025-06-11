using System.Collections.Generic;
using System.ServiceModel;
using CurrencyExchangeService.Models;

namespace CurrencyExchangeService
{
    [ServiceContract]
    public interface ICurrencyExchangeService
    {
        [OperationContract]
        double GetExchangeRate(string currencyCode);
        
        [OperationContract]
        List<Models.CurrencyInfo> GetAvailableCurrencies();
        
        [OperationContract]
        List<Models.ExchangeRateHistory> GetExchangeRateHistory(string currencyCode, int days);
    }
}
