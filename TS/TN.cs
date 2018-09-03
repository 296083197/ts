using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeAPI;

namespace ts
{
    class TN
    {
        public TN()
        {
            DBDataContext db = new DBDataContext();
            var t = from a in db.tsInstructions
                    join b in db.tsSimplifiedStrategiesObjects
                    on a.idForObj equals b.id
                    where a.state == (short)Reactor.EInstructionState.WATING
                        || a.state == (short)Reactor.EInstructionState.SENT
                    select new { b.code, a.id, a.priceAsc, a.priceDesc, a.priceType, a.price, a.category, a.quantity, a.state, a.generateTime, a.sendTime, a.resultTime, a.desc, b.direction };
            foreach (var item in t)
            {
                Reactor.CInstruction instruction = new Reactor.CInstruction()
                {
                    sso = null,

                    id = item.id,
                    priceAsc = item.priceAsc,
                    priceDesc = item.priceDesc,
                    priceType = item.priceType,
                    price = item.price,
                    category = item.category,
                    quantity = item.quantity,
                    state = item.state,
                    generateTime = item.generateTime,
                    sendTime = item.sendTime,
                    resultTime = item.resultTime,
                    desc = item.desc,
                    codeReference = item.code,
                };
                instruction.analyseEntryOrExit((Reactor.EDirection)(item.direction));

                if (Reactor.EInstructionState.WATING == (Reactor.EInstructionState)instruction.state)
                {
                    if (!Limit.getInstance().isTodayXDR(item.code))
                    { Reactor.getInstance().addInstruction2Wating(item.code, instruction); }
                }
                else if (Reactor.EInstructionState.SENT == (Reactor.EInstructionState)instruction.state)
                { Reactor.getInstance().addInstruction2Sent(item.code, instruction); }
            }
        }
    }
}
