using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using TradeAPI;

namespace ts
{
    class ThreadGetDataFromTradeX : ThreadObj
    {
        public static int MAX_COUNT_PER_TIMES = 50;
        private RealtimeDataCenter rdc;
        private static object lockCode = new object();
        private bool bChanged = false;
        private SortedSet<string> stCode = new SortedSet<string>();
        private List<Tuple<string[], byte[], short[]>> ltCodeMarket = new List<Tuple<string[], byte[], short[]>>();
        private enum EMarketType
        {
            shenzhen = 0,
            shanghai = 1,
        }

        public void addObj(string code)
        {
            lock (lockCode)
            {
                if (this.stCode.Add(code))
                { bChanged = true; }
            }
        }

        public void removeObj(string code)
        {
            lock (lockCode)
            {
                if (this.stCode.Remove(code))
                { bChanged = true; }
            }
        }

        public ThreadGetDataFromTradeX(string[] arrCode, RealtimeDataCenter rdc)
        {
            foreach (var item in arrCode)
            {
                addObj(item);
            }

            this.rdc = rdc;
        }

        ~ThreadGetDataFromTradeX()
        {
        }

        static public int connectTDX()
        {
            int idConn = -1;
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);
            string[] tdxSrvAddr = { "14.17.75.71,7709"
                ,"61.49.50.190,7709"
                ,"119.147.214.94,7709"
                ,"121.14.110.201,80"
                ,"119.147.164.60,7709"
                ,"119.147.171.207,7709"
                ,"222.73.49.4,7709"
                ,"221.231.141.67,7709"
                ,"61.135.149.186,443"
                ,"123.129.203.36,7709"
                ,"59.173.18.69,7709"
                ,"221.236.13.219,7709"
                ,"116.57.224.5,7709"
            };

            foreach (var addr in tdxSrvAddr)
            {
                string[] arry = addr.Split(new char[] { ',' });
                if (arry.Count() == 2)
                {
                    idConn = TradeXB.TdxHq_Connect(arry[0], Convert.ToInt16(arry[1]), sResult, sErrInfo);
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine(sResult + " " + sErrInfo);

                    if (idConn >= 0)
                    {
                        break;
                    }
                }
            }

            return idConn;
        }

        static private bool isTradingTime(DateTime dt)
        {
            const string szStart1 = "091430", szEnd1 = "113030";
            const string szStart2 = "125930", szEnd2 = "150030";
            string szTime = dt.ToString("HHmmss");

            bool b1 = string.Compare(szTime, szStart1) >= 0 && string.Compare(dt.ToString("HHmmss"), szEnd1) <= 0;
            bool b2 = string.Compare(szTime, szStart2) >= 0 && string.Compare(dt.ToString("HHmmss"), szEnd2) <= 0;

            return b1 || b2;
        }

        public void GetData()
        {
            List<RealtimeDataCenter.RealtimeData> ltRd = new List<RealtimeDataCenter.RealtimeData>();
            StringBuilder sErrInfo = new StringBuilder(256);
            StringBuilder sResult = new StringBuilder(1024 * 1024);

            while (true)
            {
                if(!isTradingTime(DateTime.Now))
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                int idConn = ThreadGetDataFromTradeX.connectTDX();
                if (idConn < 0)
                {
                    Console.WriteLine("没有可用的TDX行情服务器！  请检查网络...... 重连......" + DateTime.Now);

                    System.Threading.Thread.Sleep(1000);

                    continue;
                }

                bool bRet = true;
                while (bRet)
                {
                    if (!isTradingTime(DateTime.Now))
                    {
                        bRet = false;
                        TradeXB.TdxHq_Disconnect(idConn);
                        Console.WriteLine("is not trading time");

                        break;
                    }

                    if (this.bChanged)
                    { generateCodeMarketGrp(); }

                    foreach (var item in ltCodeMarket)
                    {
                        sResult.Clear();
                        sErrInfo.Clear();
                        short count = item.Item3[0];

                        bRet = TradeXB.TdxHq_GetSecurityQuotes(idConn, item.Item2, item.Item1, ref count, sResult, sErrInfo);
                        if (bRet)
                        {
                            string[] rows = sResult.ToString().Split(new char[] { '\n' });
                            foreach (var row in rows)
                            {
                                RealtimeDataCenter.RealtimeData rd = analyseRD(row);
                                if (null != rd)
                                { ltRd.Add(rd); }
                            }

                            if (ltRd.Count > 0)
                            {
                                this.rdc.recv(ltRd);
                                ltRd.Clear();
                            }
                        }
                        else
                        {
                            string szErrorInfo = sErrInfo.ToString();
                            Console.WriteLine(szErrorInfo);
                            if (szErrorInfo.Contains("连接断开"))
                            {
                                TradeXB.TdxHq_Disconnect(idConn);

                                break;
                            }
                        }
                    }

                    if (bRet)
                    { System.Threading.Thread.Sleep(800); }
                }

                TradeXB.TdxHq_Disconnect(idConn);
                System.Threading.Thread.Sleep(1000);
            }
        }

        public static RealtimeDataCenter.RealtimeData analyseRD(string row)
        {
            string[] cols = row.Split(new char[] { '\t' });
            if (cols.Count() > 10
                && (cols[0] == "0" || cols[0] == "1"))
            {
                RealtimeDataCenter.RealtimeData rd = new RealtimeDataCenter.RealtimeData()
                {
                    /*
                        0     市场
                        1     代码
                        2     活跃度
                        3     现价
                        4     昨收
                        5     开盘
                        6     最高
                        7     最低
                        8     保留 (Time, format as 'HHMMSSAA', 'AA' is 10 ms)
                        9     保留
                        10    总量
                        11    现量
                        12    总金额
                        13    内盘
                        14    外盘
                        15    保留
                        16    保留
                        17    买一价
                        18    卖一价
                        19    买一量
                        20    卖一量
                        21    买二价
                        22    卖二价
                        23    买二量
                        24    卖二量
                        25    买三价
                        26    卖三价
                        27    买三量
                        28    卖三量
                        29    买四价
                        30    卖四价
                        31    买四量
                        32    卖四量
                        33    买五价
                        34    卖五价
                        35    买五量
                        36    卖五量
                        37    保留
                        38    保留
                        39    保留
                        40    保留
                        41    保留
                        42    涨速
                        43    活跃度
                     */
                    code = cols[1],
                    C = Math.Round(Convert.ToDecimal(cols[3]), 2, MidpointRounding.AwayFromZero),
                    lastC = Math.Round(Convert.ToDecimal(cols[4]), 2, MidpointRounding.AwayFromZero),
                    O = Convert.ToDecimal(cols[5]),
                    H = Convert.ToDecimal(cols[6]),
                    L = Convert.ToDecimal(cols[7]),
                    V = Convert.ToDecimal(cols[10]),
                    vm = Convert.ToDecimal(cols[12]),
                    // fd = null,
                };
                rd.buy5c[0] = Convert.ToDecimal(cols[17]);
                rd.buy5c[1] = Convert.ToDecimal(cols[21]);
                rd.buy5c[2] = Convert.ToDecimal(cols[25]);
                rd.buy5c[3] = Convert.ToDecimal(cols[29]);
                rd.buy5c[4] = Convert.ToDecimal(cols[33]);
                rd.sell5c[0] = Convert.ToDecimal(cols[18]);
                rd.sell5c[1] = Convert.ToDecimal(cols[22]);
                rd.sell5c[2] = Convert.ToDecimal(cols[26]);
                rd.sell5c[3] = Convert.ToDecimal(cols[30]);
                rd.sell5c[4] = Convert.ToDecimal(cols[34]);
                rd.buy5v[0] = Convert.ToDecimal(cols[19]);
                rd.buy5v[1] = Convert.ToDecimal(cols[23]);
                rd.buy5v[2] = Convert.ToDecimal(cols[27]);
                rd.buy5v[3] = Convert.ToDecimal(cols[31]);
                rd.buy5v[4] = Convert.ToDecimal(cols[35]);
                rd.sell5v[0] = Convert.ToDecimal(cols[20]);
                rd.sell5v[1] = Convert.ToDecimal(cols[24]);
                rd.sell5v[2] = Convert.ToDecimal(cols[28]);
                rd.sell5v[3] = Convert.ToDecimal(cols[32]);
                rd.sell5v[4] = Convert.ToDecimal(cols[36]);

                rd.dt = DateTime.Now;

                return rd;
            }

            return null;
        }

        private void generateCodeMarketGrp()
        {
            string[] codes = null;
            lock (lockCode)
            {
                codes = new string[this.stCode.Count];
                this.stCode.CopyTo(codes);

                bChanged = false;
            }

            this.ltCodeMarket.Clear();
            foreach (var code in codes)
            {
                if (this.ltCodeMarket.Count == 0 || this.ltCodeMarket.Last().Item3[0] >= MAX_COUNT_PER_TIMES)
                { this.ltCodeMarket.Add(new Tuple<string[], byte[], short[]>(new string[MAX_COUNT_PER_TIMES], new byte[MAX_COUNT_PER_TIMES], new short[] { 0 })); }

                short count = this.ltCodeMarket.Last().Item3[0];
                this.ltCodeMarket.Last().Item1[count] = code;
                this.ltCodeMarket.Last().Item2[count] = getMarketType(code);
                this.ltCodeMarket.Last().Item3[0] = (short)(count + 1);
            }
        }

        public static byte getMarketType(string code)
        {
            byte type = 0;
            if (code.Substring(0, 1) == "6")
            { type = (byte)EMarketType.shanghai; }
            else
            { type = (byte)EMarketType.shenzhen; }

            return type;
        }

        private void GetFinancialDataFromHttp()
        {
        }
    }
}
