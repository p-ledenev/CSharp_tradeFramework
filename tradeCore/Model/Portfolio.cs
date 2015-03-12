#region

using System;
using System.Collections.Generic;
using System.Linq;
using core.CommissionStrategies;
using core.SiftCanldesStrategies;

#endregion

namespace core.Model
{
    public abstract class Portfolio
    {
        public String ticket { get; set; }
        public List<Machine> machines { get; set; }
        public String title { get; set; }
        public double maxMoney { get; set; }

        protected CommissionStrategy commissionStrategy;
        protected SiftCandlesStrategy siftStrategy;
        protected List<Candle> candles;

        protected Portfolio(String ticket, CommissionStrategy commissionStrategy, SiftCandlesStrategy siftStrategy)
        {
            machines = new List<Machine>();
            candles = new List<Candle>();

            this.ticket = ticket;
            this.commissionStrategy = commissionStrategy;
            this.siftStrategy = siftStrategy;
        }

        public void initMachines(String decisionStrategyName, int[] depths)
        {
            foreach (int depth in depths)
                machines.Add(createMachine(decisionStrategyName, 10000000, depth));

            title = machines[0].getDecisionStrategyName();
        }

        protected abstract Machine createMachine(String decisionStrategyName, double currentMoney, int depth);

        public double countMeanCandleDeviation()
        {
            double mean = 0;
            for (int i = 1; i < candles.Count; i++)
                mean += Math.Abs(candles[i].value - candles[i - 1].value) / candles[i].value;

            return mean / candles.Count;
        }

        public double getCandleValueFor(int start)
        {
            return getCandleFor(start).getValue();
        }

        public Candle getCandleFor(int start)
        {
            return candles[start];
        }

        public List<Candle> getCandles()
        {
            return candles;
        }

        public int getVolume()
        {
            return machines.Sum(machine => machine.getPositionVolume());
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

        public void calculateMaxMoney()
        {
            double currentMoney = calculateCurrentMoney();
            if (currentMoney > maxMoney)
                maxMoney = currentMoney;
        }

        public double calculateCurrentMoney()
        {
            return machines.Sum(machine => machine.computeCurrentMoney());
        }

        public DateTime getLastCandleDate()
        {
            return candles.Last().getDate();
        }

        public void removeUnusedCandles(int delta)
        {
            candles.RemoveRange(0, delta);
        }

        public int addCandlesRange(List<Candle> candles)
        {
            List<Candle> sifted = siftStrategy.sift(candles);

            candles.AddRange(sifted);

            return sifted.Count;
        }

        public int countSiftedRange(List<Candle> candles)
        {
            return siftStrategy.sift(candles).Count;
        }

        public int getMaxDepth()
        {
            int maxDepth = 0;
            foreach (Machine machine in machines)
            {
                if (machine.depth > maxDepth)
                    maxDepth = machine.depth;
            }

            return maxDepth;
        }

        public Trade getEarliestTrade()
        {
            Trade earliestTrade = Trade.createSample();

            foreach (Machine machine in machines)
            {
                if (machine.getLastTrade() != null && machine.getLastTrade().isEarlierThan(earliestTrade))
                    earliestTrade = machine.getLastTrade();
            }

            return earliestTrade;
        }

        public virtual String printPortfolio()
        {
            return "strategy " + getDescisionStrategyName() + ", title" + title + ", ticket " + ticket + ", moneyValue " + calculateCurrentMoney();
        }

        public double calculateCommission()
        {
            return commissionStrategy.computeOpenPositionCommission(CommissionRequest.forOneLot());
        }

        public double getSieveParam()
        {
            return siftStrategy.sieveParam;
        }

        public String getDescisionStrategyName()
        {
            return machines[0].getDecisionStrategyName();
        }

        public double calculateInitialMoney()
        {
            return machines.Sum(machine => machine.initialMoney);
        }

        public void addMachine(Machine machine)
        {
            machines.Add(machine);
        }
    }
}