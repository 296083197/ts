using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts
{
    class RT : RecvRealtimeData
    {
        private const decimal CASH_SIZE = 18000m;
        private Dictionary<string, SortedSet<decimal>> dcTendency = new Dictionary<string, SortedSet<decimal>>(); //code, derivateCurrentPrice

        /** conditions:
         */

        public RT()
        {
            DBDataContext ts = new DBDataContext();
            var sql = from n in ts.tbPairBingoHs
                      where n.slope > -0.05m && n.slope < -0.01m
                      select new { n.code, n.derivateCurrentPrice };
            foreach (var tendency in sql)
            {
                if (!this.dcTendency.ContainsKey(tendency.code))
                { this.dcTendency.Add(tendency.code, new SortedSet<decimal>()); }
                this.dcTendency[tendency.code].Add((decimal)(tendency.derivateCurrentPrice));

                registry(tendency.code);
            }

            //setTimer(100);
            start();
        }

        ~RT()
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

            if (this.dcTendency.ContainsKey(rd.code))
            {
                decimal[] dcps = this.dcTendency[rd.code].ToArray();
                foreach (var dcp in dcps)
                {
                    if (rd.C >= dcp)
                    {
                        bool bCreate = false;
                        Reactor.EPriceType pt = Reactor.EPriceType.REALTIME;

                        if ((rd.C / rd.lastC - 1) * 100 < 3.0m)
                        {
                            bCreate = true;
                            pt = Reactor.EPriceType.REALTIME;
                        }

                        if (bCreate)
                        {
                            Reactor.CInstruction instruction = new Reactor.CInstruction();
                            instruction.id = 0;
                            instruction.priceAsc = dcp + 0.01m;
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
                                    idForStrategies = (long)Reactor.EIFS.RT,
                                    code = rd.code,
                                    state = (short)Reactor.ESSOState.WAIT_CREATE,
                                    planQuantity = instruction.quantity,
                                    lastStopPrice = (decimal)instruction.priceAsc,
                                    createTime = instruction.generateTime,
                                    direction = (short)Reactor.EDirection.LONG,
                                };
                                instruction.analyseEntryOrExit((Reactor.EDirection)(instruction.sso.direction));

                                //直接removeObj，以及下面的break (foreach)，而不使用下面的注释部分，原因在于：Reactor.EIFS.RT中，同一对象有多个tendency line时，当日只进入一次
                                removeObj(rd.code);
                                /*
                                this.dcTendency[rd.code].Remove(dcp);
                                if (this.dcTendency[rd.code].Count == 0)
                                { removeObj(rd.code); }
                                */
                                Reactor.getInstance().addInstruction2Wating(rd.code, instruction);

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void removeObj(string code)
        {
            unregistry(code);
            this.dcTendency.Remove(code);
        }
    }
}
