using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.Windows.Forms;

namespace myBluetoothWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void bluetoothDeviceList_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem selectedItem = e.OriginalSource as TreeViewItem;
            if (selectedItem == null) return;
            if (selectedItem.Items.Count != 0)
            {
                selectedItem.ExpandSubtree();
                selectedItem.IsSelected = false;
            }
        }

        private void Button_Rename_Click(object sender, RoutedEventArgs e)
        {
            if (messageBox.Text.StartsWith("@")) messageBox.Text = "@rename: ";
            else messageBox.Text = "@rename: " + messageBox.Text;
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            { AddExtension = true, AutoUpgradeEnabled = true, CheckPathExists = true, DefaultExt = "csv", OverwritePrompt = true, Title = "导出数据为", Filter = "CSV文件|*.csv" };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                messageBox.Text = "@saveto: " + dialog.FileName;
        }

        private void Button_AutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            messageBox.Text = "@autorefresh: 1";
        }

        private void Button_Refresh_Click(object sender, RoutedEventArgs e)
        {
            messageBox.Text = "@refresh";
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            logHolder.ScrollToEnd();
        }
    }
}
