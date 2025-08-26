
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BTCPayServer.Plugins.Lqwd.Data
{
    public class ChannelData
    {
        [Key]
        public int Id { get; set; }
        public string FundedAt { get; set; }
        public string FundingOutpoint { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public sealed class CloseChannelRequest
    {
        [JsonProperty("channel_point")]
        public string ChannelPoint { get; set; } = "";
    }

    public sealed class CloseChannelResult
    {
        public string Status { get; set; } = "unknown";
        public string? ClosingTxId { get; set; }
    }


}