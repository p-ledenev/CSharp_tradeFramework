#region

using System;
using core.Model;

#endregion

namespace core.TakeProfitStrategies
{
    internal class TakeProfitFromMaxStrategy : TakeProfitStrategy
    {
        public double k { get; set; }

        public TakeProfitFromMaxStrategy(Machine machine, double k)
            : base(machine)
        {
            this.k = k;
        }

        public override bool shouldTakeProfit(int start)
        {
            double value = machine.getCandleValueFor(start);
            double extremumValue = machine.getLastTrade().position.tradeValue;

            int sign = (machine.getLastOpenPositionTrade().isBuy()) ? 1 : -1;

            for (int i = machine.getLastTrade().dateIndex; i <= start; i++)
                if (sign*machine.getCandleValueFor(i) > sign*extremumValue)
                    extremumValue = machine.getCandleValueFor(i);

            return sign*(extremumValue - value)/extremumValue > k;
        }

        public override bool shouldReopenPosition(int start)
        {
            /*
            double value = pft.getRearValue(start);
            double lastValue = pft.getLastTrade().stock.buyValue;

            int sgn = (pft.getLastOpenPositionTrade().stock.mode.Equals(Stock.buy)) ? 1 : -1;

            return sgn * (value - lastValue) / lastValue > maxProfitPercent;
            */

            return false;
        }

        public override void readParamsFrom(String xml)
        {
        }
    }
}