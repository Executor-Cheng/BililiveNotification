using BililiveNotification.Apis;
using BililiveNotification.Models;
using Executorlibs.Bilibili.Protocol.Clients;
using Executorlibs.Bilibili.Protocol.Handlers;
using Executorlibs.Bilibili.Protocol.Models.Danmaku;
using Executorlibs.Bilibili.Protocol.Models.General;
using Executorlibs.Bilibili.Protocol.Options;
using Executorlibs.Shared.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;
#if !NET5_0_OR_GREATER
using Executorlibs.MessageFramework.Clients;
using Executorlibs.MessageFramework.Models.General;
#endif

namespace BililiveNotification
{
    public sealed class RoomMonitor : IDisposable,
                                      INotifyPropertyChanged,
                                      IInvarianceBilibiliMessageHandler<ILiveStartMessage>,
                                      IInvarianceBilibiliMessageHandler<ILiveEndMessage>,
                                      IInvarianceBilibiliMessageHandler<IDisconnectedMessage>
    {
        private readonly IDanmakuClient _danmakuClient;

        private readonly HttpClient _httpClient;

        private CancellationTokenSource? _cts;

        private readonly CancellationToken _token;

        public event PropertyChangedEventHandler? PropertyChanged;

        private DateTime _lastTime;

        private bool _status;

        public bool Status 
        {
            get => _status;
            set 
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public bool LiveStatus
        {
            get => _lastTime != default;
            set
            {
                _lastTime = value ? DateTime.Now : default;
                OnPropertyChanged(nameof(LiveStatusDisplay));
            }
        }

        public int RoomId { get; }

        public string StatusDisplay => _status ? "启用" : "禁用";

        public string ConnectionStatusDisplay => _danmakuClient.Connected ? "已连接" : "未连接";

        public string LiveStatusDisplay => LiveStatus ? "正在直播" : "未直播";

        public RoomMonitor(IDanmakuClient danmakuClient, HttpClient httpClient, IOptionsSnapshot<DanmakuClientOptions> danmakuOptions)
        {
            _danmakuClient = danmakuClient;
            _httpClient = httpClient;
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
            RoomId = danmakuOptions.Value.RoomId;
        }

        private static async Task DownloadFileAsync(HttpClient client, string url, string path, CancellationToken token = default)
        {
            using FileStream fs = new FileStream(path, FileMode.Create);
            using Stream stream = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).GetStreamAsync(token);
#if NET5_0_OR_GREATER
            await stream.CopyToAsync(fs, token);
#else
            await stream.CopyToAsync(fs, 81920, token);
#endif
        }

        private async Task ConnectAsync()
        {
            OnPropertyChanged(nameof(ConnectionStatusDisplay));
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    await _danmakuClient.ConnectAsync(_token);
                    OnPropertyChanged(nameof(ConnectionStatusDisplay));
                    break;
                }
                catch
                {
                    await Task.Delay(1000, default);
                }
            }
        }

        public async Task PopToastAsync()
        {
            RoomInfo roomInfo = await BiliApis.GetRoomInfoAsync(_httpClient, RoomId, _token);
            //string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BililiveNotification", RoomId.ToString());
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "imgs", RoomId.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string faceExt = Path.GetExtension(roomInfo.FaceUrl),
                   facePath = Path.Combine(path, $"face{faceExt}");
            await DownloadFileAsync(_httpClient, $"{roomInfo.FaceUrl}@96w_96h{faceExt}", facePath); // 48x48 -> 96x96
            string? coverPath = null;
            if (roomInfo.CoverUrl != null)
            {
                string coverExt = Path.GetExtension(roomInfo.CoverUrl);
                coverPath = Path.Combine(path, $"cover{coverExt}");
                await DownloadFileAsync(_httpClient, $"{roomInfo.CoverUrl}@728w_360h{coverExt}", Path.Combine(path, coverPath)); // 364x180 -> 728x360
            }
            ToastContentBuilder builder = new ToastContentBuilder();
            if (coverPath != null)
            {
                builder.AddHeroImage(new Uri(coverPath));
            }
            builder.AddAppLogoOverride(new Uri(facePath), ToastGenericAppLogoCrop.Circle)
                .AddText("播啦播啦")
                .AddText($"{RoomId}@{roomInfo.UserName}[{roomInfo.UserId}]")
                .AddText("你订阅的直播间开播啦")
                .AddButton("这就去", ToastActivationType.Background, RoomId.ToString())
                .AddButton("稍后再说", ToastActivationType.Background, "")
                .Show();
        }

        private void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task InitializeAsync()
        {
            DateTime? time = await BiliApis.GetLiveTimeAsync(_httpClient, RoomId);
            if (time.HasValue)
            {
                _lastTime = time.Value;
                OnPropertyChanged(nameof(LiveStatusDisplay));
            }
            await StartAsync();
        }

        public Task StartAsync()
        {
            if (Status)
            {
                return Task.CompletedTask;
            }
            Status = true;
            return ConnectAsync();
        }

        public void Stop()
        {
            if (Status)
            {
                Status = false;
                _danmakuClient.Disconnect();
                OnPropertyChanged(nameof(ConnectionStatusDisplay));
            }
        }

        public Task HandleMessageAsync(IDanmakuClient client, ILiveStartMessage message)
        {
            static ref long AsLong(ref DateTime time)
            {
                return ref Unsafe.As<DateTime, long>(ref time);
            }
            DateTime liveTime = message.Time;
            if (Interlocked.CompareExchange(ref AsLong(ref _lastTime), AsLong(ref liveTime), default) == default)
            {
                OnPropertyChanged(nameof(LiveStatusDisplay));
                return PopToastAsync();
            }
            return Task.CompletedTask;
        }

        public Task HandleMessageAsync(IDanmakuClient client, ILiveEndMessage message)
        {
            _lastTime = default;
            OnPropertyChanged(nameof(LiveStatusDisplay));
            return Task.CompletedTask;
        }

        public Task HandleMessageAsync(IDanmakuClient client, IDisconnectedMessage message)
        {
            if (message.Exception == null) // 主动断线
            {
                return Task.CompletedTask;
            }
            return ConnectAsync();
        }

        public void Dispose()
        {
            CancellationTokenSource? cts = Interlocked.Exchange(ref _cts, null);
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

#if !NET5_0_OR_GREATER
        public Task HandleMessageAsync(IDanmakuClient client, IBilibiliMessage message)
        {
            throw new NotSupportedException();
        }

        public Task HandleMessageAsync(IMessageClient client, IMessage message)
        {
            throw new NotSupportedException();
        }
#endif
    }
}
