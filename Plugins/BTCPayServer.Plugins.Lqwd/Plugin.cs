using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using BTCPayServer.Plugins.Lqwd.Services;
using Microsoft.Extensions.DependencyInjection;
using BTCPayServer.Lightning;


namespace BTCPayServer.Plugins.Lqwd;

public class Plugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=1.12.0" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddSingleton<IUIExtension>(new UIExtension("LqwdPluginHeaderNav", "header-nav"));
        services.AddHostedService<ApplicationPartsLogger>();
        services.AddHostedService<PluginMigrationRunner>();
        services.AddSingleton<LqwdPluginService>();
        // applicationBuilder.AddSingleton<ILightningConnectionStringHandler>(provider => provider.GetRequiredService<LqwdLightningConnectionStringHandler>());
        services.AddSingleton<LqwdPluginDbContextFactory>();
        services.AddDbContext<LqwdPluginDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<LqwdPluginDbContextFactory>();
            factory.ConfigureBuilder(o);
        });
    }
}
