using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

using dotNSASM;
using HID;
using System.Text;

namespace LogHost
{
    public partial class MainForm : Form
    {
        class LogCore : NSASM
        {
            readonly List<byte[]> byteCode;

            LogCore(string[][] code) : base(16, 16, 8, code)
            {
                byteCode = new List<byte[]>();
            }

            public static LogCore GetSimCore(string code)
            {
                var c = Util.GetSegments(code);
                return new LogCore(c);
            }

            protected override NSASM Instance(NSASM super, string[][] code)
            {
                return new LogCore(code);
            }

            public byte[][] GetBytes()
            {
                return byteCode.ToArray();
            }

            protected override void LoadFuncList()
            {
                funcList.Add("cmd", (dst, src, ext) =>
                {
                    if (src != null) return Result.ERR;
                    if (dst == null) return Result.ERR;
                    if (dst.type != RegType.CHAR && dst.type != RegType.INT)
                        return Result.ERR;

                    byte cmd = (byte)((int)dst.data & 0xFF);
                    byteCode.Clear();
                    byteCode.Add(new byte[] { cmd });
                    return Result.OK;
                });

                funcList.Add("clr", (dst, src, ext) =>
                {
                    if (src != null) return Result.ERR;
                    if (dst == null) return Result.ERR;
                    
                    byteCode.Clear();
                    byteCode.Add(new byte[] { 0xFE, 0x00 });
                    return Result.OK;
                });
                funcList.Add("prt", (dst, src, ext) =>
                {
                    if (dst == null) return Result.ERR;
                    if (src == null) return Result.ERR;
                    if (dst.type != RegType.INT)
                        return Result.ERR;
                    if (src.type != RegType.CHAR && src.type != RegType.INT && src.type != RegType.STR)
                        return Result.ERR;

                    byteCode.Clear();
                    byteCode.Add(new byte[] { (byte)((((int)dst.data) & 0x3) + 1) });
                    if (src.type == RegType.STR)
                    {
                        string str = (string)src.data;
                        for (int i = 0; i < str.Length; i++)
                        {
                            if (i > 32) break;
                            byteCode.Add(new byte[] { (byte)str[i] });
                        }
                    }
                    else
                    {
                        byte i;
                        if (src.type == RegType.CHAR)
                            i = (byte)(((char)src.data) & 0xFF);
                        else
                            i = (byte)(((int)src.data) & 0xFF);
                        byteCode.Add(new byte[] { i });
                    }

                    return Result.OK;
                });

                funcList.Add("show", (dst, src, ext) =>
                {
                    if (ext == null) return Result.ERR;
                    if (src == null) return Result.ERR;
                    if (dst == null) return Result.ERR;
                    if (ext.type != RegType.CHAR) return Result.ERR;
                    if (dst.type != RegType.INT) return Result.ERR;
                    if (src.type != RegType.INT) return Result.ERR;

                    byteCode.Clear();
                    byte c = (byte)(((char)ext.data) & 0xFF);
                    byte offset = (byte)(((int)src.data * 16 + (int)dst.data) & 0x1F);
                    byteCode.Add(new byte[] { (byte)(0x20 | offset), c });

                    return Result.OK;
                });
            }

            protected override void LoadParamList()
            {
            
            }
        }

        Hid hid;

        const int BUF_SIZE = 42;

        public MainForm()
        {
            InitializeComponent();
            devList.SelectedIndex = 0;

            hid = new Hid();
            hid.DataReceived += Hid_DataReceived;
            hid.DeviceRemoved += Hid_DeviceRemoved;

            Util.Output = (obj) => outputBox.Text += obj.ToString();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            outputBox.Clear();
            LogCore core = LogCore.GetSimCore(codeBox.Text);
            var result = core.Run();
            if (result != null)
            {
                byte[][] bytes = core.GetBytes();
                outputBox.Text += "Code: ";
                for (int i = 0; i < bytes.Length; i++)
                    for (int j = 0; j < bytes[i].Length; j++)
                        outputBox.Text += (bytes[i][j].ToString("x2") + " ");
                outputBox.Text += "\n";

                if (hid.IsOpen)
                {
                    outputBox.Text += "Sending ...\n";

                    new Thread(new ThreadStart(() =>
                    {
                        Report report; int index = 0;
                        List<byte> buf = new List<byte>();
                        while (index < bytes.Length)
                        {
                            if (buf.Count + bytes[index].Length < BUF_SIZE)
                            {
                                buf.AddRange(bytes[index]);
                                index += 1;
                                if (index >= bytes.Length)
                                {
                                    report = new Report(0x55, buf.ToArray());
                                    hid.Write(report);
                                    buf.Clear();
                                    break;
                                }
                            }
                            else
                            {
                                report = new Report(0x55, buf.ToArray());
                                hid.Write(report);
                                Thread.Sleep(500);
                                buf.Clear();
                            }
                        }
                    })).Start();
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Connect")
            {
                ushort vid = 0x3232; byte mid = 0x02;
                ushort pid = 0xFFFF;
                switch (devList.SelectedIndex)
                {
                    case 0: // NSPAD
                        pid = 52;
                        break;
                    case 1: // NSLOG
                        pid = 46;
                        mid = 0x00;
                        break;
                    case 2: // CTMCU
                        pid = 558;
                        break;
                    case 3: // NTP PRO
                        pid = 91;
                        mid = 0x01;
                        break;
                    case 4: // NSDPC Keypad
                        pid = 92;
                        mid = 0x01;
                        break;
                    case 5: // NTP PRO II
                        pid = 96;
                        mid = 0x01;
                        break;
                    case 6: // PWM4Fan v3
                        pid = 97;
                        mid = 0x00;
                        break;
                    default:
                        break;
                }
                int cnt = 0;
                hid.ListHidDevice(ref cnt);
                var ret = hid.OpenDevice(vid, pid, mid);
                if (ret == Hid.HID_RETURN.SUCCESS)
                {
                    outputBox.Clear();
                    outputBox.Text += ("Connected to: " + devList.SelectedItem + "\n");
                    devList.Enabled = false;
                    btnConnect.Text = "Close";
                }
            }
            else
            {
                if (hid.IsOpen)
                    hid.CloseDevice();
                devList.Enabled = true;
                btnConnect.Text = "Connect";
            }
        }

        private void Hid_DeviceRemoved(object sender, EventArgs e)
        {
            Invoke(new ThreadStart(() => {
                devList.Enabled = true;
                btnConnect.Text = "Connect";
            }));
        }

        private void Hid_DataReceived(object sender, Report e)
        {
            byte[] bytes = e.reportBuff;
            if (this.IsDisposed)
                return;
            Invoke(new ThreadStart(() => {
                outputBox.Text = "Data: ";
                for (int i = 0; i < bytes.Length; i++)
                    outputBox.Text += (bytes[i].ToString("x2") + " ");
                outputBox.Text += "\n";
            }));
        }
    }
}
