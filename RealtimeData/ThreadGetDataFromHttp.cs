using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace ts
{
    class ThreadGetDataFromHttp
    {
        public const byte PEAK = 1;
        public const byte VALLEY = 2;
        public const int CYCLE = 5000;
        
        private string codes;
        private Dictionary<string, List<SPVPairBingo>> dcPVPairBingos;
        static private int s_no = 0;
        private int no = 0;

        public ThreadGetDataFromHttp(string codes)
        {
            this.codes = codes;
            no = s_no++;

            dcPVPairBingos = new Dictionary<string, List<SPVPairBingo>>();
        }
        ~ThreadGetDataFromHttp()
        {
            Console.WriteLine("Exit:{0}", this.codes);
        }

        class SPVPairBingo
        {
            public SPVPairBingo()
            {
                sent = false;
            }
      
            public long id { set; get; }
            public string code { set; get; }
            public byte PV { set; get; }
            public DateTime D1 { set; get; }
            public DateTime D2 { set; get; }
            public decimal Slope { set; get; }
            public decimal DCPrice { set; get; }
            public int TCount { set; get; }
            public decimal lastPrice { set; get; } //current day
            public decimal lastH { set; get; }
            public decimal lastL { set; get; }
            public void Match(decimal currentPrice, string currentTime, decimal lastC, decimal H, decimal L)
            {
                if (lastPrice != currentPrice)
                {
                    if ((this.PV == PEAK && currentPrice > this.DCPrice)
                        || (this.PV == VALLEY && currentPrice < this.DCPrice)
                        || sent)
                    {
                        //ctrl.Match2Show(id, code, PV, D1, D2, Slope, DCPrice, TCount, lastPrice, currentPrice, currentTime, lastC, H, L, lastH, lastL);
                        sent = true;
                    }

                    lastPrice = currentPrice;
                }
            }

            private bool sent;
        }

        public void PVPairBingo(long id, string code, byte peakOrValley, DateTime date1, DateTime date2, decimal slope, decimal derivateCurrentPrice, int testCount, decimal lastH, decimal lastL)
        {
            try
            {
                if (!dcPVPairBingos.ContainsKey(code))
                {
                    dcPVPairBingos.Add(code, new List<SPVPairBingo>());
                }
                
                dcPVPairBingos[code].Add(new SPVPairBingo() { id = id, code = code, PV = peakOrValley, D1 = date1, D2 = date2, Slope = slope,
                    DCPrice = derivateCurrentPrice, TCount = testCount, lastPrice = 0, lastH = lastH, lastL = lastL });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally { }
        }

        public void GetDataFromHttp()
        {
            System.Threading.Thread.Sleep(CYCLE/(this.no+1));

            string serviceAddr = "http://hq.sinajs.cn/list=" + this.codes;

            while (true)
            {
                try
                {
                    WebClient client = new WebClient();
                    Stream strm = client.OpenRead(serviceAddr);
                    StreamReader sr = new StreamReader(strm);
                    //this.ctrl.ShowInfo("StreamReader:" + no.ToString());

                    string line = null;
                    while (null != (line = sr.ReadLine()))
                    {
                        string[] objs = line.Split(new char[] { '\r'}, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var item in objs)
                        {
                            string[] obj = item.Split(new char[] { '=', ',' });
                            if (obj.Length == 34)
                            {
                                string code = obj[0].Substring(13, 6);
                                if (dcPVPairBingos.ContainsKey(code))
                                {
                                    //SPVPairBingo pv = dcPVPairBingo[code];
                                    List<SPVPairBingo> lt = dcPVPairBingos[code];
                                    foreach(var it in lt)
                                    {
                                        it.Match(Convert.ToDecimal(obj[4]), obj[31] + " " + obj[32], Convert.ToDecimal(obj[3]),
                                            Convert.ToDecimal(obj[5]), Convert.ToDecimal(obj[6]));
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    if (e.GetType() != typeof(System.Threading.ThreadAbortException))
                        //&& e.GetType() != typeof(System.IO.IOException))
                    {
                        Console.WriteLine(e);
                    }
                }

                System.Threading.Thread.Sleep(CYCLE);
            }
        }
    }
}
