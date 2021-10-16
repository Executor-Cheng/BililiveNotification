using Executorlibs.Bilibili.Protocol.Builders;
using Executorlibs.Bilibili.Protocol.Clients;
using Executorlibs.Bilibili.Protocol.Invokers;
using Executorlibs.Bilibili.Protocol.Parsers;
using Executorlibs.Bilibili.Protocol.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace BililiveNotification
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider? _services;

        private readonly AddRoomWindow? _addRoomWindow;

        private readonly RoomMonitorManager? _monitorManager;

        public MainWindow()
        {
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
            if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                InitializeComponent();
                _services = new ServiceCollection()
                                .AddLogging()
                                .AddBilibiliDanmakuFramework()
                                .AddCredentialProvider<DanmakuServerProvider>()
                                .AddParser<LiveStartParser>()
                                .AddParser<LiveEndParser>()
                                .AddInvoker<BilibiliMessageHandlerInvoker>()
#if NET5_0_OR_GREATER
                                .AddClient<TcpDanmakuClientV3>()
#else
                                .AddClient<TcpDanmakuClientV2>()
#endif
                                .AddHandler(services => services.GetRequiredService<RoomMonitor>())
                                .Services
                                .AddSingleton<AddRoomWindow>()
                                .AddSingleton<RoomMonitorManager>()
                                .AddSingleton<NotifyIcon>()
                                .AddScoped<RoomMonitor>()
                                .AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName, client =>
                                {
                                    client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36 Edg/93.0.961.38");
                                })
                                .Services
                                .BuildServiceProvider();
                _addRoomWindow = _services.GetRequiredService<AddRoomWindow>();
                _monitorManager = _services.GetRequiredService<RoomMonitorManager>();
                this.MonitorDG.ItemsSource = _monitorManager.RoomMonitors;
                this.Closing += MainWindow_Closing;
                this.StateChanged += MainWindow_StateChanged;
                InitializeNotifyIcon();
                return;
            }
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void InitializeNotifyIcon()
        {
            NotifyIcon icon = _services!.GetRequiredService<NotifyIcon>();
            icon.Text = "直播提醒";
            icon.Icon = Properties.Resources.favicon;
            icon.Visible = true;
            icon.DoubleClick += Icon_DoubleClick;
        }

        private void Icon_DoubleClick(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("确认退出？", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ((IDisposable)_services!).Dispose();
                return;
            }
            e.Cancel = true;
            this.Hide();
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            if (e.Argument != "")
            {
                new Thread(roomId =>
                {
                    using Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = $"https://live.bilibili.com/{roomId}";
                    process.Start();
                    if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                    {
                        Environment.Exit(0);
                    }
                }).Start(e.Argument);
            }
        }

        private void AddRoom_Click(object sender, RoutedEventArgs e)
        {
            _addRoomWindow!.Show();
        }

        private void RemoveRoom_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                _monitorManager!.RemoveMonitor(monitor);
            }
        }

        private async void StartMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                Button btn = (Button)sender;
                btn.IsEnabled = false;
                try
                {
                    await monitor.StartAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启用监控: {ex}", this.Title, 0, MessageBoxImage.Error);
                }
                btn.IsEnabled = true;
            }
        }

        private void StopMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                monitor.Stop();
            }
        }

        private void SetLiveStart_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                monitor.LiveStatus = true;
            }
        }

        private void SetLiveEnd_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                monitor.LiveStatus = false;
            }
        }

        private async void PopToast_Click(object sender, RoutedEventArgs e)
        {
            if (this.MonitorDG.SelectedItem is RoomMonitor monitor)
            {
                Button btn = (Button)sender;
                btn.IsEnabled = false;
                try
                {
                    await monitor.PopToastAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启用监控: {ex}", this.Title, 0, MessageBoxImage.Error);
                }
                btn.IsEnabled = true;
            }
        }
    }
}
