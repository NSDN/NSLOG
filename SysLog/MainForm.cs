using LogLib;
using OpenHardwareMonitor.Hardware;
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysLog
{
    public partial class MainForm : Form
    {
        NSLog log;
        FanCtl fan;
        
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
        Thread fanThread;

        private void GUI_Delay(int ms)
        {
            DateTime start = DateTime.Now;
            while (start.AddMilliseconds(ms) > DateTime.Now)
                Application.DoEvents();
        }

        private float speed = 50;

        public MainForm()
        {
            InitializeComponent();

            log = new NSLog(Environment.Is64BitProcess);
            fan = new FanCtl(Environment.Is64BitProcess);
            updateVisitor = new UpdateVisitor();
            computer = new Computer();

            fanThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        float t = tempQueue.Average();
                        float sT = 30, eT = 80;

                        if (t < sT)
                            speed = 0;
                        else if (t > eT)
                            speed = 100;
                        else
                            speed = (float)Math.Floor(100 * Math.Sin(Math.PI / 2 * (t - sT) / (eT - sT)));

                        if (!fan.Connected)
                            fan.Connect();
                        fan.Set(speed);

                        Thread.Sleep(3000);
                    }
                    catch (IOException)
                    {

                    }
                    catch (InvalidOperationException)
                    {

                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                }
            }));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fanThread.Abort();
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

            fanThread.Start();
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

        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        class WeatherInfo
        {
            [JsonProperty()]
            string obsTime;
            [JsonProperty()]
            public string temp;
            [JsonProperty()]
            public string feelsLike;
            [JsonProperty()]
            public string icon;
            [JsonProperty()]
            public string text;
            [JsonProperty()]
            public string wind360;
            [JsonProperty()]
            public string windDir;
            [JsonProperty()]
            public string windScale;
            [JsonProperty()]
            public string windSpeed;
            [JsonProperty()]
            public string humidity;
            [JsonProperty()]
            public string precip;
            [JsonProperty()]
            public string pressure;
            [JsonProperty()]
            public string vis;
            [JsonProperty()]
            public string cloud;
            [JsonProperty()]
            public string dew;
        }

        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        class WeatherRefer
        {
            [JsonProperty()]
            List<string> sources;
            [JsonProperty()]
            List<string> license;
        }

        [JsonObject(MemberSerialization =MemberSerialization.OptIn)]
        class WeatherReq
        {
            [JsonProperty()]
            string code;
            [JsonProperty()]
            string updateTime;
            [JsonProperty()]
            string fxLink;
            [JsonProperty()]
            public WeatherInfo now;
            [JsonProperty()]
            WeatherRefer refer;
        }

        const string oC = "\xDF" + "C";

        private WeatherReq weatherReq;
        private float countSeconds = 30 * 60;

        private int countIPTracker = 0;

        private void workTimer_Tick(object sender, EventArgs e)
        {
            if (countSeconds >= 30 * 60 * 1000 / workTimer.Interval)
            {
                countSeconds = 0;

                new Task(new Action(() =>
                {
                    string key = "10afedcbb7e4437491e68a9dc661cf66";
                    string loc = "101110109";
                    string lang = "en";
                    string res = HttpGETGzip("https://devapi.qweather.com/v7/weather/now" + $"?key={key}&location={loc}&lang={lang}");
                    if (res != "")
                    {
                        weatherReq = JsonConvert.DeserializeObject<WeatherReq>(res);
                    }
                })).Start();
            }
            else
            {
                countSeconds += (workTimer.Interval / 1000.0f);
            }

            new Task(new Action(() =>
            {
                GetSystemInfo(out List<float> cT, out List<float> cL, out List<float> gT, out List<float> gL);
                //text.Text = "CT:CL" + "\n";
                //for (int i = 0; i < cT.Count; i++)
                //    text.Text += $"{cT[i]}:{cL[i]}\n";
                //text.Text += "GT:GL" + "\n";
                //for (int i = 0; i < gT.Count; i++)
                //    text.Text += $"{gT[i]}:{gL[i]}\n";

                /*
                  12345678901234567890
                  CPU  T:100oC U:99.9%
                  GPU  T:100oC U:99.9%
                  GPU  T0:99oC T1:99oC
                         Cloudy
                  Act:-10oC  Fel:-10oC
                 */
                if (cT.Count > 1)
                    log.Print(0, 0, $"CPU  T0:{cT[0],2:0}{oC} T1:{cT[1],2:0}{oC}");
                else
                    log.Print(0, 0, $"CPU  T:{cT[0],3:0}{oC} U:{Math.Min(cL[0], 99.9),4:0.0}%");
                if (gT.Count > 1)
                    log.Print(0, 1, $"GPU  T0:{gT[0],2:0}{oC} T1:{gT[1],2:0}{oC}");
                else
                    log.Print(0, 1, $"GPU  T:{gT[0],3:0}{oC} U:{Math.Min(gL[0], 99.9),4:0.0}%");

                if (weatherReq != null)
                {
                    int t = int.Parse(weatherReq.now.temp);
                    int f = int.Parse(weatherReq.now.feelsLike);
                    string info = weatherReq.now.text;
                    string space(int l)
                    {
                        string res = "";
                        for (int i = 0; i < l; i++)
                            res += " ";
                        return res;
                    }
                    if ((info.Length % 2) == 1)
                        info += "!";
                    if (info.Length < 20)
                        info = space((20 - info.Length) / 2) + info + space((20 - info.Length) / 2);
                    info = info.Substring(0, 20);
                    string[] ani = { "\xA2 ", " \xA3" };
                    log.Print(0, 2, info);
                    log.Print(0, 3, $"Act:{t,3:0}{oC}{ani[(int)countSeconds % ani.Length]}Fel:{f,3:0}{oC}");
                }

                if (!log.Connected)
                {
                    log.Connect();
                    log.Clear();
                }

                if (countIPTracker < 5)
                    countIPTracker += 1;
                else
                {
                    countIPTracker = 0;

                    string id = Environment.MachineName + "-" + Environment.UserName;
                    string date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                    string ip = GetLocalIP();

                    string res = HttpGET("http://nya.ac.cn:323/api/set" + $"~id={id}&time={date}&data={ip}");
                    if (res != "")
                        if (!res.Contains("ADDED") && !res.Contains("UPDATED"))
                        {
                            notify.ShowBalloonTip(3000, "IPTracker", "IP track failed, check server or client!", ToolTipIcon.Warning);
                        }
                }

                if (gT.Count > 1)
                {
                    while (tempQueue.Count >= 3)
                        tempQueue.Dequeue();
                    tempQueue.Enqueue(gT[1]);
                }
            })).Start();
        }

        private Queue<float> tempQueue = new Queue<float>();

        private string HttpGETGzip(string url)
        {
            try
            {
                WebRequest req = WebRequest.CreateHttp(url);
                WebResponse resp = req.GetResponse();
                GZipStream gZipStream = new GZipStream(resp.GetResponseStream(), CompressionMode.Decompress);
                MemoryStream outBuffer = new MemoryStream();
                byte[] block = new byte[1024];
                while (true)
                {
                    int read = gZipStream.Read(block, 0, block.Length);
                    if (read <= 0)
                        break;
                    else
                        outBuffer.Write(block, 0, read);
                }
                gZipStream.Close();
                resp.Close();
                return Encoding.UTF8.GetString(outBuffer.ToArray());
            }
            catch (Exception)
            {
                return "";
            }
        }

        private string HttpGET(string url)
        {
            try
            {
                WebRequest req = WebRequest.CreateHttp(url);
                WebResponse resp = req.GetResponse();
                StreamReader reader = new StreamReader(resp.GetResponseStream());
                string str = reader.ReadToEnd();
                reader.Close();
                resp.Close();
                return str;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void GetSystemInfo(out List<float> cT, out List<float> cL, out List<float> gT, out List<float> gL)
        {
            cT = new List<float>();
            gT = new List<float>();
            cL = new List<float>();
            gL = new List<float>();

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
                            cT.Add(cpuTemps.First().Value);
                        var cpuLoads = from sensor in ware.Sensors
                                       where sensor.SensorType == SensorType.Load
                                       where sensor.Name == "CPU Total"
                                       select sensor.Value;
                        if (cpuLoads.First() != null)
                            cL.Add(cpuLoads.First().Value);
                        break;
                    case HardwareType.GpuNvidia:
                        var gpuTemps = from sensor in ware.Sensors
                                     where sensor.SensorType == SensorType.Temperature
                                     where sensor.Name == "GPU Core"
                                     select sensor.Value;
                        if (gpuTemps.First() != null)
                            gT.Add(gpuTemps.First().Value);
                        var gpuLoads = from sensor in ware.Sensors
                                       where sensor.SensorType == SensorType.Load
                                       where sensor.Name == "GPU Core"
                                       select sensor.Value;
                        if (gpuLoads.First() != null)
                            gL.Add(gpuLoads.First().Value);
                        break;
                }
            }
        }
    }
}
