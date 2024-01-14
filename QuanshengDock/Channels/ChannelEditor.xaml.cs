using QuanshengDock.Data;
using QuanshengDock.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QuanshengDock.Channels
{
    // code by -nicsure- 2024
    // https://www.youtube.com/nicsure

    public partial class ChannelEditor : Window
    {
        private static readonly Brush upButt = new SolidColorBrush(Color.FromArgb(255, 0x20, 0x20, 0x20));
        private static readonly Brush downButt = new SolidColorBrush(Color.FromArgb(255, 0x10, 0x10, 0x10));
        private static ButtonBorder? pressedButt = null;
        private static readonly Brush groupedBrush = new SolidColorBrush(Colors.DarkBlue);
        private static readonly Brush transBrush = new SolidColorBrush(Colors.Transparent);

        public ChannelEditor()
        {
            DataContext = Context.Instance;
            InitializeComponent();
            ChannelGrid.SelectedCellsChanged += ChannelGrid_SelectedCellsChanged;
            ChannelGrid.CurrentCellChanged += ChannelGrid_CurrentCellChanged;
        }

        private void ChannelGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            ChannelGrid.CommitEdit();
        }

        private void ChannelGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            GridChannel.SelectedChannels.Clear();
            foreach (var v in ChannelGrid.SelectedItems)
            {
                if (v is GridChannel channel)
                {
                    GridChannel.SelectedChannels.Add(channel);
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Channel.SelectedChannel = 
                ChannelGrid.SelectedItems.Count == 0 ? -1 : (ChannelGrid.SelectedItems[0] as GridChannel)!.Number - 1;
            if (Mouse.DirectlyOver is ButtonBorder butt)
            {
                switch (butt.Tag)
                {
                    case "Group":
                        GroupChannels(true);
                        break;
                    case "Ungroup":
                        GroupChannels(false);
                        break;
                }
                butt.Background = downButt;
                pressedButt = butt;
            }
            else
            if (Mouse.DirectlyOver is Border)
                this.DragMove();
        }

        private void GroupChannels(bool group)
        {
            foreach (var v in ChannelGrid.SelectedItems)
            {
                if (v is GridChannel channel)
                {
                    channel.Group(group);
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            Dehighlight();
        }

        private static void Dehighlight()
        {
            if (pressedButt != null)
            {
                pressedButt.Background = upButt;
                pressedButt = null;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            Dehighlight();
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.CanUserReorder = false;
            e.Column.CanUserResize = false;
            e.Column.CanUserSort = false;
            switch (e.Column)
            {
                case DataGridCheckBoxColumn:
                    {
                        DataGridCheckBoxColumn col = (DataGridCheckBoxColumn)e.Column;
                        switch (e.PropertyName)
                        {
                            case "Reverse":
                                col.Header = "倒频";
                                break;
                            case "BusyLock":
                                col.Header = "遇忙禁发";
                                break;
                        }
                    }
                    break;
                case DataGridTextColumn:
                    {
                        DataGridTextColumn col = (DataGridTextColumn)e.Column;
                        switch (e.PropertyName)
                        {
                            case "G":
                                col.Header = null;
                                break;
                            case "TX":
                            case "RX":
                                col.Header = e.PropertyName == "TX" ? "发射频率" : "接收频率";
                                col.Binding = new Binding(e.PropertyName) { StringFormat = "F5" };
                                break;
                            case "Number":
                                col.Header = "序号";
                                col.Binding = new Binding(e.PropertyName) { StringFormat = "D3" };
                                break;
                            case "Name":
                                col.Header = "频道名称";
                                break;
                        }
                    }
                    break;
                case DataGridComboBoxColumn:
                    {
                        DataGridComboBoxColumn col = (DataGridComboBoxColumn)e.Column;
                        switch (e.PropertyName)
                        {
                            case "RxCTCSS":
                            case "TxCTCSS":
                                col.Header = e.PropertyName == "RxCTCSS" ? "接收模拟哑音" : "发射模拟哑音";
                                col.ItemsSource = Beautifiers.Ctcss.Strings;
                                col.SelectedItemBinding = new Binding(e.PropertyName) { Converter = Beautifiers.Ctcss };
                                break;
                            case "RxDCS":
                            case "TxDCS":
                                col.Header = e.PropertyName == "RxDCS" ? "接收数字哑音" : "发射数字哑音";
                                col.ItemsSource = Beautifiers.Dcs.Strings;
                                col.SelectedItemBinding = new Binding(e.PropertyName) { Converter = Beautifiers.Dcs };
                                break;
                            case "Step":
                                col.Header = "步进频率";
                                col.ItemsSource = Beautifiers.Step.Strings;
                                col.SelectedItemBinding = new Binding(e.PropertyName) { Converter = Beautifiers.Step };
                                break;
                            case "Scramble":
                                col.Header = "加密";
                                col.ItemsSource = Beautifiers.Scramble.Strings;
                                col.SelectedItemBinding = new Binding(e.PropertyName) { Converter = Beautifiers.Scramble };
                                break;
                            case "Bandwidth":
                                col.Header = "带宽";
                                break;
                            case "Power":
                                col.Header = "发射功率";
                                break;
                            case "RxTone":
                                col.Header = "接收哑音";
                                break;
                            case "TxTone":
                                col.Header = "发射哑音";
                                break;
                            case "Modulation":
                                col.Header = "调制模式";
                                break;
                            case "Scanlists":
                                col.Header = "扫描列表";
                                break;
                            case "Compand":
                                col.Header = "压扩";
                                break;
                        }
                    }
                    break;
            }
        }

        private void Frame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void ChannelGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if(e.Row.Item is GridChannel channel)
            {
                channel.SetRow(e.Row);
                e.Row.Background = channel.IsGrouped() ? groupedBrush : transBrush;
                channel.SetRowOpacity();
            }
        }

        private void Magnify_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ChannelGrid != null && ChannelGrid.Columns != null)
            {
                foreach (var col in ChannelGrid.Columns)
                {
                    if (col == null) continue;
                    col.Width = 0;
                    col.Width = DataGridLength.Auto;
                }
            }
        }

        private void ChannelGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is GridChannel channel)
                channel.SetRow(null);
        }
    }
}
