#region

using System;
using System.Collections.Generic;
using core.CommissionStrategies;

#endregion

namespace core.Model
{
    public abstract class Portfolio
    {
        public String ticket { get; set; }
        public CommissionStrategy commissionStrategy { get; set; }
        public Candle[] candles { get; set; }
        public List<Machine> machines { get; set; }
        public String title { get; set; }

        protected Portfolio(String ticket, CommissionStrategy commissionStrategy, Candle[] candles)
        {
            machines = new List<Machine>();

            this.ticket = ticket;
            this.commissionStrategy = commissionStrategy;
            this.candles = candles;
        }

        public void initMachines(String decisionStrategyName, int[] depths)
        {
            foreach (int depth in depths)
                machines.Add(createMachine(decisionStrategyName, 10000000, depth));

            title = machines[0].getDecisionStrategyName();
        }

        protected abstract Machine createMachine(String decisionStrategyName, double currentMoney, int depth);

        public abstract void trade(String year);

        public double countMeanCandleDeviation()
        {
            double mean = 0;
            for (int i = 1; i < candles.Length; i++)
                mean += Math.Abs(candles[i].value - candles[i - 1].value) / candles[i].value;

            return mean / candles.Length;
        }

        public double getCandleValueFor(int start)
        {
            return getCandleFor(start).getValue();
        }

        public Candle getCandleFor(int start)
        {
            return candles[start];
        }

        public bool isDayChanged(int start)
        {
            if (start == 0)
                return false;

            return (candles[start].date.Day != candles[start - 1].date.Day);
        }

        public double computeClosePositionCommission(CommissionRequest request)
        {
            return commissionStrategy.computeClosePositionCommission(request);
        }

        public double computeOpenPositionCommission(CommissionRequest request)
        {
            return commissionStrategy.computeOpenPositionCommission(request);
        }

        // used via reflection
        public String getDepth()
        {
            return "average";
        }
    }
}