namespace BTCPayServer.Plugins.Lqwd.ViewModels;

using System.Collections.Generic;
using BTCPayServer.Plugins.Lqwd.Data;
using BTCPayServer.Lightning;

public class PluginPageViewModel
{
    public string ApiResponse { get; set; }
    public List<OrderData> Orders { get; set; }

    public LightningChannel[] Channels { get; set; }
}