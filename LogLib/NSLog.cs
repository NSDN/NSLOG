using System;
using System.Threading;
using System.Collections.Generic;

using HID;

namespace LogLib
{
    public class NSLog
    {
        Hid hid;
        IntPtr ptr;

        public bool Connected
        {
            get;
            protected set;
        }

        readonly List<byte> recBuf;
        public byte[] ReceivedBytes
        {
            get
            {
                byte[] buf = recBuf.ToArray();
                recBuf.Clear();
                return buf;
            }
        }

        string[] strBuf; int cur;

        public NSLog()
        {
            hid = new Hid();
            hid.DataReceived += Hid_DataReceived;
            hid.DeviceRemoved += Hid_DeviceRemoved;

            ptr = new IntPtr(-1);
            recBuf = new List<byte>();
            strBuf = new string[] { "", "" };
            cur = 0;
        }

        public void Connect()
        {
            ushort vid = 0x3232; byte mid = 0x02;
            ushort pid = 46;
            if (!Connected)
            {
                ptr = hid.OpenDevice(vid, pid, mid);
                if ((int)ptr != -1)
                {
                    Connected = true;
                }
            }
        }

        private byte[] str2bytes(string str, int len)
        {
            byte[] buf = new byte[len];
            for (int i = 0; i < str.Length && i < len; i++)
            {
                buf[i] = (byte)str[i];
            }
            for (int i = 0; i < buf.Length; i++)
            {
                if (buf[i] == 0x00)
                    buf[i] = 0x20;
            }
            return buf;
        }

        public void Print(string str)
        {
            if (Connected)
            {
                string[] lines = str.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (cur < 2)
                    {
                        strBuf[cur] = lines[i];
                        cur += 1;
                    }
                    else
                    {
                        strBuf[0] = strBuf[1];
                        strBuf[1] = lines[i];
                    }
                }
                List<byte> buf = new List<byte>();
                buf.Add(0x01);
                buf.AddRange(str2bytes(strBuf[0], 16));
                buf.AddRange(str2bytes(strBuf[1], 16));
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                Thread.Sleep(100);
            }
        }

        public void Print(int x, int y, string str)
        {
            if (Connected)
            {
                str = str.Split('\n')[0];
                strBuf[y % 2] = "";
                for (int i = 0; i < x; i++)
                    strBuf[y % 2] += " ";
                strBuf[y % 2] += str;
                List<byte> buf = new List<byte>();
                buf.Add(0x01);
                buf.AddRange(str2bytes(strBuf[0], 16));
                buf.AddRange(str2bytes(strBuf[1], 16));
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                cur = 0;
                Thread.Sleep(100);
            }
        }

        public void Draw(int x, int y, char c)
        {
            if (Connected)
            {
                List<byte> buf = new List<byte>();
                byte offset = (byte)((y * 16 + x) & 0x1F);
                buf.Add((byte)(0x20 | offset));
                buf.Add((byte)c);
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                cur = 0;
                Thread.Sleep(10);
            }
        }

        public void Clear()
        {
            if (Connected)
            {
                List<byte> buf = new List<byte>();
                buf.Add(0x00);
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                cur = 0;
                Thread.Sleep(50);
            }
        }

        private void Hid_DeviceRemoved(object sender, EventArgs e)
        {
            Connected = false;
        }

        private void Hid_DataReceived(object sender, Report e)
        {
            byte[] bytes = e.reportBuff;
            recBuf.AddRange(bytes);
        }
    }
}
