using BililiveNotification.Apis;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace BililiveNotification
{
    /// <summary>
    /// Interaction logic for AddRoomWindow.xaml
    /// </summary>
    public sealed partial class AddRoomWindow : Window, IDisposable
    {
        private readonly RoomMonitorManager _manager;

        public AddRoomWindow(RoomMonitorManager manager)
        {
            InitializeComponent();
            _manager = manager;
            this.Closing += AddRoomWindow_Closing;
        }

        private void AddRoomWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(this.RoomIdBox.Text, out int roomId) && roomId > 0)
            {
                this.SubmitBtn.IsEnabled = false;
                try
                {
                    roomId = await BiliApis.GetRealRoomIdAsync(roomId, default);
                    await _manager.AddMonitorAsync(roomId);
                    this.RoomIdBox.Text = null;
                    this.Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加失败: {ex}", this.Title, 0, MessageBoxImage.Error);
                }
                this.SubmitBtn.IsEnabled = true;
                return;
            }
            MessageBox.Show("无效的房间号", this.Title, 0, MessageBoxImage.Warning);
        }

        public void Dispose()
        {
            this.Closing -= AddRoomWindow_Closing;
            this.Close();
        }

        private void RoomIdBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Submit_Click(sender, e);
            }
        }
    }
}
