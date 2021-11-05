using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BililiveNotification.Configs;
using Executorlibs.Bilibili.Protocol.Clients;
using Executorlibs.Bilibili.Protocol.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BililiveNotification
{
    public class RoomMonitorManager : BackgroundService
    {
        public ObservableCollection<RoomMonitor> RoomMonitors { get; }

        private readonly IDictionary<int, IServiceScope> _roomScopes;

        private readonly IServiceProvider _services;

        private readonly MainConfig _config;

        public RoomMonitorManager(IServiceProvider services, IOptions<MainConfig> config)
        {
            RoomMonitors = new ObservableCollection<RoomMonitor>();
            _roomScopes = new Dictionary<int, IServiceScope>();
            _services = services;
            _config = config.Value;
        }

        public async Task AddMonitorAsync(int roomId, bool modifyConfig)
        {
            if (_roomScopes.ContainsKey(roomId))
            {
                return;
            }
            if (modifyConfig)
            {
                _config.RoomIds.Add(roomId);
            }
            IServiceScope scope = _services.CreateScope();
            _roomScopes[roomId] = scope;
            IServiceProvider services = scope.ServiceProvider;
            services.GetRequiredService<IOptionsSnapshot<DanmakuClientOptions>>().Value.RoomId = roomId;
            RoomMonitor monitor = services.GetRequiredService<RoomMonitor>();
            RoomMonitors.Add(monitor);
            await monitor.InitializeAsync();
        }

        public void RemoveMonitor(RoomMonitor monitor)
        {
            int roomId = monitor.RoomId;
            if (_roomScopes.TryGetValue(roomId, out IServiceScope? scope))
            {
                scope.ServiceProvider.GetRequiredService<IDanmakuClient>().Dispose();
                scope.Dispose();
            }
            RoomMonitors.Remove(monitor);
            _config.RoomIds.Remove(roomId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (int roomId in _config.RoomIds)
            {
                await AddMonitorAsync(roomId, false);
            }
        }
    }
}
