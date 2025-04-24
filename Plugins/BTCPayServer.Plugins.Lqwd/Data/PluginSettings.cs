using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTCPayServer.Plugins.Lqwd.Data;

public class PluginSettings
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    public string StoreId { get; set; }
    public string Key { get; set; }

    public string Value { get; set; }
}
