using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ts
{
    class RI : RecvRealtimeData
    {
        private const decimal CASH_SIZE = 18000m;
        private Dictionary<string, Tuple<decimal, decimal>> dcWindow = new Dictionary<string, Tuple<decimal, decimal>>(); //code, <UpWindowMinL, DnWindowMaxH>

        /**
         * entry:
         *      entry conditions:
         *          1: down window' size >= 1%;
         *          2: island continued days < 10;
         *          3: up window' L > island' maxH
         *          4: current C > down window' L;
         *      entry point in time:
         *          1: if current range > 9%, then entry(Reactor.EPriceType.REALTIME);
         *          2: else if current time > "14:49:40", then entry(Reactor.EPriceType.CLOSE);
         * 
         * exit:
         *      timeout: ...
         *      stop: ...
         */

        public RI()
        {
            DBDataContext ts = new DBDataContext();
            var sql = from n in ts.tbDnWindows
                      where n.continuedN < 10
                         && n.lastSize >= 0.01m
                      select new { n.code, n.UpWindowMinL, n.DnWindowMaxH };
            foreach (var wind in sql)
            {
                this.dcWindow.Add(wind.code, new Tuple<decimal, decimal>(wind.UpWindowMinL, wind.DnWindowMaxH));

                registry(wind.code);
            }

            //setTimer(100);
            start();
        }

        ~RI()
        {
        }

        protected override void timer()
        {
        }

        protected override void recv(RealtimeDataCenter.RealtimeData rd)
        {
            if (DateTime.Now.Date != rd.dt.Date)
            {
                return;
            } //expired data

            if (this.dcWindow.ContainsKey(rd.code))
            {
                Tuple<decimal, decimal> wind = this.dcWindow[rd.code];
                if (rd.L <= wind.Item2) //当日将不再会形成跳空式的关闭窗口（即RI）
                {
                    removeObj(rd.code);
                }
                else
                {
                    if (rd.C >= wind.Item1)
                    {
                        bool bCreate = false;
                        Reactor.EPriceType pt = Reactor.EPriceType.REALTIME;

                        if ((rd.C / rd.lastC - 1) * 100 > 9.0m) //推测其将limit-up，先买入（此点需继续验证）
                        {
                            bCreate = true;
                            pt = Reactor.EPriceType.REALTIME;
                        }
                        else if (string.Compare(rd.dt.ToString("HHmmss"), "145940") > 0 )
                        {
                            bCreate = true;
                            pt = Reactor.EPriceType.CLOSE;
                        }

                        if (bCreate)
                        {
                            Reactor.CInstruction instruction = new Reactor.CInstruction();
                            instruction.id = 0;
                            instruction.priceAsc = wind.Item2 + 0.01m;
                            instruction.priceDesc = Limit.getInstance().getLimitUp(rd.code, rd.lastC) - 0.01m; //-0.01m, don't buy when limit-up
                            instruction.generateTime = DateTime.Now;
                            instruction.priceType = (short)pt;
                            instruction.category = (short)Reactor.ECategory.BUY;
                            instruction.state = (short)Reactor.EInstructionState.WATING;
                            instruction.desc = "entry";
                            instruction.quantity = (int)(Math.Round(CASH_SIZE / (decimal)instruction.priceAsc / 100m) * 100m);
                            instruction.codeReference = rd.code;

                            if (instruction.quantity > 0)
                            {
                                instruction.sso = new Reactor.CSimplifiedStrategiesObject()
                                {
                                    id = 0,
                                    idForStrategies = (long)Reactor.EIFS.RI,
                                    code = rd.code,
                                    state = (short)Reactor.ESSOState.WAIT_CREATE,
                                    planQuantity = instruction.quantity,
                                    lastStopPrice = (decimal)instruction.priceAsc,
                                    createTime = instruction.generateTime,
                                    direction = (short)Reactor.EDirection.LONG,
                                };
                                instruction.analyseEntryOrExit((Reactor.EDirection)(instruction.sso.direction));

                                removeObj(rd.code);
                                Reactor.getInstance().addInstruction2Wating(rd.code, instruction);
                            }
                        }
                    }
                }
            }
        }

        private void removeObj(string code)
        {
            unregistry(code);
            this.dcWindow.Remove(code);
        }
    }
}
