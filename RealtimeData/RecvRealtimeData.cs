using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ts
{
    public class RecvRealtimeData //: System.Windows.Forms.Form
    {
        private SortedSet<string> ssCode = new SortedSet<string>();
        private Thread th = null;
        protected bool bNeedSort = false;

        private DateTime dtTimerLast = DateTime.Now;
        private int iTimerMS = -1;

        protected virtual void recv(RealtimeDataCenter.RealtimeData rd) { }
        protected virtual void sort() { }
        protected virtual void timer() { }

        protected void setTimer(int ms)
        {
            this.iTimerMS = ms;
        }

        protected void start()
        {
            RealtimeDataCenter.getInstance().registry(getCodes(), this);
            this.th = new Thread(this.worker) { IsBackground = true };
            this.th.Start();
        }

        protected void stop()
        {
            RealtimeDataCenter.getInstance().unregistry(getCodes(), this);
            this.th.Abort();
        }

        protected bool registry(string code)
        {
            if (this.ssCode.Contains(code))
            { return false; }

            this.ssCode.Add(code);
            if (this.th != null)
            { RealtimeDataCenter.getInstance().registry(code, this); }

            return true;
        }
        protected void unregistry(string code)
        {
            if (this.ssCode.Contains(code))
            { this.ssCode.Remove(code); }

            if (this.th != null)
            { RealtimeDataCenter.getInstance().unregistry(code, this); }
        }
        private string getCodes()
        {
            string codes = "";
            foreach (var code in this.ssCode)
            { codes += (code + ","); }

            return codes;
        }

        private void worker()
        {
            List<RealtimeDataCenter.RealtimeData> ltRd = new List<RealtimeDataCenter.RealtimeData>();

            bool bHasData = true;
            while (true)
            {
                if (!bHasData)
                {
                    System.Threading.Thread.Sleep(1);
                }

                RealtimeDataCenter.getInstance().getData(this, ref ltRd);
                if (ltRd.Count <= 0)
                {
                    bHasData = false;
                }
                else
                {
                    bHasData = true;

                    this.bNeedSort = false;
                    foreach (var rd in ltRd)
                    {
                        recv(rd); //this.bNeedSort' value may be changed in here
                    }

                    if (bNeedSort)
                    {
                        sort();
                        this.bNeedSort = false;
                    }

                    ltRd.Clear();
                }

                if (this.iTimerMS >= 0)
                {
                    TimeSpan tsNow  = new TimeSpan(DateTime.Now.Ticks);
                    TimeSpan tsLast = new TimeSpan(this.dtTimerLast.Ticks);
                    if (tsNow.Subtract(tsLast).TotalMilliseconds >= this.iTimerMS)
                    {
                        timer();
                        this.dtTimerLast = DateTime.Now;
                    }
                }
            }
        }
    }
}
