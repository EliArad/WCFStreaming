using System;
//using System.Collections.Generic;
//using System.Text;
using System.Threading;

namespace FileServerWinClientLib
{
    class ThreadedExecuter<T> where T : class
    {
        public delegate void CallBackDelegate(T returnValue);
        public delegate T MethodDelegate();
        private CallBackDelegate callback;
        private MethodDelegate method;

        private Thread t;
        public Thread handle
        {
            get
            {
                return t;
            }
        }

        public ThreadedExecuter(MethodDelegate method, CallBackDelegate callback)
        {
            this.method = method;
            this.callback = callback;
            t = new Thread(this.Process);
        }
        public void Start()
        {
            t.Start();
        }
        public void Abort()
        {
            t.Abort();
            callback(null); //can be left out depending on your needs
        }
        private void Process()
        {
            try
            {
                T stuffReturned = method();
                callback(stuffReturned);
            }
            catch (Exception err)
            {
                // set callback here..
            }
        }
    }
}