using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeAPI;

namespace ts
{
    class Program
    {
        public static string ramdisk = System.Environment.GetEnvironmentVariable("RAMDisk", EnvironmentVariableTarget.Machine);

        static void Main(string[] args)
        {
            /*
            string szTime = DateTime.Now.ToString("HHmmss");
            while (string.Compare(szTime, "091500") < 0
                || string.Compare(szTime, "150000") > 0)
            {
                Console.WriteLine(szTime);
                System.Threading.Thread.Sleep(1000);
                szTime = DateTime.Now.ToString("HHmmss");
            }
            */

            StringBuilder sErrInfo = new StringBuilder(256);
            int ret = TradeXB.OpenTdx(sErrInfo);
            if (ret < 0)
            {
                Console.WriteLine("TradeXB.OpenTdx error: " + sErrInfo);
                Console.ReadLine();
                return;
            }

            RealtimeDataCenter.getInstance(5, "999999,399001,399006", RealtimeDataCenter.EDataSource.tradeX);
            Limit.getInstance();
            Reactor.getInstance();

            TN tn = new TN();
            MAID maid = new MAID();
            RI ri = new RI();
            RT rt = new RT();

            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
