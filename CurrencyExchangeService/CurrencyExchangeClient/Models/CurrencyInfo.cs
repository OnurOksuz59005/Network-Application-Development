using System.Runtime.Serialization;

namespace CurrencyExchangeClient.Models
{
    [DataContract(Namespace = "http://tempuri.org/")]
    public class CurrencyInfo
    {
        [DataMember]
        public string CurrencyCode { get; set; }
        
        [DataMember]
        public string CurrencyName { get; set; }
        
        [DataMember]
        public bool IsCommon { get; set; }
    }
}
