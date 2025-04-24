namespace BTCPayServer.Plugins.Lqwd.ViewModels;

using System.Collections.Generic;
using BTCPayServer.Plugins.Lqwd.Data;

public class SettingsViewModel
{
    public List<PluginSettings> Settings { get; set; }
    public string ActiveLsps { get; set; }
}
