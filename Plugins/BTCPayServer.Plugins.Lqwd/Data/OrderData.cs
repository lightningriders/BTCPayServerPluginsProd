using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Lqwd.Data
{
    public class OrderData
    {
        [Key]
        public string OrderId { get; set; }
        public string StoreId { get; set; }
        public long LspBalanceSat { get; set; }
        public long ClientBalanceSat { get; set; }
        public int RequiredChannelConfirmations { get; set; }
        public int FundingConfirmsWithinBlocks { get; set; }
        public int ChannelExpiryBlocks { get; set; }
        public string Token { get; set; }
        public string RefundOnchainAddress { get; set; }
        public bool AnnounceChannel { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string OrderState { get; set; }

        public PaymentData Payment { get; set; }
        public ChannelData Channel { get; set; }
    }


}
