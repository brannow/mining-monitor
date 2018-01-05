using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using WebSocketSharp;

namespace FuyukaiMiningClient.Classes.CCMiner
{
    class CCMiner
    {
        private Process ccminer;
        private WebSocket summary;
        private WebSocket hwInfo;
        private WebSocket threads;

        private bool ignoreChain = true;

        private TelemetryData.Rig rig;

        private Dictionary<string, string> data = new Dictionary<string, string>();

        public static IList<IList<KeyValuePair<string, string>>> ResultParser(string data)
        {
            List<IList<KeyValuePair<string, string>>> list = new List<IList<KeyValuePair<string, string>>>();
            Dictionary<string, string> resultSegment = new Dictionary<string, string>();
            String[] segments = data.Split('|');

            foreach (String segment in segments) {
                String[] items = segment.Split(';');
                foreach (String item in items) {
                    String[] keyValue = item.Split('=');
                    if (keyValue.Length == 2) {
                        resultSegment.Add(keyValue[0], keyValue[1]);
                    }
                }

                if (resultSegment.Count > 0) {
                    list.Add(resultSegment.ToList());
                    resultSegment.Clear();
                }
            }

            return list.ToArray();
        }

        public CCMiner(Config cfg, TelemetryData.Rig r)
        {
            this.rig = r;
            Process[] ccminers = Process.GetProcessesByName("ccminer-x64");
            if (ccminers.Length > 0)
            {
                ccminer = ccminers[0];
            }

            summary = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/summary", "text");
            hwInfo = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/hwinfo", "text");
            threads = new WebSocket("ws://" + cfg.CCMinerHost() + ":" + cfg.CCMinerPort() + "/threads", "text");

            summary.OnMessage += (sender, e) => {
                data.Add("summary", e.Data);
                if (sender is WebSocket s)
                {
                    s.CloseAsync();
                }
                else
                {
                    summary.CloseAsync();
                }
                if (!ignoreChain)
                {
                    this.CollectHwInfo();
                }
            };
            summary.OnError += (sender, e) => {
                rig.CCMinerError(this);
            };

            hwInfo.OnMessage += (sender, e) => {
                data.Add("hwInfo", e.Data);
                if (sender is WebSocket s)
                {
                    s.CloseAsync();
                }
                else
                {
                    hwInfo.CloseAsync();
                }
                if (!ignoreChain)
                {
                    this.CollectThreads();
                }
            };
            hwInfo.OnError += (sender, e) => {
                if (sender is WebSocket s) {
                    this.OnError(s);
                }
                this.OnError();
            };

            threads.OnMessage += (sender, e) => {
                data.Add("threads", e.Data);
                if (sender is WebSocket s)
                {
                    s.CloseAsync();
                }
                else
                {
                    threads.CloseAsync();
                }
                if (!ignoreChain)
                {
                    this.CollectDone();
                }
            };
            threads.OnError += (sender, e) => {
                if (sender is WebSocket s)
                {
                    this.OnError(s);
                }
                this.OnError();
            };
        }

        public void Collect()
        {
            Process[] ccminers = Process.GetProcessesByName("ccminer-x64");
            if (ccminers.Length > 0)
            {
                this.CollectStart();
            } else
            {
                this.OnError();
            }
        }

        public void CollectStart()
        {
            data.Clear();
            ignoreChain = false;
            summary.ConnectAsync();
        }

        public void CollectHwInfo()
        {
            hwInfo.ConnectAsync();
        }

        public void CollectThreads()
        {
            threads.ConnectAsync();
        }


        public void CollectDone()
        {
            ignoreChain = true;
            rig.CCMinerDone(this, data["summary"], data["hwInfo"], data["threads"]);
        }

        public void OnError(WebSocket s)
        {
            ignoreChain = true;
            s.CloseAsync();
            rig.CCMinerError(this);
        }

        public void OnError()
        {
            ignoreChain = true;
            rig.CCMinerError(this);
        }

        public void Clear()
        {
            ignoreChain = true;
            data.Clear();
            summary.Close();
            threads.Close();
            hwInfo.Close();
        }
    }
}
