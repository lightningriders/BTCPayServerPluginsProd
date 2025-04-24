

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class OrderRequest
{
    [JsonProperty("lsp_balance_sat")]
    [JsonConverter(typeof(ToStringJsonConverter))]
    public long LspBalanceSat { get; set; }

    [JsonProperty("client_balance_sat")]
    [JsonConverter(typeof(ToStringJsonConverter))]
    public long ClientBalanceSat { get; set; }

    [JsonProperty("channel_expiry_blocks")]
    public int ChannelExpiryBlocks { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }

    [JsonProperty("refund_onchain_address")]
    public string RefundOnchainAddress { get; set; }

    [JsonProperty("announce_channel")]
    public bool AnnounceChannel { get; set; }

    [JsonProperty("required_channel_confirmations")]
    public int RequiredChannelConfirmations { get; set; }

    [JsonProperty("funding_confirms_within_blocks")]
    public int FundingConfirmsWithinBlocks { get; set; }

    [JsonProperty("public_key")]
    public string PublicKey { get; set; }
}



