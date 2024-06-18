using HID;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SysLog
{
    public class FanCtl
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

        public FanCtl(bool is64bit = true)
        {
            hid = new Hid(is64bit);
            hid.DataReceived += Hid_DataReceived;
            hid.DeviceRemoved += Hid_DeviceRemoved;

            recBuf = new List<byte>();
        }

        public void Connect()
        {
            ushort vid = 0x3232; byte mid = 0x00;
            ushort pid = 97;
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

        public void Set(float speed)
        {
            speed = speed < 0 ? 0 : speed;
            speed = speed > 100 ? 100 : speed;
            if (Connected)
            {
                byte[] bytes = { (byte)(uint)(speed / 100.0f * 255) };
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
