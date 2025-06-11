using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace CurrencyExchangeService.Models
{
    [Table("ExchangeRates")]
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; }
        
        [Required]
        public double Rate { get; set; }
        
        [Required]
        public DateTime FetchDate { get; set; }
        
        [Required]
        public DateTime EffectiveDate { get; set; }
        
        public string TableNumber { get; set; }
    }

    [Table("CurrencyInfo")]
    [DataContract(Namespace = "http://tempuri.org/")]
    public class CurrencyInfo
    {
        [Key]
        [StringLength(3)]
        [DataMember]
        public string CurrencyCode { get; set; }
        
        [Required]
        [StringLength(100)]
        [DataMember]
        public string CurrencyName { get; set; }
        
        [DataMember]
        public bool IsCommon { get; set; }
    }

    [Table("QueryLogs")]
    public class QueryLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; }
        
        [Required]
        public DateTime QueryTime { get; set; }
        
        public bool WasFromCache { get; set; }
        
        [StringLength(200)]
        public string ClientInfo { get; set; }
    }
}
