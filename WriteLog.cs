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
    public class WriteLog
    {
        static WriteLog s_instance = null;
        private Queue<KeyValuePair<DateTime, string>> quData = new Queue<KeyValuePair<DateTime, string>>();
        private static object lockData = new object();
        Thread th = null;
        FileStream fs = null;

        static public WriteLog getInstance()
        {
            if (null == s_instance)
            { s_instance = new WriteLog(); }

            return s_instance;
        }

        ~WriteLog()
        {
            if (null != s_instance)
            {
                this.th.Abort();
                fs.Close();
            }
        }

        private WriteLog()
        {
            this.th = new Thread(this.write) { IsBackground = true };
            this.th.Start();
        }

        private void openFile()
        {
            this.fs = new FileStream(Program.ramdisk + "\\ts.log", FileMode.Append);
            Console.WriteLine("openFile:" + fs + "(" + DateTime.Now + ")");
        }

        private void write()
        {
            Queue<KeyValuePair<DateTime, string>> quTmpData = new Queue<KeyValuePair<DateTime, string>>();
            KeyValuePair<DateTime, string>? row = null;
            openFile();

            while (true)
            {
                if (this.quData.Count <= 0)
                {
                    System.Threading.Thread.Sleep(100);

                    continue;
                }

                lock (lockData)
                {
                    row = this.quData.Dequeue();
                }

                //write log
                string szRow = row.Value.Key.ToString("yyyy-MM-dd HH:mm:ss.fff")
                            + "," + row.Value.Value
                            + "\r\n";
                bool bNeedRepeat = false;
                do {
                    try
                    {
                        StreamWriter sw = new StreamWriter(fs);
                        sw.Write(szRow);
                        sw.Flush();
                        //sw.Close();
                        bNeedRepeat = false;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        System.Threading.Thread.Sleep(100);

                        openFile();

                        bNeedRepeat = true;
                    }
                } while (bNeedRepeat);

                row = null;
            }
        }

        public void pushData(string log)
        {
            lock (lockData)
            {
                this.quData.Enqueue(new KeyValuePair<DateTime, string>(DateTime.Now, log));
            }
        }
    }
}
