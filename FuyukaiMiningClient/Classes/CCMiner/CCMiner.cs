using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Timers;

using WebSocketSharp;

namespace FuyukaiMiningClient.Classes.CCMiner
{
    class CCMiner : System.ComponentModel.ISynchronizeInvoke
    {
        private delegate object GeneralDelegate(Delegate method,
                                            object[] args);

        public bool InvokeRequired { get { return true; } }

        public Object Invoke(Delegate method, object[] args)
        {
            return method.DynamicInvoke(args);
        }

        public IAsyncResult BeginInvoke(Delegate method,
                                        object[] args)
        {
            GeneralDelegate x = Invoke;
            return x.BeginInvoke(method, args, null, x);
        }

        public object EndInvoke(IAsyncResult result)
        {
            GeneralDelegate x = (GeneralDelegate)result.AsyncState;
            return x.EndInvoke(result);
        }


        private WebSocket summary;
        private WebSocket hwInfo;
        private WebSocket threads;
        private TelemetryData.Rig rig;
        private static System.Timers.Timer stateTimer;
        private static uint stateCheckEventsFired = 0;
        private static double runtimeMS = 0;
        public Dictionary<string, string> data = new Dictionary<string, string>();

        public static IList<IList<KeyValuePair<string, string>>> ResultParser(string data)
        {
            List<IList<KeyValuePair<string, string>>> list = new List<IList<KeyValuePair<string, string>>>();
            Dictionary<string, string> resultSegment = new Dictionary<string, string>();
            String[] segments = data.Split('|');

            foreach (String segment in segments)
            {
                String[] items = segment.Split(';');
                foreach (String item in items)
                {
                    String[] keyValue = item.Split('=');
                    if (keyValue.Length == 2)
                    {
                        resultSegment.Add(keyValue[0], keyValue[1]);
                    }
                }

                if (resultSegment.Count > 0)
                {
                    list.Add(resultSegment.ToList());
                    resultSegment.Clear();
                }
            }

            return list.ToArray();
        }

        public CCMiner(Config cfg, TelemetryData.Rig r)
        {
            stateTimer = new System.Timers.Timer(200);
            stateTimer.Elapsed += CheckState;
            stateTimer.SynchronizingObject = this;
            stateTimer.AutoReset = true;

            this.rig = r;
            summary = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/summary", "text")
            {
                WaitTime = TimeSpan.FromSeconds(4)
            };

            hwInfo = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/hwinfo", "text")
            {
                WaitTime = TimeSpan.FromSeconds(4)
            };

            threads = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/threads", "text")
            {
                WaitTime = TimeSpan.FromSeconds(4)
            };

            summary.OnMessage += (sender, e) => {
                Program.WriteLine("CCMiner: Summary data rerceived", false, true);
                data.Add("summary", e.Data);
            };

            hwInfo.OnMessage += (sender, e) => {
                Program.WriteLine("CCMiner: HWInfo data rerceived", false, true);
                data.Add("hwInfo", e.Data);
            };

            threads.OnMessage += (sender, e) => {
                Program.WriteLine("CCMiner: Threads data rerceived", false, true);
                data.Add("threads", e.Data);
            };
        }

        private static void CheckState(Object sender, ElapsedEventArgs e)
        {
            ++stateCheckEventsFired;
            
            if (sender is System.Timers.Timer timer)
            {
                runtimeMS += timer.Interval;

                if (runtimeMS > 4000)
                {
                    timer.Stop();
                    if (timer.SynchronizingObject is CCMiner cm)
                    {
                        cm.OnError();
                    }
                    return;
                }

                if (timer.SynchronizingObject is CCMiner ccminer)
                {
                    if (ccminer.data.Count == 3)
                    {
                        timer.Stop();
                        ccminer.CollectDone();
                    }
                }
            }
        }

        public void Collect()
        {
            Program.WriteLine("Connect to CCMiner", false, true);
            Process[] ccminers = Process.GetProcessesByName("ccminer-x64");
            Process[] ccminer32 = Process.GetProcessesByName("ccminer");
            if (ccminers.Length > 0 || ccminer32.Length > 0)
            {
                Program.WriteLine("CCMiner found", false, true);
                this.CollectStart();
            }
            else
            {
                OnError();
            }
        }

        public void CollectStart()
        {
            data.Clear();
            runtimeMS = 0;
            stateTimer.Start();
            Program.WriteLine("CCMiner: start loading Summary", false, true);
            summary.ConnectAsync();
            Program.WriteLine("CCMiner: start loading HW Info", false, true);
            hwInfo.ConnectAsync();
            Program.WriteLine("CCMiner: start loading Mining Threads", false, true);
            threads.ConnectAsync();
        }


        public void CollectDone()
        {
            stateTimer.Stop();
            summary.CloseAsync();
            threads.CloseAsync();
            hwInfo.CloseAsync();
            rig.CCMinerDone(this, data["summary"], data["hwInfo"], data["threads"]);
        }

        public void OnError()
        {
            Program.WriteLine("CCMiner: OnError", false, true);
            ResetCCMinerConnection();
            rig.CCMinerError(this);
        }

        public void Clear()
        {
            Program.WriteLine("CCMiner: Clear", false, true);
            ResetCCMinerConnection();
        }

        private void ResetCCMinerConnection()
        {
            stateTimer.Stop();
            data.Clear();
            summary.Close();
            threads.Close();
            hwInfo.Close();
        }
    }
}
