using System;
using System.Runtime.Serialization;

namespace CurrencyExchangeService.Models
{
    [DataContract(Namespace = "http://tempuri.org/")]
    public class ExchangeRateHistory
    {
        [DataMember]
        public DateTime Date { get; set; }
        
        [DataMember]
        public double Rate { get; set; }
        
        [DataMember]
        public string TableNumber { get; set; }
    }
}
