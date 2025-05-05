using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Plugins.Lqwd.Services
{
    public class ReconnectWorker : BackgroundService
    {
        private readonly LqwdPluginService _pluginService;
        private readonly ILogger<ReconnectWorker> _logger;

        public ReconnectWorker(LqwdPluginService pluginService, ILogger<ReconnectWorker> logger)
        {
            _pluginService = pluginService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[LQWD] ReconnectWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("[LQWD] Reconnecting to hardcoded LSP peers...");
                    await _pluginService.ReconnectToHardcodedLsps();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LQWD] Error during reconnect attempt");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Change interval if needed
            }

            _logger.LogInformation("[LQWD] ReconnectWorker stopping");
        }
    }
}
