﻿using Microsoft.Win32;
using QuanshengDock.Data;
using QuanshengDock.General;
using QuanshengDock.Serial;
using QuanshengDock.User;
using QuanshengDock.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;

namespace QuanshengDock.Channels
{
    // code by -nicsure- 2024
    // https://www.youtube.com/nicsure

    internal class ChannelStatics { }

    public partial class Channel
    {
        public static int Activator { get => 0; set { } }
        private static readonly ViewModel<ObservableCollection<GridChannel>> AllChannelsVM = VM.Get<ObservableCollection<GridChannel>>("AllChannels");
        private static readonly ViewModel<ObservableCollection<GridChannel>> UsedChannelsVM = VM.Get<ObservableCollection<GridChannel>>("UsedChannels");
        private static readonly ViewModel<ObservableCollection<GridChannel>> ViewedChannelsVM = VM.Get<ObservableCollection<GridChannel>>("Channels");
        private static readonly ViewModel<string> message = VM.Get<string>("ChEditMessage");
        private static readonly ViewModel<bool> enableChButts = VM.Get<bool>("EnableChanButtons");
        private static bool timeout = false;
        public static byte[] DataBytes { get; } = new byte[3200];
        public static byte[] NameBytes { get; } = new byte[3200];
        public static byte[] AttrBytes { get; } = new byte[200];
        public static Channel[] Channels { get; } = new Channel[200];
        public static int SelectedChannel { get; set; } = -1;

        static Channel()
        {
            for (int i = 0; i < 200; i++)
            {
                Channels[i] = new() { number = i };
                AllChannelsVM.Value.Add(new(i));
            }
            Sanitize();
            FilterUsed();
            ViewedChannelsVM.Value = AllChannelsVM.Value;
        }
        public static Channel Get(int c) => Channels[c];

        public static void Refresh()
        {
            var t = ViewedChannelsVM.Value;
            ViewedChannelsVM.Value = null!;
            ViewedChannelsVM.Value = t;
        }

        public static void Sanitize()
        {
            foreach (var channel in Channels)
            {
                if (channel.Band > 6 || channel.RxFrequency == 0)
                    channel.Clear();
            }
        }

        public static void FilterUsed()
        {
            UsedChannelsVM.Value.Clear();
            foreach(var channel in AllChannelsVM.Value)
            {
                if (channel.IsInUse() || channel.IsGrouped())
                    UsedChannelsVM.Value.Add(channel);
            }
        }

        public static void CopyChannels()
        {
            if(GridChannel.SelectedChannels.Count > 0)
            {
                List<CopiedChannel> channels = new();   
                foreach (var channel in GridChannel.SelectedChannels)
                    channels.Add(new(channel.Number - 1));
                CopiedChannel.Clipboard = channels.ToArray();
                DisplayMessage($"{channels.Count} 信道已复制");
            }
            else
                DisplayMessage("未选中信道");
        }

        public static void PasteChannels(int start)
        {
            if (start > -1)
            {
                int cnt = 0;
                foreach (var channel in CopiedChannel.Clipboard)
                {
                    channel.Paste(start++);
                    cnt++;
                    if (start == 200)
                        break;
                }
                DisplayMessage($"{cnt} 信道已粘贴");
                Refresh();
            }
            else
                DisplayMessage("未复制信道");
        }

        public static void SetDisplayed(bool all)
        {
            if (!all)
            {
                ViewedChannelsVM.Value = null!;
                FilterUsed();
                ViewedChannelsVM.Value = UsedChannelsVM.Value;
            }
            else
            {
                ViewedChannelsVM.Value = null!;
                ViewedChannelsVM.Value = AllChannelsVM.Value;
            }
        }

        public static void EepromDataRead(int offset, byte[] data, int length)
        {
            if (offset > 0) DisplayMessage($"读取中 {(offset * 100) / 6992}%");
            if (offset <= 3072)
            {
                Array.Copy(data, 0, DataBytes, offset, length);
                offset += 128;
                if (offset < 3200)
                    Comms.SendCommand(Packet.ReadEeprom, (ushort)offset, (ushort)128, Comms.TimeStamp);
                else
                    Comms.SendCommand(Packet.ReadEeprom, (ushort)3424, (ushort)100, Comms.TimeStamp);
                return;
            }
            if (offset <= 3524)
            {
                Array.Copy(data, 0, AttrBytes, offset - 3424, length);
                offset += 100;
                if (offset < 3624)
                    Comms.SendCommand(Packet.ReadEeprom, (ushort)offset, (ushort)100, Comms.TimeStamp);
                else
                    Comms.SendCommand(Packet.ReadEeprom, (ushort)3920, (ushort)128, Comms.TimeStamp);
                return;
            }
            if (offset <= 6992)
            {
                Array.Copy(data, 0, NameBytes, offset - 3920, length);
                offset += 128;
                if (offset < 7120)
                {
                    Comms.SendCommand(Packet.ReadEeprom, (ushort)offset, (ushort)128, Comms.TimeStamp);
                    return;
                }
                timeout = false;
                DisplayMessage("已从设备读出信道");
                Radio.Invoke(() => {
                    enableChButts.Value = true;
                    Sanitize();
                    FilterUsed();
                    Refresh();
                });
            }
        }        

        public static void EepromDataWritten(int offset)
        {
            if (offset > 0) DisplayMessage($"写入中 {(offset * 100) / 6992}%");
            if (offset <= 3072)
            {
                offset += 128;
                if (offset < 3200)
                {
                    byte[] b = new byte[128];
                    Array.Copy(DataBytes, offset, b, 0, 128);
                    Comms.SendCommand(Packet.WriteEeprom, (ushort)offset, (byte)128, (byte)1, Comms.TimeStamp, b);
                    return;
                }
                offset = 3324;
            }
            if (offset <= 3524)
            {
                offset += 100;
                if (offset < 3624)
                {
                    byte[] b = new byte[100];
                    Array.Copy(AttrBytes, offset - 3424, b, 0, 100);
                    Comms.SendCommand(Packet.WriteEeprom, (ushort)offset, (byte)100, (byte)1, Comms.TimeStamp, b);
                    return;
                }
                offset = 3792;
            }
            if (offset <= 6992)
            {
                offset += 128;
                if (offset < 7120)
                {
                    byte[] b = new byte[128];
                    Array.Copy(NameBytes, offset - 3920, b, 0, 128);
                    Comms.SendCommand(Packet.WriteEeprom, (ushort)offset, (byte)128, (byte)1, Comms.TimeStamp, b);
                    return;
                }
                timeout = false;
                DisplayMessage("信道已写入设备");
                enableChButts.Value = true;
            }
        }


        public static void ReadFromRadio()
        {
            if (!timeout)
            {
                Comms.SendHello();
                DisplayMessage("读取信道数据中...");
                enableChButts.Value = false;
                Comms.SendCommand(Packet.ReadEeprom, (ushort)0, (ushort)128, Comms.TimeStamp);
                _ = EepromTimeout(9000);
            }
        }

        public static void WriteToRadio()
        {
            if (!timeout)
            {
                if (MessageBox.Show("确定将配置写入设备？", "Quansheng Dock", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    Comms.SendHello();
                    DisplayMessage("写入信道数据中...");
                    enableChButts.Value = false;
                    EepromDataWritten(-128);
                    _ = EepromTimeout(20000);
                }
            }
        }

        private static async Task EepromTimeout(int millis)
        {
            timeout = true;
            await Task.Delay(millis);
            if(timeout)
            {
                timeout = false;
                DisplayMessage("写入配置超时");
                enableChButts.Value = true;
            }
        }


        public static void LoadFromDisk()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "选择信道文件",
                Filter = "CHAN Files|*.chan|All Files|*.*",
                InitialDirectory = UserFolder.Dir
            };
            if (openFileDialog.ShowDialog() ?? false)
            {
                byte[] b;
                try { b = File.ReadAllBytes(openFileDialog.FileName); } catch 
                {
                    DisplayMessage("Unable to open file");
                    return;
                }
                if (b.Length == 6600)
                {
                    Array.Copy(b, 0, DataBytes, 0, 3200);
                    Array.Copy(b, 3200, NameBytes, 0, 3200);
                    Array.Copy(b, 6400, AttrBytes, 0, 200);
                    Sanitize();
                    FilterUsed();
                    Refresh();
                    DisplayMessage("信道文件加载成功");
                }
                else
                    DisplayMessage("文件大小错误");
            }
        }

        public static void SaveToDisk() 
        {
            SaveFileDialog saveFileDialog = new()
            {
                Title = "保存信道文件",
                Filter = "CHAN Files|*.chan|All Files|*.*",
                InitialDirectory = UserFolder.Dir
            };
            if(saveFileDialog.ShowDialog() ?? false) 
            {
                try
                {
                    File.WriteAllBytes(saveFileDialog.FileName, DataBytes.Concat(NameBytes).Concat(AttrBytes).ToArray());
                }
                catch
                {
                    DisplayMessage("写入文件失败");
                    return;
                }
                DisplayMessage("信道保存成功");
            }
        }

        private static void DisplayMessage(string s)
        {
            message.Value = s;
            _ = ClearMessage(s);
        }

        private static async Task ClearMessage(string s)
        {
            await Task.Delay(5000);
            if (message.Value.Equals(s))
                message.Value = string.Empty;
        }
    }


}
