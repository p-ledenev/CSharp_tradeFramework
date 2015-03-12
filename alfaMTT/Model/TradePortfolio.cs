using System;
using System.Collections.Generic;
using alfaMTT.Alfa;
using core.CommissionStrategies;
using core.Model;
using core.SiftCanldesStrategies;

namespace alfaMTT.Model
{
    class TradePortfolio : Portfolio
    {
        public String id { get; set; }
        public String account { get; set; }
        public int lot { get; set; }

        public TradePortfolio(string ticket, CommissionStrategy commissionStrategy, SiftCandlesStrategy siftStrategy)
            : base(ticket, commissionStrategy, siftStrategy)
        {
        }

        protected override Machine createMachine(string decisionStrategyName, double currentMoney, int depth)
        {
            return new TradeMachine(decisionStrategyName, currentMoney, depth, this);
        }

        public List<TerminalOrder> collectTerminalOrders()
        {
            List<TerminalOrder> orders = new List<TerminalOrder>();

            foreach (TradeMachine machine in machines)
            {
                if (machine.isBlocked)
                    continue;

                TerminalOrder order;
                try
                {
                    order = machine.processCandleWithIndex(candles.Count - 1);
                }
                catch(TradeSignalIgnored)
                {
                    continue;
                }

                if (order.parent != null)
                    orders.Add(order.parent);

                orders.Add(order);
            }

            return orders;
        }
       
        public override String printPortfolio()
        {
            return "id " + id + ", account " + account + ", lot " + lot + ", " + base.printPortfolio();
        }
    }
}
