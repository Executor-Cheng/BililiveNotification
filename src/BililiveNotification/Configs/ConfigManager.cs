using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BililiveNotification.Configs
{
    public sealed class ConfigManager : BackgroundService
    {
        private readonly MainConfig _config;

        public ConfigManager(IOptions<MainConfig> config)
        {
            _config = config.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(_config);
            File.WriteAllText("./MainConfig.json", json);
        }

        public override void Dispose()
        {
            Save();
            base.Dispose();
        }
    }
}
