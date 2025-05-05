
using BTCPayServer.Lightning;
using System.Collections.Generic;
public class LspChannelsViewModel
{
    public string StoreId { get; set; }
    public string CryptoCode { get; set; }
    public LightMoney TotalLocalBalance { get; set; }
    public List<LspChannelInfo> LspChannels { get; set; } = new();
    public bool IsConnectedToLsp { get; set; }

    public class LspChannelInfo
    {
        public string ChannelId { get; set; }
        public LightMoney LocalBalance { get; set; }
        public LightMoney RemoteBalance { get; set; }
    }
}