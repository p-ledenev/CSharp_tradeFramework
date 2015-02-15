using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core.CommissionStrategies;
using core.Model;

namespace alfaMTT.Model
{
    class TradePortfolio : Portfolio
    {
        public String account { get; set; }

        public TradePortfolio(string ticket, CommissionStrategy commissionStrategy, Candle[] candles) : base(ticket, commissionStrategy, candles)
        {
        }

        protected override Machine createMachine(string decisionStrategyName, double currentMoney, int depth)
        {
            throw new NotImplementedException();
        }

        public override void trade(string year)
        {
            throw new NotImplementedException();
        }
    }
}
