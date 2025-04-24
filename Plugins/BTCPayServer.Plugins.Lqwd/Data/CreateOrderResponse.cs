using System;
using Newtonsoft.Json;

namespace BTCPayServer.Plugins.Lqwd.Models
{
    public class CreateOrderResponse
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("lsp_balance_sat")]
        public long LspBalanceSat { get; set; }

        [JsonProperty("client_balance_sat")]
        public long ClientBalanceSat { get; set; }

        [JsonProperty("required_channel_confirmations")]
        public int RequiredChannelConfirmations { get; set; }

        [JsonProperty("funding_confirms_within_blocks")]
        public int FundingConfirmsWithinBlocks { get; set; }

        [JsonProperty("channel_expiry_blocks")]
        public int ChannelExpiryBlocks { get; set; }

        public string Token { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("expires_at")]
        public DateTimeOffset ExpiresAt { get; set; }

        [JsonProperty("announce_channel")]
        public bool AnnounceChannel { get; set; }

        [JsonProperty("order_state")]
        public string OrderState { get; set; }

        public PaymentResponse Payment { get; set; }
        public ChannelResponse Channel { get; set; }
    }

    public class PaymentResponse
    {
        public Bolt11Response Bolt11 { get; set; }
        public OnchainResponse Onchain { get; set; }
    }

    public class Bolt11Response
    {
        public string State { get; set; }
        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; }
        [JsonProperty("fee_total_sat")]
        public string FeeTotalSat { get; set; }
        [JsonProperty("order_total_sat")]
        public string OrderTotalSat { get; set; }
        public string Invoice { get; set; }
    }

    public class OnchainResponse
    {
        public string State { get; set; }
        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; }
        [JsonProperty("fee_total_sat")]
        public string FeeTotalSat { get; set; }
        [JsonProperty("order_total_sat")]
        public string OrderTotalSat { get; set; }
        [JsonProperty("onchain_address")]
        public string OnchainAddress { get; set; }
        [JsonProperty("min_onchain_payment_confirmations")]
        public int MinOnchainPaymentConfirmations { get; set; }
        [JsonProperty("min_fee_for_0conf")]
        public int MinFeeFor0Conf { get; set; }
    }

    public class ChannelResponse
    {
        [JsonProperty("funded_at")]
        public string FundedAt { get; set; }
        [JsonProperty("funding_outpoint")]
        public string FundingOutpoint { get; set; }
        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; }
    }
}
