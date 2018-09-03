using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    class Limit
    {
        static Limit s_instance = null;
        private Dictionary<string, decimal> mpCodeLastC = new Dictionary<string, decimal>();
        private Dictionary<string, string> dcCodeNameShenzhen = new Dictionary<string, string>();
        private Dictionary<string, string> dcCodeNameShanghai = new Dictionary<string, string>();
        private Dictionary<string, Tuple<decimal, decimal>> dcModifiedLastC = new Dictionary<string, Tuple<decimal, decimal>>(); //code, <lastC in DB, lastC from realtime>
        private Dictionary<string, coRDC.TodayXDR> dcXDR = new Dictionary<string, coRDC.TodayXDR>(); //code, xdr

        static public Limit getInstance()
        {
            if (null == s_instance)
            { s_instance = new Limit(); }

            return s_instance;
        }

        private Limit()
        {
            //:lastC
            DBDataContext db = new DBDataContext();
            var t0 = from a in db.tbDateLineExts
                     group a by a.code into g
                     select new { code = g.Key, date = g.Max(a => a.date) };

            var t1 = from a in db.tbDateLineExts
                     join b in t0
                     on new { a.code, a.date } equals new { b.code, b.date }
                     select new { a.code, a.date, a.C };

            foreach (var item in t1)
            {
                this.mpCodeLastC.Add(item.code, item.C);
            }

            coRDC crdc = new coRDC();

            //update
            Console.WriteLine("Limit-getAllQuotes");
            List<RealtimeDataCenter.RealtimeData> ltRd = new List<RealtimeDataCenter.RealtimeData>();
            if (!crdc.getAllQuotes(ref ltRd))
            { throw new Exception("getAllQuotes"); }
            foreach (var rd in ltRd)
            {
                if (this.mpCodeLastC.ContainsKey(rd.code))
                {
                    if (this.mpCodeLastC[rd.code] != rd.lastC && rd.lastC != 0)
                    {
                        Console.WriteLine("ModifiedLastC:" + rd.code + "," + this.mpCodeLastC[rd.code].ToString() + "," + rd.lastC.ToString());

                        dcModifiedLastC.Add(rd.code, new Tuple<decimal, decimal>(this.mpCodeLastC[rd.code], rd.lastC));
                        this.mpCodeLastC[rd.code] = rd.lastC;
                    }
                }
                else
                { this.mpCodeLastC.Add(rd.code, rd.lastC); }
            }
            Console.WriteLine("Limit-getAllQuotes OK");
            ///:~

            //:TodayXDR
            Console.WriteLine("Limit-getAllTodayXDR");
            List<KeyValuePair<string, coRDC.TodayXDR>> ltData = new List<KeyValuePair<string, coRDC.TodayXDR>>();
            crdc.getAllTodayXDR(ref ltData);
            foreach (var item in ltData)
            {
                if (!this.dcXDR.ContainsKey(item.Key))
                { this.dcXDR.Add(item.Key, item.Value); }
            }
            Console.WriteLine("Limit-getAllTodayXDR OK");
            ///:~

            //:name
            Console.WriteLine("Limit-getAllSecuritys");
            if (!crdc.getAllSecuritys(ref dcCodeNameShenzhen, ref dcCodeNameShanghai) )
            { throw new Exception("getAllSecuritys"); }
            Console.WriteLine("Limit-getAllSecuritys OK");
            ///:~
        }

        public bool isModifiedLastC(string code)
        {
            return this.dcModifiedLastC.ContainsKey(code);
        }

        public bool isTodayXDR(string code)
        { return this.dcXDR.ContainsKey(code); }

        public decimal getLastC(string code)
        {
            decimal p = -1;
            if (this.mpCodeLastC.ContainsKey(code))
            { p = this.mpCodeLastC[code]; }

            return p;
        }

        public decimal getLimitUp(string code, decimal? lastC = null)
        {
            decimal p = -1;
            if (lastC == null)
            {
                if (this.mpCodeLastC.ContainsKey(code))
                { lastC = this.mpCodeLastC[code]; }
            }

            if (lastC != null)
            { p = Math.Round((decimal)lastC * (1m + getLimitRange(code)), 2, MidpointRounding.AwayFromZero); }

            return p;
        }

        public decimal getLimitDown(string code, decimal? lastC = null)
        {
            decimal p = -1;
            if (lastC == null)
            {
                if (this.mpCodeLastC.ContainsKey(code))
                { lastC = this.mpCodeLastC[code]; }
            }

            if (lastC != null)
            { p = Math.Round((decimal)lastC * (1m - getLimitRange(code)), 2, MidpointRounding.AwayFromZero); }

            return p;
        }

        public decimal getLimitRange(string code)
        {
            string name = "";
            decimal range = 0;
            if (code.StartsWith("6"))
            {
                if (this.dcCodeNameShanghai.ContainsKey(code))
                { name = this.dcCodeNameShanghai[code]; }
            }
            else
            {
                if (this.dcCodeNameShenzhen.ContainsKey(code))
                { name = this.dcCodeNameShenzhen[code]; }
            }

            if (name != "")
            {
                if (name.IndexOf("ST") > 0)
                { range = 0.05m; }
                else
                { range = 0.10m; }
            }

            return range;
        }
    }
}
