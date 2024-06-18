using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

using dotNSASM;
using HID;
using System.Text;

namespace LogHost
{
    public partial class LogForm : Form
    {
        Hid hid;

        public LogForm()
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
            codeBox.Clear();
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

                codeBox.AppendText(Encoding.ASCII.GetString(bytes));
                //codeBox.AppendText("\n");
                codeBox.ScrollToCaret();
            }));
        }
    }
}
