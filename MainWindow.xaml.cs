using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkMonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadActiveConnections();

            // Запуск обновления данных о трафике
            UpdateTrafficData();
        }

        // Получение имени сетевого интерфейса
        private string GetNetworkInterfaceName()
        {
            var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .ToList();

            if (activeInterfaces.Count == 0)
            {
                MessageBox.Show("Нет активных сетевых интерфейсов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // Возвращаем имя первого активного интерфейса
            return activeInterfaces.First().Name;
        }

        // Обновление данных о трафике
        private async void UpdateTrafficData()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .ToList();

            if (interfaces.Count == 0)
            {
                MessageBox.Show("Нет активных сетевых интерфейсов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            while (true)
            {
                await Task.Delay(1000); // Обновление каждую секунду

                long totalIncoming = 0;
                long totalOutgoing = 0;

                foreach (var nic in interfaces)
                {
                    var stats = nic.GetIPv4Statistics();
                    totalIncoming += stats.BytesReceived;
                    totalOutgoing += stats.BytesSent;

                    Debug.WriteLine($"Интерфейс: {nic.Name}, Входящий трафик: {stats.BytesReceived} байт, Исходящий трафик: {stats.BytesSent} байт");
                }

                Debug.WriteLine($"Общий входящий трафик: {totalIncoming} байт, Общий исходящий трафик: {totalOutgoing} байт");

                Dispatcher.Invoke(() =>
                {
                    IncomingTrafficText.Text = FormatBytes(totalIncoming);
                    OutgoingTrafficText.Text = FormatBytes(totalOutgoing);
                });
            }
        }

        private string FormatBytes(long bytes)
        {
            double value = bytes;
            string[] units = { "Б", "КБ", "МБ", "ГБ" };
            int i = 0;

            while (value >= 1024 && i < units.Length - 1)
            {
                value /= 1024;
                i++;
            }

            return $"{value:0.##} {units[i]}";
        }

        // Загрузка активных подключений
        private void LoadActiveConnections()
        {
            var activeConnections = new List<ConnectionInfo>();

            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = properties.GetActiveTcpConnections();

            foreach (var connection in tcpConnections)
            {
                activeConnections.Add(new ConnectionInfo
                {
                    LocalAddress = connection.LocalEndPoint.Address.ToString(),
                    LocalPort = connection.LocalEndPoint.Port,
                    RemoteAddress = connection.RemoteEndPoint.Address.ToString(),
                    RemotePort = connection.RemoteEndPoint.Port,
                    State = connection.State.ToString()
                });
            }

            ActiveConnectionsGrid.ItemsSource = activeConnections;
        }

        // Блокировка IP-адреса
        private void BlockIp_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpToBlockTextBox.Text;
            if (!string.IsNullOrEmpty(ip))
            {
                ExecuteFirewallCommand($"netsh advfirewall firewall add rule name=\"Block IP {ip}\" dir=in action=block remoteip={ip}");
                MessageBox.Show($"IP-адрес {ip} заблокирован.");
            }
        }

        // Блокировка порта
        private void BlockPort_Click(object sender, RoutedEventArgs e)
        {
            string port = PortToBlockTextBox.Text;
            if (!string.IsNullOrEmpty(port) && int.TryParse(port, out int portNumber))
            {
                ExecuteFirewallCommand($"netsh advfirewall firewall add rule name=\"Block Port {port}\" dir=in action=block protocol=TCP localport={port}");
                MessageBox.Show($"Порт {port} заблокирован.");
            }
        }

        // Выполнение команды брандмауэра
        private void ExecuteFirewallCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
    }

    // Класс для хранения информации о подключениях
    public class ConnectionInfo
    {
        public string LocalAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public string State { get; set; }
    }
}