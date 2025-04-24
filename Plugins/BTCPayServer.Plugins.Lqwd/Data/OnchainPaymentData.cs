
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Lqwd.Data
{
    public class OnchainPaymentData
    {
        [Key]
        public int Id { get; set; }
        public string State { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public long FeeTotalSat { get; set; }
        public long OrderTotalSat { get; set; }
        public string OnchainAddress { get; set; }
        public int MinOnchainPaymentConfirmations { get; set; }
        public int MinFeeFor0Conf { get; set; }
    }


}