using System.ServiceModel;

namespace CurrencyExchangeService
{
    [ServiceContract]
    public interface ICurrencyExchangeService
    {
        [OperationContract]
        double GetExchangeRate(string currencyCode);
    }
}
