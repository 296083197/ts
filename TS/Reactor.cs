using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    class Reactor : RecvRealtimeData
    {
        private object lockInstructions = new object();
        private long idNoneDB = 0;
        private Dictionary<string, Dictionary<long, CInstruction>> dcInstrunctionWaiting    = new Dictionary<string, Dictionary<long, CInstruction>>();
        private Dictionary<string, Dictionary<long, CInstruction>> dcInstrunctionSent       = new Dictionary<string, Dictionary<long, CInstruction>>();
        private DBDataContext db = new DBDataContext();

        private Dictionary<string, RealtimeDataCenter.RealtimeData> dcCodeLastRdForOpen     = new Dictionary<string, RealtimeDataCenter.RealtimeData>();
        private Dictionary<string, RealtimeDataCenter.RealtimeData> dcCodeLastRdForClose    = new Dictionary<string, RealtimeDataCenter.RealtimeData>();

        public bool addInstruction2Wating(string code, CInstruction instruction)
        {
            decimal range = Limit.getInstance().getLimitRange(code);
            if (range != 0.10m)
            {
                string info = "";
                if (range == 0.05m)
                { info = "ST stock"; }
                else if (range == 0m)
                { info = "unkown object"; }

                Console.Write("Error-addInstruction2Wating: " + info + " (" + code + ")" );

                return false;
            }

            lock (lockInstructions)
            {
                registry2RDC(code);

                if (instruction.id == 0)
                { instruction.id = --idNoneDB; }

                if (!this.dcInstrunctionWaiting.ContainsKey(code))
                { this.dcInstrunctionWaiting.Add(code, new Dictionary<long, CInstruction>()); }
                this.dcInstrunctionWaiting[code].Add(instruction.id, instruction);
            }
            return true;
        }
        public void addInstruction2Sent(string code, CInstruction instruction)
        {
            lock (lockInstructions)
            {
                registry2RDC(code);

                if (!this.dcInstrunctionSent.ContainsKey(code))
                { this.dcInstrunctionSent.Add(code, new Dictionary<long, CInstruction>()); }
                this.dcInstrunctionSent[code].Add(instruction.id, instruction);
            }
        }
        private void removeInstructionFromWating(string code, long id)
        {
            lock (lockInstructions)
            {
                if (this.dcInstrunctionWaiting.ContainsKey(code))
                {
                    Dictionary<long, CInstruction> inss = this.dcInstrunctionWaiting[code];
                    inss.Remove(id);
                    if (inss.Count == 0)
                    { this.dcInstrunctionWaiting.Remove(code); }

                    unregistry2RDC(code);
                }
            }
        }
        private void removeInstructionFromSent(string code, long id)
        {
            lock (lockInstructions)
            {
                if (this.dcInstrunctionSent.ContainsKey(code))
                {
                    Dictionary<long, CInstruction> inss = this.dcInstrunctionSent[code];
                    inss.Remove(id);
                    if (inss.Count == 0)
                    { this.dcInstrunctionSent.Remove(code); }

                    unregistry2RDC(code);
                }
            }
        }
        private CInstruction[] getInstructionFromWaitingByCode(string code)
        {
            CInstruction[] inss = null;
            lock (lockInstructions)
            {
                if (this.dcInstrunctionWaiting.ContainsKey(code))
                { inss = this.dcInstrunctionWaiting[code].Values.ToArray(); }
            }

            return inss;
        }
        private CInstruction[] getInstructionFromSentByCode(string code)
        {
            CInstruction[] inss = null;
            lock (lockInstructions)
            {
                if (this.dcInstrunctionSent.ContainsKey(code))
                { inss = this.dcInstrunctionSent[code].Values.ToArray(); }
            }

            return inss;
        }
        private void registry2RDC(string code)
        {
            if (!dcInstrunctionWaiting.ContainsKey(code)
                && !dcInstrunctionSent.ContainsKey(code))
            { registry(code); }
        }
        private void unregistry2RDC(string code)
        {
            if (!dcInstrunctionWaiting.ContainsKey(code)
                && !dcInstrunctionSent.ContainsKey(code))
            { unregistry(code); }
        }

        public enum ESSOState
        {
            WAIT_CREATE = 0,
            CREATING = 1,
            CREATED = 2,
            CLEANING = 3,
            COMPELTED = 4,
            ABORT = 5,
        }
        public enum EInstructionState
        {
            WATING = 0,
            SENT = 1,
            SUCCESSFUL = 2,
            CANCLED = 3,
        }
        public enum EPriceType
        {
            REALTIME = 1,
            OPEN = 2,
            CLOSE = 3,
            REALTIME_OPEN = 4,
            REALTIME_CLOSE = 5,
        }
        public enum ECategory
        {
            BUY = 0,
            SELL = 1,
            RZ_BUY = 2,
            RQ_SELL = 3,
            RQ_RETURN = 4,
            RZ_RETURN = 5,
            RQ_Q2Q = 6,
        }
        public enum EDirection
        {
            LONG = 1,
            SHORT = 2,
        }
        public enum EEntryOrExit
        {
            entry = 1,
            exit = 2,
        }
        public enum EIFS
        {
            TN = 101,
            MAID = 102,
            RI = 103,
            RT = 104,
        }

        public class CSimplifiedStrategiesObject
        {
            public long id { get; set; }
            public long idForStrategies { get; set; }
            public DateTime createTime { get; set; }
            public string code { get; set; }
            public short direction { get; set; }
            public int planQuantity { get; set; }
            public decimal lastStopPrice { get; set; }
            public short state { get; set; }
            public string desc { get; set; }

            public string getInsertSql()
            {
                string sql = Convert.ToString(idForStrategies)
                    + "," + "'" + createTime.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                    + "," + "'" + code + "'"
                    + "," + Convert.ToString(direction)
                    + "," + Convert.ToString(planQuantity)
                    + "," + Convert.ToString(lastStopPrice)
                    + "," + Convert.ToString(state);
                if (desc == null)
                { sql += (",null"); }
                else
                { sql += ("," + "'" + desc + "'"); }

                return sql;
            }
        }
        public class CInstruction
        {
            public CSimplifiedStrategiesObject sso { get; set; }
            public long id { get; set; }
            public decimal? priceAsc { get; set; }
            public decimal? priceDesc { get; set; }
            public short priceType { get; set; }
            public decimal? price { get; set; }
            public short category { get; set; }
            public int quantity { get; set; }
            public short state { get; set; }
            public DateTime generateTime { get; set; }
            public DateTime? sendTime { get; set; }
            public DateTime? resultTime { get; set; }
            public string desc { get; set; }
            public short? entryOrExit { get; set; }
            public string codeReference { get; set; } //code for write log

            public static string szExecOpenTimeBegin    = "092450";
            public static string szExecOpenTimeEnd      = "092500";
            public static string szExecCloseTimeBegin   = "145950";
            public static string szExecCloseTimeEnd     = "150000";
            public void analyseEntryOrExit(EDirection drc)
            {
                if (drc == EDirection.LONG)
                {
                    switch ((ECategory)(category))
                    {
                        case ECategory.BUY:
                        case ECategory.RZ_BUY:
                            entryOrExit = (short)EEntryOrExit.entry;
                            break;

                        case ECategory.SELL:
                        case ECategory.RZ_RETURN:
                            entryOrExit = (short)EEntryOrExit.exit;
                            break;

                        default:
                            break;
                    }
                }
                else if (drc == EDirection.SHORT)
                {
                    switch ((ECategory)(category))
                    {
                        case ECategory.RQ_SELL:
                            entryOrExit = (short)EEntryOrExit.entry;
                            break;

                        case ECategory.RQ_RETURN:
                        case ECategory.RQ_Q2Q:
                            entryOrExit = (short)EEntryOrExit.exit;
                            break;

                        default:
                            break;
                    }
                }
            }
            public string getInsertSql()
            {
                string sql = Convert.ToString(priceAsc)
                    + "," + Convert.ToString(priceDesc)
                    + "," + Convert.ToString(priceType)
                    + "," + Convert.ToString(price)
                    + "," + Convert.ToString(category)
                    + "," + Convert.ToString(quantity)
                    + "," + Convert.ToString(state)
                    + "," + "'" + generateTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                if (sendTime == null)
                { sql += (",null"); }
                else
                { sql += ("," + "'" + ((DateTime)sendTime).ToString("yyyy-MM-dd HH:mm:ss") + "'"); }

                if (resultTime == null)
                { sql += (",null"); }
                else
                { sql += ("," + "'" + ((DateTime)resultTime).ToString("yyyy-MM-dd HH:mm:ss") + "'"); }

                if (desc == null)
                { sql += (",null"); }
                else
                { sql += ("," + "'" + desc + "'"); }

                { sql += (",null"); } //desc2

                return sql;
            }
            public bool bingo(RealtimeDataCenter.RealtimeData rd)
            {
                if (rd.C == 0) //call auction in open
                {
                    switch ((ECategory)(category))
                    {
                        case ECategory.BUY:
                        case ECategory.RZ_BUY:
                        case ECategory.RQ_RETURN:
                            rd.C = rd.sell5c[0];
                            break;

                        case ECategory.SELL:
                        case ECategory.RQ_SELL:
                        case ECategory.RZ_RETURN:
                            rd.C = rd.buy5c[0];
                            break;

                        case ECategory.RQ_Q2Q:
                            return false;
                        default:
                            return false;
                    }
                }

                bool bAsc = true, bDesc = true, bType = true;
                if (null != priceAsc && rd.C < priceAsc)
                { bAsc = false; }
                if (null != priceDesc && rd.C > priceDesc)
                { bDesc = false; }

                switch ((EPriceType)priceType)
                {
                    case EPriceType.REALTIME:
                        bType = string.Compare(rd.dt.ToString("HHmmss"), szExecOpenTimeBegin) >= 0;
                        break;
                    case EPriceType.OPEN:
                        bType = isOpenTime(rd.dt);
                        if (!bType
                            && entryOrExit == (short)(EEntryOrExit.exit)
                            && string.Compare(rd.dt.ToString("HHmmss"), szExecOpenTimeBegin) >= 0) //未能在OPEN时段exit的确保在OPEN之后也能退出
                        { bType = true; }
                        break;
                    case EPriceType.CLOSE:
                        bType = isCloseTime(rd.dt);
                        break;
                    case EPriceType.REALTIME_OPEN:
                        bType = !isCloseTime(rd.dt);
                        break;
                    case EPriceType.REALTIME_CLOSE:
                        bType = !isOpenTime(rd.dt);
                        break;
                    default:
                        bType = false;
                        break;
                }

                if (bAsc && bDesc && bType)
                { return true; }

                return false;
            }
            private bool isCloseTime(DateTime dt)
            {
                if (string.Compare(dt.ToString("HHmmss"), szExecCloseTimeBegin) >= 0)
                { return true; }
                return false;
            }
            private bool isOpenTime(DateTime dt)
            {
                string szTime = dt.ToString("HHmmss");
                return (string.Compare(szTime, szExecOpenTimeBegin) >= 0 && string.Compare(szTime, szExecOpenTimeEnd) < 0);
            }

            public string getLog()
            {
                string log = id
                    + "," + priceAsc
                    + "," + priceDesc
                    + "," + priceType
                    + "," + price
                    + "," + category
                    + "," + quantity
                    + "," + state
                    + "," + generateTime
                    + "," + sendTime
                    + "," + resultTime
                    + "," + desc;

                return log;
            }
            public bool modifyDBObj(ref tsInstruction dbins)
            {
                bool ret = true;
                dbins.priceAsc = priceAsc;
                dbins.priceDesc = priceDesc;
                dbins.priceType = priceType;
                dbins.price = price;
                dbins.category = category;
                dbins.quantity = quantity;
                dbins.state = state;
                dbins.sendTime = sendTime;
                dbins.resultTime = resultTime;
                dbins.desc = desc;

                return ret;
            }
        }

        private static Reactor s_instance = null;
        public static Reactor getInstance()
        {
            if (null == s_instance)
            { s_instance = new Reactor(); }

            return s_instance;
        }

        private Reactor()
        {
            start();
        }

        protected override void timer()
        {
            string szTime = DateTime.Now.ToString("HHmmss");
            if (string.Compare(szTime, CInstruction.szExecOpenTimeBegin) >= 0
                && string.Compare(szTime, CInstruction.szExecOpenTimeEnd) < 0)
            {
                if (this.dcCodeLastRdForOpen.Count > 0)
                {
                    Console.WriteLine("timer-OPEN");
                    foreach (var rd in this.dcCodeLastRdForOpen.Values)
                    {
                        rd.dt = DateTime.Now;
                        recv(rd);
                    }
                    this.dcCodeLastRdForOpen.Clear();

                    setTimer(-1);
                }
            }
            else if (string.Compare(szTime, CInstruction.szExecCloseTimeBegin) >= 0
                && string.Compare(szTime, CInstruction.szExecCloseTimeEnd) < 0)
            {
                if (this.dcCodeLastRdForClose.Count > 0)
                {
                    Console.WriteLine("timer-CLOSE");
                    foreach (var rd in this.dcCodeLastRdForClose.Values)
                    {
                        rd.dt = DateTime.Now;
                        recv(rd);
                    }
                    this.dcCodeLastRdForClose.Clear();

                    setTimer(-1);
                }
            }
        }

        protected override void recv(RealtimeDataCenter.RealtimeData rd)
        {
            if (DateTime.Now.Date != rd.dt.Date)
            { return; } //expired data

            //Console.Write(1);
            CInstruction[] inss = getInstructionFromWaitingByCode(rd.code);
            if (inss != null)
            {
                foreach (var instruction in inss)
                {
                    //:open or close instruction preprocess
                    string szTime = DateTime.Now.ToString("HHmmss");
                    if (string.Compare(szTime, CInstruction.szExecOpenTimeBegin) < 0
                        && instruction.priceType == (short)EPriceType.OPEN)
                    {
                        if (!this.dcCodeLastRdForOpen.ContainsKey(rd.code))
                        { this.dcCodeLastRdForOpen.Add(rd.code, rd); }
                        else
                        { this.dcCodeLastRdForOpen[rd.code] = rd; }

                        setTimer(10);
                        continue;
                    }
                    if (string.Compare(szTime, CInstruction.szExecCloseTimeBegin) < 0
                        && instruction.priceType == (short)EPriceType.CLOSE)
                    {
                        if (!this.dcCodeLastRdForClose.ContainsKey(rd.code))
                        { this.dcCodeLastRdForClose.Add(rd.code, rd); }
                        else
                        { this.dcCodeLastRdForClose[rd.code] = rd; }

                        setTimer(10);
                        continue;
                    }
                    ///:~

                    /*
                    if (rd.C == 0) //call auction in open
                    {
                    }
                    */

                    if (instruction.bingo(rd))
                    {
                        decimal range = 0.01m;
                        EEntryOrExit ee = (EEntryOrExit)instruction.entryOrExit;
                        if (ee == EEntryOrExit.entry)
                        { range = 0.01m; } //谨慎entry
                        else if (ee == EEntryOrExit.exit)
                        { range = 0.03m; } //确保exit

                        //:send
                        ECategory category = (ECategory)instruction.category;
                        switch (category)
                        {
                            case ECategory.BUY:
                            case ECategory.RZ_BUY:
                            case ECategory.RQ_RETURN:
                                if (instruction.price == null)
                                { instruction.price = rd.sell5c[0]; }
                                if (instruction.price == 0) //limit-up, sell none
                                { instruction.price = rd.buy5c[0]; }
                                instruction.price *= (1m + range);

                                decimal limitUp = Limit.getInstance().getLimitUp(rd.code, rd.lastC);
                                if (instruction.price > limitUp)
                                { instruction.price = limitUp; }

                                if (instruction.priceDesc != null && instruction.priceDesc != 0)
                                {
                                    if (instruction.price > instruction.priceDesc)
                                    { instruction.price = instruction.priceDesc; }
                                }
                                break;

                            case ECategory.SELL:
                            case ECategory.RQ_SELL:
                            case ECategory.RZ_RETURN:
                                if (instruction.price == null)
                                { instruction.price = rd.buy5c[0]; }
                                if (instruction.price == 0) //limit-down, buy none
                                { instruction.price = rd.sell5c[0]; }
                                instruction.price *= (1m - range);

                                decimal limitDown = Limit.getInstance().getLimitDown(rd.code, rd.lastC);
                                if (instruction.price < limitDown)
                                { instruction.price = limitDown; }

                                if (instruction.priceAsc != null && instruction.priceAsc != 0)
                                {
                                    if(instruction.price < instruction.priceAsc)
                                    { instruction.price = instruction.priceAsc; }
                                }
                                break;

                            case ECategory.RQ_Q2Q:
                                break;
                            default:
                                break;
                        }

                        instruction.state = (short)EInstructionState.SENT;
                        instruction.sendTime = DateTime.Now;
                        instruction.price = Math.Floor((decimal)instruction.price * 100m) / 100m;

                        //exec
                        //...SendOrder
                        //update DB
                        long maybeOldId = instruction.id;
                        if (instruction.id > 0)
                        {
                            var vs = (from n in db.tsInstructions
                                      where n.id == instruction.id
                                      select n).Single();
                            instruction.modifyDBObj(ref vs);
                            db.SubmitChanges();
                        }
                        else
                        {
                            if (instruction.sso != null)
                            {
                                string sqlSSO = instruction.sso.getInsertSql();
                                string sqlInstruction = instruction.getInsertSql();
                                long? idSSO = instruction.sso.id;
                                long? idInstruction = instruction.id;

                                try
                                {
                                    db.spAddSSOAndInstruction(sqlSSO, sqlInstruction, ref idSSO, ref idInstruction);
                                    instruction.id = (long)idInstruction;
                                    instruction.sso.id = (long)idSSO;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    Console.WriteLine(sqlSSO);
                                    Console.WriteLine(sqlInstruction);

                                    WriteLog.getInstance().pushData("error2DB: " + sqlSSO + "; " + sqlInstruction);
                                }
                            }
                        }

                        Console.Write(2);
                        string ifs = "";
                        if (instruction.sso != null)
                        { ifs = instruction.sso.idForStrategies.ToString(); }
                        Console.WriteLine("(" + rd.code + "," + ifs + "," + instruction.desc + "," + instruction.sendTime + "," + instruction.price + ")");

                        //write log
                        WriteLog.getInstance().pushData(instruction.getLog());

                        addInstruction2Sent(rd.code, instruction);
                        removeInstructionFromWating(rd.code, maybeOldId);
                        ///:~
                    }
                }
            }

            inss = getInstructionFromSentByCode(rd.code);
            if (inss != null)
            {
                foreach (var instruction in inss)
                {
                    //...QueryDatas(Category=2)
                    bool bSuccesful = false;
                    ECategory category = (ECategory)instruction.category;
                    if (category == ECategory.BUY)
                    {
                        if (instruction.price > rd.C
                            || (instruction.price == rd.C && DateTime.Now.Subtract((DateTime)instruction.sendTime).TotalSeconds > 5))
                        {
                            bSuccesful = true;
                        }
                    }
                    else if (category == ECategory.SELL)
                    {
                        if (instruction.price < rd.C
                            || (instruction.price == rd.C && DateTime.Now.Subtract((DateTime)instruction.sendTime).TotalSeconds > 5))
                        {
                            bSuccesful = true;
                        }
                    }

                    if (bSuccesful)
                    {
                        instruction.price = rd.C;  //...QueryDatas(Category=2)' price
                        instruction.state = (short)EInstructionState.SUCCESSFUL;
                        instruction.resultTime = DateTime.Now;

                        //exec
                        //update DB
                        if (instruction.id > 0)
                        {
                            var vs = (from n in db.tsInstructions
                                      where n.id == instruction.id
                                      select n).Single();
                            instruction.modifyDBObj(ref vs);
                            db.SubmitChanges();
                        }

                        Console.Write(3);
                        Console.WriteLine("(" + rd.code + "," + instruction.resultTime + "," + instruction.price + ")");

                        //write log
                        WriteLog.getInstance().pushData(instruction.getLog());

                        removeInstructionFromSent(rd.code, instruction.id);
                    }
                }
            }
        }
    }
}
