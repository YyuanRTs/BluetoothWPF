using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using InTheHand.Windows.Forms;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.Commands;
using System.IO;
using System.Windows.Data;
using System.Globalization;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace myBluetoothWPF
{
    public class DataConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.ToArray();
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new object[] { value };
    }

    public abstract class BluetoothListItemBase : BindableBase
    {
        public string FriendlyName { get; set; } = "";
        public virtual string Description { get { return FriendlyName; } }
        public DelegateCommandBase TakeActionCommand { get { return DelegateCommand<object>.FromAsyncHandler(TakeActionAsync); } }

        private ConcurrentQueue<string> loggedQueue = new ConcurrentQueue<string>();
        private uint logMargin = uint.MaxValue
            ;
        private uint logCurrent = 0;

        public async Task TakeActionAsync(object textbox)
        {
            System.Windows.Controls.TextBox tb = textbox as System.Windows.Controls.TextBox;
            if (tb == null) return;
            try
            {
                await TakeActionAsync(tb.Text);
                await App.Current.Dispatcher.InvokeAsync(() => tb.Text = "");
            }
            catch (Exception) { }
        }

        public async Task TakeActionAsync(string command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command)) return;
                if (command.StartsWith("@rename:"))
                {
                    FriendlyName = command.Substring(8).Trim();
                    OnPropertyChanged(nameof(FriendlyName));
                }
                else if (command.StartsWith("@saveto:"))
                {
                    string path = command.Substring(8).Trim();
                    await saveToFileAsync(path);
                }
                else if (command.TrimEnd() == "@refresh")
                {
                    logCurrent = 0;
                    OnPropertyChanged(nameof(Log));
                }
                else if (command.StartsWith("@autorefresh:"))
                {
                    string margin = command.Substring(13).Trim();
                    uint m;
                    if (uint.TryParse(margin, out m))
                    {
                        if (m == 0) logMargin = uint.MaxValue;
                        else logMargin = m;
                        logCurrent = 0;
                        OnPropertyChanged(nameof(Log));
                    }
                }
                else if (!command.StartsWith("@"))
                {
                    await SendMessageAsync(command);
                    OnPropertyChanged(nameof(Log));
                }
            }
            catch (Exception) { }
        }

        public async Task saveToFileAsync(string path)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    while (!loggedQueue.IsEmpty)
                    {
                        string result;
                        if (!loggedQueue.TryDequeue(out result)) break;
                        await sw.WriteLineAsync(result);
                    }
                }
            }
            catch (Exception e)
            {
                AppendLog("系统消息", e.Message);
            }
        }

        public virtual string Log { get; set; }
        public void AppendLog(string sender, string log)
        {
            Log = Log + $"\n{sender}({DateTime.Now})：\n\t{log}";
            logCurrent++;
            if(logCurrent >= logMargin)
            {
                logCurrent = 0;
                OnPropertyChanged(nameof(Log));
            }
        }
        public void AppendLog(string log)
        {
            loggedQueue.Enqueue(log);
        }

        public abstract Task SendMessageAsync(string message);
    }

    public class BluetoothServiceItem : BluetoothListItemBase
    {
        public BluetoothDeviceItem Device { get; set; }

        public event EventHandler<string> SendingMessage = (o, e) => { };
        public event EventHandler<string> ReceivedMessage = (o, e) => { };

        private BluetoothEndPoint endPoint;
        private Stream peerStream;
        private StreamReader reader;
        private StreamWriter writer;

        private BluetoothServiceItem() { }

        public static async Task<BluetoothServiceItem> GetBluetoothServiceItemAsync(BluetoothAddress address, Guid service)
        {
            BluetoothServiceItem item = new BluetoothServiceItem();
            item.FriendlyName = BluetoothService.GetName(service);
            item.endPoint = new BluetoothEndPoint(address, service);
            try
            {
                BluetoothClient client = new BluetoothClient();
                await Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, item.endPoint, null);
                item.peerStream = client.GetStream();
                item.reader = new StreamReader(item.peerStream, Encoding.ASCII);
                item.writer = new StreamWriter(item.peerStream, Encoding.ASCII);
                item.writer.AutoFlush = true;
                return item;
            }
            catch (Exception) { return null; }
        }

        public override Task SendMessageAsync(string message) => SendMessageAsync(message, false);
        public async Task SendMessageAsync(string message, bool multi)
        {
            try
            {
                await writer.WriteAsync(message);
                AppendLog("本机", message);
                if (!multi) SendingMessage(this, message);
            }
            catch (Exception e) { AppendLog("系统错误", e.Message); }
        }

        public async Task BeginReceiveMessageAsync()
        {
            //int count = 0;
            //uint[] res = new uint[2];
            //byte[] buffer = new byte[100];
            try
            {
                //byte[] buffer = new byte[2];
                while (true)
                {
                    string message = await reader.ReadLineAsync();
                    message = Convert.ToString(Convert.ToUInt16(message, 16));
                    //await peerStream.ReadAsync(buffer, 0, 2);
                    //Debug.WriteLine(buffer[0]);
                    //Debug.WriteLine(buffer[1]);
                    //int data = (((int)buffer[1]) << 8 | buffer[0]);

                    //string message = data.ToString();
                    //if (message1.Length < 2) continue;
                    //int b =await peerStream.ReadAsync(buffer,0,1);
                    //if (b == -1) continue;
                    //if(count==0)
                    //{
                    //    res[0] = (uint)b;
                    //    count = 1;
                    //}
                    //else
                    //{
                    //    res[1] = (uint)b;
                    //    count = 0;
                    //}
                    //uint temp = message1[1] + (uint)(message1[0]==16? 10:message1[0]) * 256;
                    //string message = Convert.ToString(temp);
                    AppendLog($"{DateTime.Now.TimeOfDay.TotalMilliseconds},{message}");
                    //AppendLog("设备", message);
                    ReceivedMessage(this, message);
                }
            }
            catch (Exception e) { AppendLog("系统错误", e.Message); }
        }
    }

    public class BluetoothDeviceItem : BluetoothListItemBase
    {
        public override string Log { get; set; } = "请选择一项服务";
        public override string Description { get { return $"设备名：{FriendlyName}\n地址：{DeviceInfo.DeviceAddress}"; } }

        public ObservableCollection<BluetoothServiceItem> Services { get; set; } = new ObservableCollection<BluetoothServiceItem>();
        public BluetoothDeviceInfo DeviceInfo { get; set; }

        private BluetoothDeviceItem() { }

        public static async Task<BluetoothDeviceItem> GetBluetoothDeviceItemAsync(BluetoothDeviceInfo info)
        {
            BluetoothDeviceItem device = new BluetoothDeviceItem()
            { FriendlyName = info.DeviceName, DeviceInfo = info };
            List<Guid> services = device.DeviceInfo.InstalledServices.ToList();
            if (!services.Contains(BluetoothService.SerialPort)) services.Add(BluetoothService.SerialPort);

            foreach (Guid service in services)
            {
                BluetoothServiceItem bluetoothService = await BluetoothServiceItem.GetBluetoothServiceItemAsync(device.DeviceInfo.DeviceAddress, service);
                if (bluetoothService != null)
                {
                    bluetoothService.AppendLog("系统", $"{device.FriendlyName}的{bluetoothService.FriendlyName}服务已加入");
                    bluetoothService.Device = device;
                    await bluetoothService.BeginReceiveMessageAsync();
                    device.Services.Add(bluetoothService);
                }
            }

            if (device.Services.Count == 0) return null;
            else return device;
        }

        public override Task SendMessageAsync(string message)
        {
            throw new NotImplementedException();
        }
    }

    public class AllBluetoothItem : BluetoothListItemBase
    {
        private List<BluetoothServiceItem> allDevices = new List<BluetoothServiceItem>();

        public void Insert(BluetoothServiceItem item)
        {
            if (item != null)
            {
                allDevices.Add(item);
                item.SendingMessage += (o, e) => AppendLog($"本机=>{((BluetoothServiceItem)o).Device.FriendlyName}的{((BluetoothServiceItem)o).FriendlyName}", e);
                item.ReceivedMessage += (o, e) =>
                {
                    AppendLog($"{((BluetoothServiceItem)o).Device.FriendlyName},{DateTime.Now.TimeOfDay.TotalMilliseconds},{e}");
                    //AppendLog(((BluetoothServiceItem)o).Device.FriendlyName, e);
                };
            }
        }

        public override async Task SendMessageAsync(string message)
        {
            foreach (var item in allDevices)
                await item.SendMessageAsync(message, true);
            AppendLog("本机=>全体", message);
        }

        public AllBluetoothItem()
        {
            FriendlyName = "全体设备";
        }
    }

    public class MainWindowViewModel : BindableBase
    {
        public DelegateCommandBase AddDeviceCommand { get { return DelegateCommand.FromAsyncHandler(AddDeviceAsync); } }

        public ObservableCollection<BluetoothListItemBase> BluetoothDevices { get; private set; }

        private AllBluetoothItem allBluetoothItem = new AllBluetoothItem();

        public MainWindowViewModel()
        {
            BluetoothDevices = new ObservableCollection<BluetoothListItemBase>();
            BluetoothDevices.Add(allBluetoothItem);
        }

        private async Task AddDeviceAsync()
        {
            SelectBluetoothDeviceDialog dialog = new SelectBluetoothDeviceDialog()
            { ShowRemembered = false, ShowAuthenticated = true, ShowUnknown = true };

            DialogResult result = dialog.ShowDialog();

            if(result == DialogResult.OK)
            {
                BluetoothDeviceInfo selectedDevice = dialog.SelectedDevice;
                if (selectedDevice != null && !BluetoothDevices.Any(d => (d as BluetoothDeviceItem)?.DeviceInfo.DeviceAddress == selectedDevice.DeviceAddress))
                {
                    BluetoothDeviceItem item = await BluetoothDeviceItem.GetBluetoothDeviceItemAsync(selectedDevice);
                    if (item == null) MessageBox.Show("该设备没有可连接的服务", "错误");
                    else
                    {
                        BluetoothDevices.Add(item);
                        foreach (var s in item.Services)
                            allBluetoothItem.Insert(s);
                    }
                }
            }
        }
    }
}
