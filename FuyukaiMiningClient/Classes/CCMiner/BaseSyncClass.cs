﻿using System;

namespace FuyukaiMiningClient.Classes
{
    abstract class BaseSyncClass : System.ComponentModel.ISynchronizeInvoke
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
    }
}
