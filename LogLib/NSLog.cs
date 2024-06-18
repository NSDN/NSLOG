using System;
using System.Threading;
using System.Collections.Generic;

using HID;

namespace LogLib
{
    public class NSLog
    {
        Hid hid;

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

        string strBuf;

        public NSLog(bool is64bit = true)
        {
            hid = new Hid(is64bit);
            hid.DataReceived += Hid_DataReceived;
            hid.DeviceRemoved += Hid_DeviceRemoved;

            recBuf = new List<byte>();
            strBuf = "";
        }

        public void Connect()
        {
            ushort vid = 0x3232; byte mid = 0x00;
            ushort pid = 46;
            if (!Connected)
            {
                int cnt = 0;
                hid.ListHidDevice(ref cnt);
                var ret = hid.OpenDevice(vid, pid, mid);
                if (ret == Hid.HID_RETURN.SUCCESS || ret == Hid.HID_RETURN.DEVICE_OPENED)
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
                    Print(0, i % 4, lines[i]);
                }
            }
        }

        public void Print(int x, int y, string str)
        {
            if (Connected)
            {
                if (str.Contains("\n"))
                    str = str.Split('\n')[0];
                strBuf = "";
                for (int i = 0; i < x; i++)
                    strBuf += " ";
                strBuf += str;
                List<byte> buf = new List<byte>();
                buf.Add((byte)((y % 4) + 1));
                buf.AddRange(str2bytes(strBuf, 20));
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                Thread.Sleep(200);
            }
        }

        public void Draw(int x, int y, char c)
        {
            if (Connected)
            {
                List<byte> buf = new List<byte>();
                buf.Add(0xFF);
                buf.Add((byte)(x & 0xFF));
                buf.Add((byte)(y & 0xFF));
                buf.Add((byte)c);
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                Thread.Sleep(20);
            }
        }

        public void Clear()
        {
            if (Connected)
            {
                List<byte> buf = new List<byte>();
                buf.Add(0xFE);
                byte[] bytes = buf.ToArray();
                Report report = new Report(0x55, bytes);
                hid.Write(report);
                Thread.Sleep(100);
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
