using LogLib;
using OpenHardwareMonitor.Hardware;
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;

namespace SysLog
{
    public partial class MainForm : Form
    {
        NSLog log;
        
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        UpdateVisitor updateVisitor;
        Computer computer;

        public MainForm()
        {
            InitializeComponent();

            log = new NSLog();
            updateVisitor = new UpdateVisitor();
            computer = new Computer();
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            workTimer.Enabled = false;
            computer.Close();
            Application.Exit();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            log.Connect();

            computer.Open();
            computer.CPUEnabled = true;
            computer.GPUEnabled = true;

            workTimer.Enabled = true;
        }
        
        public static string RunApp(string filename, string arguments)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = filename;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
 
                using (StreamReader sr = new StreamReader(proc.StandardOutput.BaseStream, Encoding.Default))
                {
                    Thread.Sleep(100);
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    }
                    string txt = sr.ReadToEnd();
                    sr.Close();

                    return txt;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return ex.Message;
            }
        }

        public static string GetLocalIP()
        {
            string result = RunApp("route", "print");
            Match m = Regex.Match(result, @"0.0.0.0\s+0.0.0.0\s+(\d+.\d+.\d+.\d+)\s+(\d+.\d+.\d+.\d+)");
            if (m.Success)
            {
                return m.Groups[2].Value;
            }
            else
            {
                try
                {
                    System.Net.Sockets.TcpClient c = new System.Net.Sockets.TcpClient();
                    c.Connect("www.baidu.com", 80);
                    string ip = ((System.Net.IPEndPoint)c.Client.LocalEndPoint).Address.ToString();
                    c.Close();
                    return ip;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void workTimer_Tick(object sender, EventArgs e)
        {
            float cT, gT, cL, gL;
            GetSystemInfo(out float? cpuTemp, out float? gpuTemp, out float? cpuLoad, out float? gpuLoad);
            cT = cpuTemp.Value; gT = gpuTemp.Value; cL = cpuLoad.Value; gL = gpuLoad.Value;
            text.Text = $"CT: {cT}, GT: {gT}, CL: {cL}, GL: {gL}" + "\n";
            text.Text += "\n" + "CPU:" + string.Format("{0,3:0}", cT) + "\xDF" + "C " + string.Format("{0,5:0.0}", cL) + "%"
                       + "\n" + "GPU:" + string.Format("{0,3:0}", gT) + "\xDF" + "C " + string.Format("{0,5:0.0}", gL) + "%";

            /*
             123456789ABCDEF0
             CPU:100oC  99.9%
             GPU:100oC  99.9%
             */
            log.Print(0, 0, "CPU:" + string.Format("{0,3:0}", cT) + "\xDF" + "C " + string.Format("{0,5:0.0}", cL) + "%");
            log.Print(0, 1, "GPU:" + string.Format("{0,3:0}", gT) + "\xDF" + "C " + string.Format("{0,5:0.0}", gL) + "%");

            if (!log.Connected)
                log.Connect();

            string id = Environment.MachineName + "-" + Environment.UserName;
            string date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string ip = GetLocalIP();
            
            try
            {
                WebRequest req = WebRequest.CreateHttp("http://nya.ac.cn:323/api/set" + $"~id={id}&time={date}&data={ip}");
                WebResponse resp = req.GetResponse();
                StreamReader reader = new StreamReader(resp.GetResponseStream());
                string str = reader.ReadToEnd();
                if (!str.Contains("ADDED") && !str.Contains("UPDATED"))
                {
                    notify.ShowBalloonTip(3000, "IPTracker", "IP track failed, check server or client!", ToolTipIcon.Warning);
                }
                reader.Close();
                resp.Close();
            }
            catch (Exception)
            {

            }
        }
        private void GetSystemInfo(out float? cpuTemp, out float? gpuTemp, out float? cpuLoad, out float? gpuLoad)
        {
            cpuTemp = -274.15f; gpuTemp = -274.15f;
            cpuLoad = -1; gpuLoad = -1;

            computer.Accept(updateVisitor);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                var ware = computer.Hardware[i];
                switch (ware.HardwareType)
                {
                    case HardwareType.CPU:
                        var cpuTemps = from sensor in ware.Sensors
                                     where sensor.SensorType == SensorType.Temperature
                                     where sensor.Name == "CPU Package"
                                     select sensor.Value;
                        if (cpuTemps.First() != null)
                            cpuTemp = cpuTemps.First();
                        var cpuLoads = from sensor in ware.Sensors
                                       where sensor.SensorType == SensorType.Load
                                       where sensor.Name == "CPU Total"
                                       select sensor.Value;
                        if (cpuLoads.First() != null)
                            cpuLoad = cpuLoads.First();
                        break;
                    case HardwareType.GpuNvidia:
                        var gpuTemps = from sensor in ware.Sensors
                                     where sensor.SensorType == SensorType.Temperature
                                     where sensor.Name == "GPU Core"
                                     select sensor.Value;
                        if (gpuTemps.First() != null)
                            gpuTemp = gpuTemps.First();
                        var gpuLoads = from sensor in ware.Sensors
                                       where sensor.SensorType == SensorType.Load
                                       where sensor.Name == "GPU Core"
                                       select sensor.Value;
                        if (gpuLoads.First() != null)
                            gpuLoad = gpuLoads.First();
                        break;
                }
            }
        }
    }
}
