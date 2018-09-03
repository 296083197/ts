//#define LOCAL_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace ts
{
    public class RealtimeDataCenter
    {
        public enum EDataSource
        {
            sinajs,
            tradeX,
        }
        public class FinancialData
        {
            public decimal EPS_lastYear { get; set; }
            public decimal EPS_lastQ4 { get; set; }
            public decimal EPS_thisYear { get; set; }
            public decimal Flow_of_equity { get; set; }
            public decimal Total_share_capital { get; set; }
        }

        public class RealtimeData
        {
            public string code { get; set; }
            public decimal H { get; set; }
            public decimal L { get; set; }
            public decimal O { get; set; }
            public decimal C { get; set; }
            public decimal lastC { get; set; }
            public DateTime dt { get; set; }
            public decimal V { get; set; }
            public decimal vm { get; set; }
            public decimal[] buy5c = new decimal[5];
            public decimal[] buy5v = new decimal[5];
            public decimal[] sell5c = new decimal[5];
            public decimal[] sell5v = new decimal[5];
            public FinancialData fd { get; set; }
        }

        static RealtimeDataCenter s_instance = null;
        List<Thread> ltTh = new List<Thread>();
        List<ThreadObj> ltThObj = new List<ThreadObj>();
        //SortedSet<string> ssCode = new SortedSet<string>();
        Dictionary<string, List<RecvRealtimeData>> dcCodeRRD = new Dictionary<string, List<RecvRealtimeData>>();
        Dictionary<RecvRealtimeData, int> dcRRDReferencedCount = new Dictionary<RecvRealtimeData, int>();
        Dictionary<string, RealtimeData> dcCodeLastRd = new Dictionary<string, RealtimeData>();
        Dictionary<RecvRealtimeData, List<RealtimeData>> dcPushData = new Dictionary<RecvRealtimeData, List<RealtimeData>>();
        private static object lockRsrc = new object(); //resource
        private static object lockData = new object();
        private static object lockHttp = new object();
        private int idxThObjForNewRegistry = 0;
        private Dictionary<string, int> dcCodeIdxThObj = new Dictionary<string, int>();
#if (!LOCAL_TEST)
        Thread thSendRd2Interface = null;
#endif
        private List<RealtimeData> ltRd = new List<RealtimeData>();
        //private List<KeyValuePair<RealtimeData, RecvRealtimeData>> ltRddRd = new List<KeyValuePair<RealtimeData, RecvRealtimeData>>();

        /*
         * 两种创建方式：唯一实例，这也是推荐的使用方式；也可以在某些特殊情况下使用普通new（即手动调用构造函数）
         */
        static public RealtimeDataCenter getInstance(int thCount = 8, string codes = null, EDataSource ds = EDataSource.sinajs)
        {
            if (null == s_instance)
            { s_instance = new RealtimeDataCenter(thCount, codes, ds); }

            return s_instance;
        }

        ~RealtimeDataCenter()
        {
#if (!LOCAL_TEST)
            if (null != s_instance)
            {
                this.thSendRd2Interface.Abort();
                foreach(var item in this.ltTh)
                {
                    item.Abort();
                }
            }
#endif
        }

        public RealtimeDataCenter(int thCount = 8, string codes = null, EDataSource ds = EDataSource.sinajs)
        {
#if (!LOCAL_TEST)
            if (thCount <= 0
                || thCount > 100)
            { throw new Exception("invalid thCount"); }

            List<string> ltCode = new List<string>();
            if (null == codes)
            {
                /*
                StreamReader srCodes = new StreamReader(Program.ramdisk + "\\George\\sql\\objCode.txt", Encoding.Default);
                string line;
                while (null != (line = srCodes.ReadLine()))
                {
                    string[] arrCodes = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in arrCodes)
                    { ltCode.Add(item); }
                }
                */
                DBDataContext db = new DBDataContext();
                var q = from n in db.tbObjs
                        select new { n.code };
                foreach (var item in q)
                { ltCode.Add(item.code); }
            }
            else
            {
                string[] arrCodes = codes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in arrCodes)
                { ltCode.Add(item); }
            }

            if (ltCode.Count <= 0)
            { throw new Exception("null code"); }

            if (ltCode.Count < thCount)
            { thCount = ltCode.Count; }

            int countPlanPerTh = ltCode.Count / thCount;
            int countCurrentTh = countPlanPerTh;
            for (int i = 0; i < thCount; i++)
            {
                if (i == thCount - 1) //lastTh
                { countCurrentTh = ltCode.Count - countPlanPerTh * (thCount - 1); }

                string[] arrCodes = new string[countCurrentTh];
                ltCode.CopyTo(i * countPlanPerTh, arrCodes, 0, countCurrentTh);
                foreach (var item in arrCodes)
                { this.dcCodeIdxThObj.Add(item, i); }

                if (EDataSource.sinajs == ds)
                {
                    var to = new ThreadGetDataFromHttp2(arrCodes, this);
                    var th = new Thread(to.GetData) { IsBackground = true };
                    this.ltTh.Add(th);
                    this.ltThObj.Add(to);

                    th.Start();
                }
                else if (EDataSource.tradeX == ds)
                {
                    var to = new ThreadGetDataFromTradeX(arrCodes, this);
                    var th = new Thread(to.GetData) { IsBackground = true };
                    this.ltTh.Add(th);
                    this.ltThObj.Add(to);

                    th.Start();
                }

                System.Threading.Thread.Sleep(10);
            }

            this.thSendRd2Interface = new Thread(this.sendRd2Interface) { IsBackground = true };
            this.thSendRd2Interface.Start();
#endif
        }

        private void sendRd2Interface()
        {
            List<RealtimeData> ltTmpRd = new List<RealtimeData>();

            while (true)
            {
                int count = this.ltRd.Count;
                if (count <= 0)
                {
                    System.Threading.Thread.Sleep(10);

                    continue;
                }

                lock (lockHttp)
                {
                    ltTmpRd.AddRange(this.ltRd);
                    this.ltRd.Clear();
                }
                foreach (var item in ltTmpRd)
                {
                    //:集合竞价
                    /* use buy5c or sell5c
                    if (0 == item.C)
                    {
                        if (item.buy5c[0] != 0)
                        { }
                    }
                    */

                    if (0 == item.H)
                    { item.H = item.C; }
                    if (0 == item.L)
                    { item.L = item.C; }
                    if (0 == item.O)
                    { item.O = item.C; }
                    ///:~

                    lock (lockRsrc)
                    {
                        if (this.dcCodeLastRd.ContainsKey(item.code))
                        {
                            RealtimeData rd = this.dcCodeLastRd[item.code];
                            if (isOpenCallAuctionTime(rd.dt) || isCloseCallAuctionTime(rd.dt, rd.code))
                            {
                                if (rd.buy5c != item.buy5c || rd.sell5c != item.sell5c || rd.buy5v != item.buy5v || rd.sell5v != item.sell5v || rd.C != item.C)
                                { this.dcCodeLastRd[item.code] = item; }
                                else
                                { continue; }
                            }
                            else
                            {
                                if (rd.H != item.H || rd.L != item.L || rd.O != item.O || rd.C != item.C || rd.fd != item.fd)
                                { this.dcCodeLastRd[item.code] = item; }
                                else
                                { continue; }
                            }
                        }
                        else { this.dcCodeLastRd.Add(item.code, item); }

                        if (this.dcCodeRRD.ContainsKey(item.code))
                        {
                            foreach (var rrd in this.dcCodeRRD[item.code])
                            {
                                pushData(rrd, item);
                            }
                        }
                    }
                }
                ltTmpRd.Clear();
            }
        }

        private bool isOpenCallAuctionTime(DateTime dt)
        {
            string szTime = dt.ToString("HHmmss");
            return (string.Compare(szTime, "091500") >= 0 && string.Compare(szTime, "092500") < 0);
        }

        private bool isCloseCallAuctionTime(DateTime dt, string code)
        {
            if (!code.StartsWith("6"))
            {
                string szTime = dt.ToString("HHmmss");
                return (string.Compare(szTime, "145700") >= 0 && string.Compare(szTime, "150000") < 0);
            }

            return false;
        }

        public string registry(string codes, RecvRealtimeData rrd)
        {
            string bingo = "";

            string[] arrRegistryCode = { };

            if (codes.ToLower() == "all"
                || codes == null
                || codes == "")
            { arrRegistryCode = new string[this.dcCodeIdxThObj.Count]; this.dcCodeIdxThObj.Keys.CopyTo(arrRegistryCode, 0); }
            else
            { arrRegistryCode = codes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }

            lock (lockRsrc)
            {
                createDataList(rrd);

                foreach (var item in arrRegistryCode)
                {
                    //...
                    if (!this.dcCodeIdxThObj.ContainsKey(item))
                    {
                        ThreadObj to = this.ltThObj[idxThObjForNewRegistry];
                        if (to.GetType().Equals(typeof(ThreadGetDataFromTradeX)))
                        {
                            this.dcCodeIdxThObj.Add(item, idxThObjForNewRegistry);
                            ((ThreadGetDataFromTradeX)to).addObj(item);

                            idxThObjForNewRegistry++;
                            if (idxThObjForNewRegistry >= this.ltThObj.Count)
                            { idxThObjForNewRegistry = 0; }
                        }
                    }

                    if (this.dcCodeIdxThObj.ContainsKey(item))
                    {
                        if (!this.dcCodeRRD.ContainsKey(item))
                        {
                            this.dcCodeRRD.Add(item, new List<RecvRealtimeData>());
                        }
                        this.dcCodeRRD[item].Add(rrd);

                        if (!this.dcRRDReferencedCount.ContainsKey(rrd))
                        { this.dcRRDReferencedCount.Add(rrd, 1); }
                        else
                        { this.dcRRDReferencedCount[rrd]++; }

                        bingo += (item + ",");
                    }

                    if (this.dcCodeLastRd.ContainsKey(item))
                    {
                        pushData(rrd, this.dcCodeLastRd[item]);
                    }
                }
            }

            return bingo;
        }

        public string unregistry(string codes, RecvRealtimeData rrd)
        {
            string bingo = "";

            string[] arrRegistryCode = { };

            if (codes.ToLower() == "all"
                || codes == null
                || codes == "")
            { arrRegistryCode = new string[this.dcCodeIdxThObj.Count]; this.dcCodeIdxThObj.Keys.CopyTo(arrRegistryCode, 0); }
            else
            { arrRegistryCode = codes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }

            lock (lockRsrc)
            {
                foreach (var item in arrRegistryCode)
                {
                    if (this.dcCodeIdxThObj.ContainsKey(item))
                    {
                        if (this.dcCodeRRD.ContainsKey(item))
                        {
                            if (this.dcCodeRRD[item].Remove(rrd))
                            {
                                if (this.dcRRDReferencedCount.ContainsKey(rrd))
                                { this.dcRRDReferencedCount[rrd]--; }

                                if (this.dcCodeRRD[item].Count <= 0)
                                {
                                    this.dcCodeRRD.Remove(item);
                                }
                            }
                        }
                    }
                }

                if ((this.dcRRDReferencedCount.ContainsKey(rrd) && this.dcRRDReferencedCount[rrd] <= 0)
                    || !this.dcRRDReferencedCount.ContainsKey(rrd) )
                { deleteDataList(rrd); }
            }

            return bingo;
        }

        public void recv(List<RealtimeData> ltRd)
        {
            lock (lockHttp)
            {
                this.ltRd.AddRange(ltRd);
            }

            return;
        }

        private void createDataList(RecvRealtimeData rrd)
        {
            lock (lockData)
            {
                if (!this.dcPushData.ContainsKey(rrd))
                {
                    this.dcPushData.Add(rrd, new List<RealtimeData>());
                }
            }
        }

        private void deleteDataList(RecvRealtimeData rrd)
        {
            lock (lockData)
            {
                if (this.dcPushData.ContainsKey(rrd))
                {
                    this.dcPushData.Remove(rrd);
                }
            }
        }

        private void pushData(RecvRealtimeData rrd, RealtimeData rd)
        {
            lock (lockData)
            {
                this.dcPushData[rrd].Add(rd);
            }
        }

        public void getData(RecvRealtimeData rrd, ref List<RealtimeData> ltRd)
        {
            lock (lockData)
            {
                if (this.dcPushData.ContainsKey(rrd))
                {
                    ltRd.AddRange(this.dcPushData[rrd]);
                    this.dcPushData[rrd].Clear();
                }
            }
        }
    }
}
