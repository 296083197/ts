using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace ts
{
    class WebClientTimeout : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.Timeout = 2000;
            request.ReadWriteTimeout = 3000;
            return request;
        }
    }

    class ThreadGetDataFromHttp2 : ThreadObj
    {
        public const int CYCLE = 3000;

        private string codes;
        private string codes_i;
        private RealtimeDataCenter rdc;
        Dictionary<string, RealtimeDataCenter.FinancialData> dcCodeFD;

        public ThreadGetDataFromHttp2(string[] arrCode, RealtimeDataCenter rdc)
        {
            this.codes = "";
            this.codes_i = "";
            foreach (var item in arrCode)
            {
                this.codes += (getPlusPrefixCode(item) + ",");
                this.codes_i += (getPlusPrefixCode(item) + "_i,");
            }

            this.rdc = rdc;
            this.dcCodeFD = new Dictionary<string, RealtimeDataCenter.FinancialData>();
        }

        ~ThreadGetDataFromHttp2()
        {
        }

        private string getPlusPrefixCode(string code)
        {
            if (code.StartsWith("6"))
            {
                return ("sh" + code);
            }
            else if (code.StartsWith("0")
                || code.StartsWith("3"))
            {
                return ("sz" + code);
            }
            else if (code == "999999")
            { return "sh000001"; }

            return null;
        }

        private string getSubtractPrefixCode(string prefixCode)
        {
            if (prefixCode.Substring(11, 8) == "sh000001")
            { return "999999"; }
            else
            { return prefixCode.Substring(13, 6); }
        }

        public void GetData()
        {
            GetFinancialDataFromHttp();

            string serviceAddr = "http://hq.sinajs.cn/list=" + this.codes;
            List<RealtimeDataCenter.RealtimeData> ltRd = new List<RealtimeDataCenter.RealtimeData>();

            while (true)
            {
                try
                {
                    WebClient client = new WebClientTimeout();
                    Stream strm = client.OpenRead(serviceAddr);
                    StreamReader sr = new StreamReader(strm);

                    string line = null;
                    while (null != (line = sr.ReadLine()))
                    {
                        string[] objs = line.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var item in objs)
                        {
                            string[] obj = item.Split(new char[] { '=', ',' });
                            if (obj.Length == 34)
                            {
                                string codeTdx = getSubtractPrefixCode(obj[0]);
                                
                                RealtimeDataCenter.RealtimeData rd = new RealtimeDataCenter.RealtimeData()
                                {
                                    code = codeTdx,
                                    dt = Convert.ToDateTime(obj[31] + " " + obj[32]),
                                    lastC = Convert.ToDecimal(obj[3]),
                                    H = Convert.ToDecimal(obj[5]),
                                    L = Convert.ToDecimal(obj[6]),
                                    O = Convert.ToDecimal(obj[2]),
                                    C = Convert.ToDecimal(obj[4]),
                                    V = Convert.ToDecimal(obj[9]),
                                    vm = Convert.ToDecimal(obj[10]),
                                    fd = null,
                                };

                                if (rd.C == 0
                                    && Convert.ToDecimal(obj[12]) != 0)
                                { rd.C = rd.H = rd.L = rd.O = Convert.ToDecimal(obj[12]); }

                                if (this.dcCodeFD.ContainsKey(codeTdx))
                                {
                                    rd.fd = this.dcCodeFD[codeTdx];
                                }
                                else
                                {
                                    this.codes_i += (getPlusPrefixCode(codeTdx) + "_i,");
                                }

                                if (rd.code.StartsWith("39900")) //399001, 399006
                                {
                                    rd.V = Convert.ToInt64(rd.V / 100);
                                }

                                ltRd.Add(rd);
                            }
                        }
                    }

                    sr.Close();
                    strm.Close();
                    client.Dispose();

                    if (ltRd.Count > 0)
                    {
                        this.rdc.recv(ltRd);
                        ltRd.Clear();
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() != typeof(System.Threading.ThreadAbortException))
                    //&& e.GetType() != typeof(System.IO.IOException))
                    {
                        //ctrl.ShowInfo(no.ToString() + ":" + e.ToString());
                    }
                }

                if ("" != this.codes_i)
                {
                    GetFinancialDataFromHttp();
                }

                System.Threading.Thread.Sleep(CYCLE);
            }
        }

        private void GetFinancialDataFromHttp()
        {
            string serviceAddr = "http://hq.sinajs.cn/list=" + this.codes_i;
            this.codes_i = "";

            try
            {
                WebClient client = new WebClientTimeout();
                Stream strm = client.OpenRead(serviceAddr);
                StreamReader sr = new StreamReader(strm);

                string line = null;
                while (null != (line = sr.ReadLine()))
                {
                    string[] objs = line.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in objs)
                    {
                        string[] obj = item.Split(new char[] { '=', ',' });
                        if (obj.Length == 20)
                        {
                            /*
                            if (obj[0].Substring(11, 8) == "sh000001"
                                || obj[0].Substring(11, 8) == "sz399001"
                                || obj[0].Substring(11, 8) == "sz399006")
                            {
                                continue;
                            }*/

                            string codeTdx = getSubtractPrefixCode(obj[0]);

                            RealtimeDataCenter.FinancialData fd = new RealtimeDataCenter.FinancialData
                            {
                                EPS_lastYear        = (obj[3] != "" ? Convert.ToDecimal(obj[3]) : 0),
                                EPS_lastQ4          = (obj[4] != "" ? Convert.ToDecimal(obj[4]) : 0),
                                EPS_thisYear        = (obj[5] != "" ? Convert.ToDecimal(obj[5]) : 0),
                                Total_share_capital = (obj[8] != "" ? Convert.ToDecimal(obj[8]) : 0),
                                Flow_of_equity      = (obj[9] != "" ? Convert.ToDecimal(obj[9]) : 0),
                            };

                            if (!this.dcCodeFD.ContainsKey(codeTdx))
                            {
                                this.dcCodeFD.Add(codeTdx, fd);
                            }
                            else
                            {
                                this.dcCodeFD[codeTdx] = fd;
                            }
                        }
                    }
                }

                sr.Close();
                strm.Close();
                client.Dispose();
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(System.Threading.ThreadAbortException))
                //&& e.GetType() != typeof(System.IO.IOException))
                {
                    //ctrl.ShowInfo(no.ToString() + ":" + e.ToString());
                }
            }
        }
    }
}
