using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Executorlibs.Bilibili.Protocol.Invokers;
using Executorlibs.Bilibili.Protocol.Models;
using Executorlibs.Bilibili.Protocol.Options;
using Executorlibs.Bilibili.Protocol.Services;
using Microsoft.Extensions.Options;
#if NET5_0_OR_GREATER
using TcpDanmakuClient = Executorlibs.Bilibili.Protocol.Clients.TcpDanmakuClientV3;
#else
using Executorlibs.Shared.Extensions;
using TcpDanmakuClient = Executorlibs.Bilibili.Protocol.Clients.TcpDanmakuClientV2;
#endif

namespace BililiveNotification.Clients
{
    public class AnonymousDanmakuClient : TcpDanmakuClient
    {
        public AnonymousDanmakuClient(IBilibiliMessageHandlerInvoker invoker, IBilibiliMessageSubscriptionResolver resolver, IOptionsSnapshot<DanmakuClientOptions> options, IDanmakuServerProvider credentialProvider) : base(invoker, resolver, options, credentialProvider)
        {

        }

        protected override async Task InternalConnectAsync(CancellationToken token)
        {
            int roomId = _options.RoomId;
            DanmakuServerInfo server = await _credentialProvider.GetDanmakuServerInfoAsync(token);
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            int sendTimeout = (socket.ReceiveTimeout = (int)_options.HeartbeatInterval.TotalMilliseconds + 10000);
            socket.SendTimeout = sendTimeout;
            token.Register(socket.Dispose);
            DanmakuServerHostInfo danmakuServerHostInfo = server.Hosts[(int)(Stopwatch.GetTimestamp() % server.Hosts.Length)];
#if NET5_0_OR_GREATER
            await socket.ConnectAsync(danmakuServerHostInfo.Host, danmakuServerHostInfo.Port, token);
#else
            await socket.ConnectAsync(danmakuServerHostInfo.Host, danmakuServerHostInfo.Port);
#endif
            await socket.SendAsync(CreateNewJoinRoomPayload(roomId, 0, server.Token), SocketFlags.None, token);
            _Socket = socket;
        }

        private byte[] CreateNewJoinRoomPayload(int roomId, long userId, string token)
        {
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                uid = userId,
                roomid = roomId,
                protover = Version,
                buvid = $"{Guid.NewGuid()}{new Random().Next(10000, 100000)}infoc",
                platform = "web",
                type = 2,
                key = token
            });
            return CreatePayload(7, body);
        }
    }
}
