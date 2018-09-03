using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TradeAPI;

namespace ts
{
    class coRDC
    {
        public class Bar1M
        {
            public DateTime dt { get; set; }
            public decimal O { get; set; }
            public decimal C { get; set; }
            public decimal H { get; set; }
            public decimal L { get; set; }
            public decimal V { get; set; }
            public decimal vm { get; set; }
        }
        public class TodayXDR
        {
            public decimal presentCash { get; set; }
            public decimal presentStock { get; set; }
        }

        object lockData = new object();
        List<RealtimeDataCenter.RealtimeData> ltDataQuotes = new List<RealtimeDataCenter.RealtimeData>();
        List<KeyValuePair<string, List<Bar1M>>> ltData1M = new List<KeyValuePair<string, List<Bar1M>>>();
        List<KeyValuePair<string, TodayXDR>> ltDataTodayXDR = new List<KeyValuePair<string, TodayXDR>>();
        object lockCode = new object();
        Queue<KeyValuePair<string, short>> quCodeGetCounts = new Queue<KeyValuePair<string, short>>();
        List<Thread> ltTh = new List<Thread>();
        object lockReferenced = new object();
        short nReferencedCount = 0;

        public bool getAllSecuritys(ref Dictionary<string, string> dcCodeNameShenzhen, ref Dictionary<string, string> dcCodeNameShanghai)
        {
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);
            short MAX_COUNT_PER_TIMES = 1000;
            byte MARKET_SHENZHEN = 0;
            byte MARKET_SHANGHAI = 1;

            int idConn = ThreadGetDataFromTradeX.connectTDX();
            if (idConn < 0)
            { return false; }

            byte[] markets = new byte[] { MARKET_SHENZHEN, MARKET_SHANGHAI };

            foreach (var market in markets)
            {
                short count = 0;
                bool bRet = TradeXB.TdxHq_GetSecurityCount(idConn, market, ref count, sErrInfo);
                if (!bRet)
                { return false; }

                int loops = count / MAX_COUNT_PER_TIMES;
                if (count % MAX_COUNT_PER_TIMES > 0)
                { loops++; }
                for (short i = 0; i < loops; i++)
                {
                    bRet = TradeXB.TdxHq_GetSecurityList(idConn, market, (short)(i * MAX_COUNT_PER_TIMES), ref count, sResult, sErrInfo);
                    if (bRet)
                    {
                        string[] rows = sResult.ToString().Split(new char[] { '\n' });
                        foreach (var row in rows.Skip(1))
                        {
                            string[] cols = row.Split(new char[] { '\t' });
                            if (cols.Count() >= 8)
                            {
                                if (market == MARKET_SHENZHEN)
                                { dcCodeNameShenzhen.Add(cols[0], cols[2]); }
                                else if (market == MARKET_SHANGHAI)
                                { dcCodeNameShanghai.Add(cols[0], cols[2]); }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool getAllQuotes(ref List<RealtimeDataCenter.RealtimeData> ltData)
        {
            preprocessAllCodeFromDB();

            StringBuilder sErrInfo = new StringBuilder(256);

            nReferencedCount = 8;
            for (int i = 0; i < nReferencedCount; i++)
            {
                Thread th = new Thread(this.GetQuotesFromTradeXB) { IsBackground = true };
                ltTh.Add(th);
                th.Start();
            }

            while (getReferenced() > 0)
            {
                System.Threading.Thread.Sleep(1);
            }

            foreach (var item in ltTh)
            { item.Abort(); }
            ltTh.Clear();

            quCodeGetCounts.Clear();
            ltData = new List<RealtimeDataCenter.RealtimeData>(this.ltDataQuotes);
            this.ltDataQuotes.Clear();

            return true;
        }

        private void GetQuotesFromTradeXB()
        {
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);
            
            int idConn = -1;
            while (true)
            {
                if (idConn < 0)
                { idConn = ThreadGetDataFromTradeX.connectTDX(); }
                if (idConn < 0)
                {
                    Console.WriteLine("没有可用的TDX行情服务器！  请检查网络...... 重连......" + DateTime.Now);

                    System.Threading.Thread.Sleep(1000);

                    continue;
                }

                List<string> ltCode = new List<string>();
                List<byte> ltMarket = new List<byte>();
                List<short> ltGetCounts = new List<short>();

                bool bRet = true;
                string code = "";
                short getCounts = 0;
                while (getCode(ref code, ref getCounts))
                {
                    ltCode.Add(code);
                    ltMarket.Add(ThreadGetDataFromTradeX.getMarketType(code));
                    ltGetCounts.Add(getCounts);

                    if (ltCode.Count >= ThreadGetDataFromTradeX.MAX_COUNT_PER_TIMES)
                    { break; }
                }

                short count = (short)ltCode.Count;
                if (count > 0)
                {
                    sResult.Clear();
                    sErrInfo.Clear();

                    string[] arryCode = new string[count];
                    byte[] arryMarket = new byte[count];
                    ltCode.CopyTo(arryCode);
                    ltMarket.CopyTo(arryMarket);
                    short c = count;
                    bRet = TradeXB.TdxHq_GetSecurityQuotes(idConn, arryMarket, arryCode, ref c, sResult, sErrInfo);
                    if (bRet)
                    {
                        Console.Write(0);

                        string[] rows = sResult.ToString().Split(new char[] { '\n' });
                        foreach (var row in rows)
                        {
                            RealtimeDataCenter.RealtimeData rd = ThreadGetDataFromTradeX.analyseRD(row);
                            if (null != rd)
                            { pushDataQuotes(rd); }
                        }
                    }
                    else
                    {
                        Console.Write(0);

                        for (int i = 0; i < ltCode.Count; i++)
                        {
                            if (ltGetCounts[i] < 3)
                            {
                                pushCode(ltCode[i], ++ltGetCounts[i]);
                            }
                        }

                        string szErrorInfo = sErrInfo.ToString();
                        Console.WriteLine(szErrorInfo);
                        if (szErrorInfo.Contains("连接断开"))
                        {
                            System.Threading.Thread.Sleep(100);
                            idConn = -1;

                            break;
                        }
                        else
                        { bRet = true; }
                    }
                }

                if (bRet && count <= 0)
                {
                    if (idConn >= 0)
                    { TradeXB.TdxHq_Disconnect(idConn); }

                    break;
                }
            }

            subtractReferenced();
        }

        public bool getAll1MBars(ref List<KeyValuePair<string, List<Bar1M>>> ltData)
        {
            preprocessAllCodeFromDB();

            StringBuilder sErrInfo = new StringBuilder(256);
            nReferencedCount = 20;
            for (int i = 0; i < nReferencedCount; i++)
            {
                Thread th = new Thread(this.Get1MBarsFromTradeXB) { IsBackground = true };
                ltTh.Add(th);
                th.Start();
            }

            while (getReferenced() > 0)
            {
                System.Threading.Thread.Sleep(1);
            }

            foreach (var item in ltTh)
            { item.Abort(); }
            ltTh.Clear();

            quCodeGetCounts.Clear();
            ltData = new List<KeyValuePair<string, List<Bar1M>>>(this.ltData1M);
            this.ltData1M.Clear();

            return true;
        }

        private void Get1MBarsFromTradeXB()
        {
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);

            int idConn = 0;
            while (true)
            {
                idConn = ThreadGetDataFromTradeX.connectTDX();
                if (idConn < 0)
                {
                    Console.WriteLine("没有可用的TDX行情服务器！  请检查网络...... 重连......" + DateTime.Now);

                    System.Threading.Thread.Sleep(1000);

                    continue;
                }

                bool bRet = true;
                string code = "";
                short getCounts = -1;
                while (getCode(ref code, ref getCounts))
                {
                    sResult.Clear();
                    sErrInfo.Clear();

                    short c = 240;
                    byte market = 0;
                    if (code.Substring(0, 1) == "6")
                    { market = 1; }

                    bRet = TradeXB.TdxHq_GetSecurityBars(idConn, 7, market, code, 0, ref c, sResult, sErrInfo);
                    if (bRet)
                    {
                        List<Bar1M> ltTmpData = new List<Bar1M>();
                        DateTime? lastDay = null;
                        string[] rows = sResult.ToString().Split(new char[] { '\n' });
                        foreach (var row in rows)
                        {
                            string[] cols = row.Split(new char[] { '\t' });
                            if (cols.Count() >= 7
                                && cols[0].StartsWith("2"))
                            {
                                Bar1M b1m = new Bar1M()
                                {
                                    dt = Convert.ToDateTime(cols[0]),
                                    O = Convert.ToDecimal(cols[1]),
                                    C = Convert.ToDecimal(cols[2]),
                                    H = Convert.ToDecimal(cols[3]),
                                    L = Convert.ToDecimal(cols[4]),
                                    V = Convert.ToDecimal(cols[5]),
                                    vm = Convert.ToDecimal(cols[6]),
                                };
                                lastDay = b1m.dt.Date;
                                ltTmpData.Add(b1m);
                            }
                        }

                        List<Bar1M> ltB1M = new List<Bar1M>();
                        foreach (var b1m in ltTmpData)
                        {
                            if (b1m.dt.Date == lastDay)
                            {
                                ltB1M.Add(b1m);
                            }
                        }
                        if (ltB1M.Count > 0)
                        { pushData1M(code, ltB1M); }
                    }
                    else
                    {
                        Console.Write(0);

                        if (getCounts < 3)
                        {
                            pushCode(code, ++getCounts);
                        }

                        string szErrorInfo = sErrInfo.ToString();
                        Console.WriteLine(szErrorInfo);
                        if (szErrorInfo.Contains("连接断开"))
                        {
                            System.Threading.Thread.Sleep(100);
                            idConn = -1;

                            break;
                        }
                        else
                        { bRet = true; } //no break
                    }
                }

                if (bRet)
                {
                    if (idConn >= 0)
                    { TradeXB.TdxHq_Disconnect(idConn); }

                    break;
                }
            }

            subtractReferenced();
        }

        public bool getAllTodayXDR(ref List<KeyValuePair<string, TodayXDR>> ltData)
        {
            preprocessAllCodeFromDB();

            StringBuilder sErrInfo = new StringBuilder(256);
            nReferencedCount = 20;
            for (int i = 0; i < nReferencedCount; i++)
            {
                Thread th = new Thread(this.GetTodayXDRFromTradeXB) { IsBackground = true };
                ltTh.Add(th);
                th.Start();
            }

            while (getReferenced() > 0)
            {
                System.Threading.Thread.Sleep(1);
            }

            foreach (var item in ltTh)
            { item.Abort(); }
            ltTh.Clear();

            quCodeGetCounts.Clear();
            ltData = new List<KeyValuePair<string, TodayXDR>>(this.ltDataTodayXDR);
            this.ltDataTodayXDR.Clear();

            return true;
        }

        private void GetTodayXDRFromTradeXB()
        {
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);

            int idConn = 0;
            while (true)
            {
                idConn = ThreadGetDataFromTradeX.connectTDX();
                if (idConn < 0)
                {
                    Console.WriteLine("没有可用的TDX行情服务器！  请检查网络...... 重连......" + DateTime.Now);

                    System.Threading.Thread.Sleep(1000);

                    continue;
                }

                bool bRet = true;
                string code = "";
                short getCounts = -1;
                while (getCode(ref code, ref getCounts))
                {
                    sResult.Clear();
                    sErrInfo.Clear();

                    byte market = 0;
                    if (code.Substring(0, 1) == "6")
                    { market = 1; }

                    bRet = TradeXB.TdxHq_GetXDXRInfo(idConn, market, code, sResult, sErrInfo);
                    if (bRet)
                    {
                        string[] rows = sResult.ToString().Split(new char[] { '\n' });
                        foreach (var row in rows)
                        {
                            string[] cols = row.Split(new char[] { '\t' });
                            if (cols.Count() >= 8
                                && (cols[0] == "0" || cols[0] == "1")
                                && cols[2] == DateTime.Now.ToString("yyyyMMdd")
                                && cols[3] == "1")
                            {
                                TodayXDR xdr = new TodayXDR
                                {
                                    presentCash = Convert.ToDecimal(cols[4]),
                                    presentStock = Convert.ToDecimal(cols[6]),
                                };
                                pushDataTodayXDR(code, xdr);
                            }
                        }
                    }
                    else
                    {
                        Console.Write(0);

                        if (getCounts < 3)
                        {
                            pushCode(code, ++getCounts);
                        }

                        string szErrorInfo = sErrInfo.ToString();
                        Console.WriteLine(szErrorInfo);
                        if (szErrorInfo.Contains("连接断开"))
                        {
                            System.Threading.Thread.Sleep(100);
                            idConn = -1;

                            break;
                        }
                        else
                        { bRet = true; } //no break
                    }
                }

                if (bRet)
                {
                    if (idConn >= 0)
                    { TradeXB.TdxHq_Disconnect(idConn); }

                    break;
                }
            }

            subtractReferenced();
        }

        private void preprocessAllCodeFromDB()
        {
            DBDataContext db = new DBDataContext();
            var q = from n in db.tbObjs
                    select new { n.code };
            foreach (var item in q)
            {
                if (item.code != "399001"
                    && item.code != "399006"
                    && item.code != "999999")
                { pushCode(item.code); }
            }
        }

        private bool getCode(ref string code, ref short getCounts)
        {
            bool ret = false;
            lock (lockCode)
            {
                if (quCodeGetCounts.Count > 0)
                {
                    ret = true;
                    KeyValuePair<string, short> kv = quCodeGetCounts.Dequeue();
                    code = kv.Key;
                    getCounts = kv.Value;
                }
            }

            return ret;
        }

        private void pushCode(string code, short getCounts = 0)
        {
            lock (lockCode)
            {
                quCodeGetCounts.Enqueue(new KeyValuePair<string, short>(code, getCounts));
            }
        }

        private void subtractReferenced()
        {
            lock (lockReferenced)
            { nReferencedCount--; }
        }
        private short getReferenced()
        {
            short count = 0;
            lock (lockReferenced)
            { count = nReferencedCount; }

            return count;
        }
        private void pushData1M(string code, List<Bar1M> ltB1M)
        {
            lock (lockData)
            {
                this.ltData1M.Add(new KeyValuePair<string, List<Bar1M>>(code, ltB1M));
            }
        }
        private void pushDataQuotes(RealtimeDataCenter.RealtimeData rd)
        {
            lock (lockData)
            {
                this.ltDataQuotes.Add(rd);
            }
        }
        private void pushDataTodayXDR(string code, TodayXDR xdr)
        {
            lock (lockData)
            {
                this.ltDataTodayXDR.Add(new KeyValuePair<string, TodayXDR>(code, xdr));
            }
        }
    }
}
