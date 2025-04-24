
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Lqwd.Data
{

    public class Bolt11PaymentData
    {
        [Key]
        public int Id { get; set; }
        public string State { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public long FeeTotalSat { get; set; }
        public long OrderTotalSat { get; set; }
        public string Invoice { get; set; }
    }

}