
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Lqwd.Data
{
    public class PaymentData
    {
        [Key]
        public int Id { get; set; }
        public string OrderId { get; set; }

        public Bolt11PaymentData Bolt11 { get; set; }
        public OnchainPaymentData Onchain { get; set; }
    }

}