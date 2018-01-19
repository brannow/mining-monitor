using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FuyukaiMiningClient.Classes
{
    public struct CCMinerResult
    {
        public GPUThreadResult[] GPUThreadResult;
    }

    class CCMinerCollector
    {
        private Queue<CCMiner> connectionQueue = new Queue<CCMiner>();
        private string host = "";
        private string port = "";
        private TelemetryData.Rig rig;
        private CCMinerResult result;

        public CCMinerCollector(string host, string port, TelemetryData.Rig rig)
        {
            this.host = host;
            this.port = port;
            this.rig = rig;
        }

        private void QueueWorker()
        {
            if (connectionQueue.Count() != 0)
            {
                CCMiner c = connectionQueue.Dequeue();
                c.LoadData();
                return;
            }

            this.rig.CCMinerCollectorDone(this.result);
        }


        public void CollectData()
        {
            Program.WriteLine("Init CCminer Data Collector", false, true);
            this.result = new CCMinerResult();

            Process[] processCheck = Process.GetProcessesByName("ccminer-x64");
            Process[] processCheck32 = Process.GetProcessesByName("ccminer");
            if (processCheck.Length > 0 || processCheck32.Length > 0)
            {
                connectionQueue.Clear();
                connectionQueue.Enqueue(new CCMiner(host, port, CCMinerConnectionType.Threads, this));
            }
        
            this.QueueWorker();
        }

        public void DoneCollectHWThreads(GPUThreadResult[] threads)
        {
            Program.WriteLine("Done Collecting Threads", false, true);
            this.result.GPUThreadResult = threads;
            this.QueueWorker();
        }

        public void Clear()
        {
            connectionQueue.Clear();
        }
    }
}
