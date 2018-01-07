using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

using WebSocketSharp;

namespace FuyukaiMiningClient.Classes.CCMiner
{
    class CCMiner
    {
        private WebSocket summary;
        private WebSocket hwInfo;
        private WebSocket threads;

        private TelemetryData.Rig rig;

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
                data.Add("summary", e.Data);
                if (sender is WebSocket s)
                {
                    s.Close();
                    CollectHwInfo();
                }
            };
            summary.OnError += (sender, e) => {
                if (sender is WebSocket s)
                {
                    OnError(s);
                }
            };

            hwInfo.OnMessage += (sender, e) => {
                data.Add("hwInfo", e.Data);
                if (sender is WebSocket s)
                {
                    s.Close();
                    CollectThreads();
                }
            };
            hwInfo.OnError += (sender, e) => {
                if (sender is WebSocket s)
                {
                    OnError(s);
                }
            };

            threads.OnMessage += (sender, e) => {
                data.Add("threads", e.Data);
                if (sender is WebSocket s)
                {
                    s.Close();
                    CollectDone();
                }

            };
            threads.OnError += (sender, e) => {
                if (sender is WebSocket s)
                {
                    OnError(s);
                }
            };
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
            Program.WriteLine("CCMiner: Load Summary", false, true);
            data.Clear();
            summary.Connect();
        }

        public void CollectHwInfo()
        {
            Program.WriteLine("CCMiner: Load HW Info", false, true);
            hwInfo.Connect();
        }

        public void CollectThreads()
        {
            Program.WriteLine("CCMiner: Load Mining Threads", false, true);
            threads.Connect();
        }


        public void CollectDone()
        {
            rig.CCMinerDone(this, data["summary"], data["hwInfo"], data["threads"]);
        }

        public void OnError(WebSocket s)
        {
            Program.WriteLine("CCMiner: OnError", false, true);
            s.Close();
            rig.CCMinerError(this);
        }

        public void OnError()
        {
            Program.WriteLine("CCMiner: OnError", false, true);
            rig.CCMinerError(this);
        }

        public void Clear()
        {
            Program.WriteLine("CCMiner: Clear", false, true);
            data.Clear();
            summary.Close();
            threads.Close();
            hwInfo.Close();
        }
    }
}
