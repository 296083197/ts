using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ts
{
    class MAID
    {
        private const decimal CASH_SIZE = 18000m;
        private Thread th = null;

        public MAID()
        {
            this.th = new Thread(this.worker) { IsBackground = true };
            this.th.Start();
        }

        ~MAID()
        {
            this.th.Abort();
        }

        private void worker()
        {
            bool bExecuted = false;
            while (!bExecuted)
            {
                DateTime dt = DateTime.Now;
                if (string.Compare(dt.ToString("HHmmss"), "144000") > 0
                    && string.Compare(dt.ToString("HHmmss"), "155900") < 0)
                {
                    bExecuted = true;

                    List<KeyValuePair<string, List<coRDC.Bar1M>>> lt1M = new List<KeyValuePair<string, List<coRDC.Bar1M>>>();
                    coRDC crdc = new coRDC();
                    crdc.getAll1MBars(ref lt1M);
                    foreach (var obj in lt1M)
                    {
                        string code = obj.Key;
                        int count = obj.Value.Count;
                        if (count < 200)
                        { continue; }

                        int moreAvgInDayCount = 0;
                        decimal sumV = 0, sum_vm = 0, maxH = 0;
                        decimal[] avg = new decimal[count];
                        int idx = 0;
                        foreach (var b1m in obj.Value)
                        {
                            if (b1m.H > maxH)
                            { maxH = b1m.H; }

                            sumV += b1m.V;
                            sum_vm += b1m.vm;
                            if (sumV > 0)
                            {
                                avg[idx] = sum_vm / sumV;
                                if (b1m.C > avg[idx])
                                { moreAvgInDayCount++; }
                            }

                            idx++;
                        }

                        decimal percent = (decimal)moreAvgInDayCount / count;
                        if (percent >= 0.50m)
                        {
                            decimal lastC = Limit.getInstance().getLastC(code);
                            if (lastC > 0
                                && maxH != Limit.getInstance().getLimitUp(code, lastC) )  //exclude that limit up to drawdown
                            {
                                decimal priceAsc = Math.Max(lastC * 1.085m, maxH * 0.999m);
                                decimal priceDesc = Limit.getInstance().getLimitUp(code, lastC) - 0.01m;
                                if (priceDesc >= priceAsc)
                                {
                                    Reactor.CInstruction instruction = new Reactor.CInstruction();
                                    instruction.id = 0;
                                    instruction.priceAsc = priceAsc;
                                    instruction.priceDesc = priceDesc;
                                    instruction.generateTime = DateTime.Now;
                                    instruction.priceType = (short)Reactor.EPriceType.REALTIME;
                                    instruction.category = (short)Reactor.ECategory.BUY;
                                    instruction.state = (short)Reactor.EInstructionState.WATING;
                                    instruction.desc = "entry";
                                    instruction.quantity = (int)(Math.Round(CASH_SIZE / (decimal)instruction.priceAsc / 100m) * 100m);
                                    instruction.codeReference = code;

                                    if (instruction.quantity > 0)
                                    {
                                        instruction.sso = new Reactor.CSimplifiedStrategiesObject()
                                        {
                                            id = 0,
                                            idForStrategies = (long)Reactor.EIFS.MAID,
                                            code = code,
                                            state = (short)Reactor.ESSOState.WAIT_CREATE,
                                            planQuantity = instruction.quantity,
                                            lastStopPrice = (decimal)instruction.priceAsc,
                                            createTime = instruction.generateTime,
                                            direction = (short)Reactor.EDirection.LONG,
                                        };
                                        instruction.analyseEntryOrExit((Reactor.EDirection)(instruction.sso.direction));

                                        Reactor.getInstance().addInstruction2Wating(code, instruction);
                                    }
                                }
                            }
                        }
                    }
                }

                if (!bExecuted)
                { System.Threading.Thread.Sleep(1000); }
            }
        }
    }
}
