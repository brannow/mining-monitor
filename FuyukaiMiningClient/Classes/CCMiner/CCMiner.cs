using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Threading;

using WebSocketSharp;

namespace FuyukaiMiningClient.Classes
{
    enum CCMinerConnectionType : uint
    {
        Summary = 0,
        HWInfo = 1,
        Threads = 2
    };

    public struct SummaryResult
    {
        public float rigHashRate;
        public long minerUptime;
    }

    public struct GPUHWInfoResult
    {
        public uint bus;

        public float temperature;
    }

    public struct GPUThreadResult
    {
        public uint bus;

        public uint watt;
        public float hashRateK;
    }

    class CCMiner : BaseSyncClass
    {
        private static List<Dictionary<string, string>> ResultParser(string data, uint maxCountSize = 0)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            String[] segments = data.Split('|');

            foreach (String segment in segments)
            {
                Dictionary<string, string> resultSegment = new Dictionary<string, string>();
                String[] items = segment.Split(';');
                foreach (String item in items)
                {
                    String[] keyValue = item.Split('=');
                    if (keyValue.Length == 2)
                    {
                        resultSegment.Add(keyValue[0], keyValue[1]);
                    }
                }

                if (resultSegment.Count > maxCountSize)
                {
                    list.Add(resultSegment);
                }
            }

            return list;
        }

        private readonly string host;
        private readonly string port;
        private readonly CCMinerConnectionType type;

        private static double runtimeMS = 0;
        private static System.Timers.Timer timeoutTimer;
        public const uint timeout = 5000;

        private bool errorReceived = false;
        private string data = "";
        private readonly CCMinerCollector collector;
        private uint timeoutStrike = 0;
        private WebSocket request = null;

        public CCMiner(string host, string port, CCMinerConnectionType type, CCMinerCollector collector)
        {
            this.collector = collector;
            this.host = host;
            this.port = port;
            this.type = type;
        }

        public void LoadData()
        {
            string node = "";
            if (this.type == CCMinerConnectionType.Summary)
            {
                node = "summary";
            }
            if (this.type == CCMinerConnectionType.HWInfo)
            {
                node = "hwinfo";
            }
            if (this.type == CCMinerConnectionType.Threads)
            {
                node = "threads";
            }
            Program.WriteLine("Send CCminer Api Request: " + node, false, true);
            
            if (request != null)
            {
                request.Close();
            }

            Thread.Sleep(250);

            request = new WebSocket("ws://" + this.host + ":" + this.port + "/" + node, "text")
            {
                WaitTime = TimeSpan.FromSeconds(5)
            };

            request.OnMessage += this.MessageReceived;
            request.OnError += this.MessageError;

            timeoutTimer = new System.Timers.Timer(200);
            runtimeMS = 0;
            timeoutTimer.Elapsed += CheckAnswerState;
            timeoutTimer.SynchronizingObject = this;
            timeoutTimer.AutoReset = true;

            timeoutTimer.Start();
            request.ConnectAsync();

        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            Program.WriteLine("CCMiner Message Received", false, true);
            if (sender is WebSocket ws)
            {
                ws.CloseAsync();
            }

            this.data = e.Data;
        }

        public void MessageError(object sender, ErrorEventArgs e)
        {
            Program.WriteLine("CCMiner Error Received", false, true);
            if (sender is WebSocket ws)
            {
                ws.CloseAsync();
            }

            this.errorReceived = true;
        }

        public void CheckAnswerState(Object sender, ElapsedEventArgs e)
        {
            if (sender is System.Timers.Timer timer && timer.SynchronizingObject is CCMiner ccminer)
            {
                runtimeMS += timer.Interval;

                if (((timeoutStrike < 5 && runtimeMS > CCMiner.timeout) && (ccminer.data == null || ccminer.data.Length == 0)) || ccminer.errorReceived)
                {
                    ++timeoutStrike;
                    Program.WriteLine("Timeout Retry ("+ timeoutStrike + "/5)", false, true);
                    timer.Stop();
                    ccminer.LoadData();
                    return;
                }

                if (runtimeMS > CCMiner.timeout || ccminer.errorReceived || (ccminer.data != null && ccminer.data.Length > 0))
                {
                    timer.Stop();
                    ccminer.ParseData();
                }
            }
        }

        public void ParseData()
        {
            Program.WriteLine("Start Parsing", false, true);
            if (this.type == CCMinerConnectionType.Summary)
            {
                collector.DoneCollectSummary(this.ParseSummary());
            }
            if (this.type == CCMinerConnectionType.HWInfo)
            {
                collector.DoneCollectHWinfo(this.ParseHWInfo());
            }
            if (this.type == CCMinerConnectionType.Threads)
            {
                collector.DoneCollectHWThreads(this.ParseThreadResult());
            }
        }

        private SummaryResult ParseSummary()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            SummaryResult sr = new SummaryResult
            {
                rigHashRate = 0,
                minerUptime = 0
            };

            List<Dictionary<string, string>> summaryList = CCMiner.ResultParser(this.data);
            if (summaryList.Count() > 0)
            {
                Dictionary<string, string> summaryResult = summaryList.First();
                foreach (KeyValuePair<string, string> keyValue in summaryResult)
                {
                    if (keyValue.Key == "KHS" && keyValue.Value != null)
                    {
                        sr.rigHashRate = float.Parse(keyValue.Value);
                    }
                    if (keyValue.Key == "UPTIME" && keyValue.Value != null)
                    {
                        sr.minerUptime = long.Parse(keyValue.Value);
                    }
                }
            }

            return sr;
        }

        private GPUHWInfoResult[] ParseHWInfo()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;


            List<Dictionary<string, string>> hwInfoGpuList = CCMiner.ResultParser(this.data, 10);
            if (hwInfoGpuList.Count() > 0)
            {
                GPUHWInfoResult[] hwInfos = new GPUHWInfoResult[hwInfoGpuList.Count()];
                uint i = 0;
                foreach (Dictionary<string, string> gpuData in hwInfoGpuList)
                {
                    foreach (KeyValuePair<string, string> keyValue in gpuData)
                    {
                        if (keyValue.Key == "BUS")
                        {
                            hwInfos[i].bus = uint.Parse(keyValue.Value);
                        }
                        if (keyValue.Key == "TEMP")
                        {
                            hwInfos[i].temperature = float.Parse(keyValue.Value);
                        }
                    }

                    i++;
                }

                return hwInfos;
            }


            return new GPUHWInfoResult[0];
        }

        private GPUThreadResult[] ParseThreadResult()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            List<Dictionary<string, string>> threadData = CCMiner.ResultParser(this.data, 10);
            if (threadData.Count() > 0)
            {
                GPUThreadResult[] threadResults = new GPUThreadResult[threadData.Count()];
                uint i = 0;
                foreach (Dictionary<string, string> gpuData in threadData)
                {
                    foreach (KeyValuePair<string, string> keyValue in gpuData)
                    {
                        if (keyValue.Key == "POWER")
                        {
                            threadResults[i].watt = uint.Parse(keyValue.Value);
                        }
                        if (keyValue.Key == "BUS")
                        {
                            threadResults[i].bus = uint.Parse(keyValue.Value);
                        }
                        if (keyValue.Key == "KHS")
                        {
                            threadResults[i].hashRateK = float.Parse(keyValue.Value);
                        }
                    }

                    i++;
                }

                return threadResults;
            }


            return new GPUThreadResult[0];
        }
    }
}
