
using System;
using System.ComponentModel.DataAnnotations;

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


}