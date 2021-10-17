using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Executorlibs.Bilibili.Protocol.Options;
using Microsoft.Extensions.Options;
using Executorlibs.Bilibili.Protocol.Clients;

namespace BililiveNotification
{
    public class RoomMonitorManager
    {
        public ObservableCollection<RoomMonitor> RoomMonitors { get; }

        private readonly IDictionary<int, IServiceScope> _roomScopes;

        private readonly IServiceProvider _services;

        public RoomMonitorManager(IServiceProvider services)
        {
            RoomMonitors = new ObservableCollection<RoomMonitor>();
            _roomScopes = new Dictionary<int, IServiceScope>();
            _services = services;
        }

        public async Task AddMonitorAsync(int roomId)
        {
            if (_roomScopes.ContainsKey(roomId))
            {
                return;
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
            if (_roomScopes.TryGetValue(monitor.RoomId, out IServiceScope? scope))
            {
                scope.ServiceProvider.GetRequiredService<IDanmakuClient>().Dispose();
                scope.Dispose();
            }
            RoomMonitors.Remove(monitor);
        }
    }
}
