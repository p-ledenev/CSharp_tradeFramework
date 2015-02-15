using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using core.CommissionStrategies;
using core.Model;

namespace strategiesFrame.Model
{
    class FramePortfolio : Portfolio
    {
        public static String resultFolder = "results";

        public List<Slice> averageMoney { get; set; }

        public FramePortfolio(string ticket, CommissionStrategy commissionStrategy, Candle[] candles)
            : base(ticket, commissionStrategy, candles)
        {
            averageMoney = new List<Slice>();
        }

        protected override Machine createMachine(string decisionStrategyName, double currentMoney, int depth)
        {
            return new FrameMachine(decisionStrategyName, currentMoney, depth, this);
        }

        public void writeAverageMoney(String year)
        {
            List<String> collection = averageMoney.Select(slice => slice.print()).ToList();

            File.WriteAllLines(resultFolder + "\\averageMoney_" + ticket + "_" + year + "_" + title + ".csv", collection);
        }

        public void writeTradeResult(String year)
        {
            List<String> collection = new List<String>();

            String str = "";
            foreach (FrameMachine machine in machines)
                str += machine.depth + ";date;money; ;";

            collection.Add(str);

            int count = getMaxTradesNumber();
            for (int j = 0; j < count; j++)
            {
                str = "";
                foreach (FrameMachine machine in machines)
                {
                    if (j < machine.averageMoney.Count)
                        str += machine.averageMoney.ElementAt(j).print() + "; ;";
                    else
                        str += ";;; ;";
                }

                collection.Add(str);
            }

            File.WriteAllLines(resultFolder + "\\machinesMoney_" + ticket + "_" + year + "_" + title + ".csv",
                collection);
        }

        public void writeTradeSummaryResult(String year)
        {
            List<String> collection = new List<String>
            {
                createSummaryStringFor(" ", "getDepth"),
                createSummaryStringFor("maxLoss", "computeMaxLoss"),
                createSummaryStringFor("maxMoney", "computeMaxMoney"),
                createSummaryStringFor("endPeriodMoney", "computeEndPeriodMoney")
            };

            File.WriteAllLines(resultFolder + "\\machinesSummary_" + ticket + "_" + year + "_" + title + ".csv",
                collection);
        }

        protected String createSummaryStringFor(String title, String methodName)
        {
            String str = title + ";";

            MethodInfo methodInfo;
            foreach (FrameMachine machine in machines)
            {
                methodInfo = machine.GetType().GetMethod(methodName);
                str += methodInfo.Invoke(machine, null) + ";";
            }

            str += ";";

            methodInfo = GetType().GetMethod(methodName);
            str += methodInfo.Invoke(this, null) + ";";

            return str;
        }

        public void writeTradeDetailResult(String year)
        {
            foreach (FrameMachine machine in machines)
                machine.writeTradeResult(year);
        }

        private int getMaxTradesNumber()
        {
            int count = 0;
            foreach (FrameMachine machine in machines)
                if (machine.trades.Count > count)
                    count = machine.trades.Count;

            return count;
        }

        protected void flushResults(String year)
        {
            if (machines.Count > 1)
            {
                writeTradeResult(year);
                writeAverageMoney(year);
            }
            else
            {
                writeTradeDetailResult(year);
            }

            writeTradeSummaryResult(year);
        }

        public void addAverageMoney(DateTime dt, int index)
        {
            double averageValue = machines.Sum(machine => Math.Round(machine.computeCurrentMoney() / machines.Count, 2));

            Slice slice = new Slice(dt, index, averageValue);

            if (averageMoney.Count <= 0 || !slice.hasEqualDate(averageMoney.Last()))
                averageMoney.Add(slice);
        }

        public override void trade(String year)
        {
            for (int i = 0; i < candles.Length - 2; i++)
            {
                Candle candle = candles[i];

                if (isDayChanged(i))
                {
                    addAverageMoney(candle.date, candle.dateIndex);
                    flushResults(year);
                }

                bool onlyCalculate = (candle.date.Year.ToString() != year);

                foreach (FrameMachine machine in machines)
                    machine.trade(i, onlyCalculate);
            }

            flushResults(year);
        }

        // used via reflection
        public double computeMaxLoss()
        {
            double loss = 0;
            for (int i = 0; i < averageMoney.Count; i++)
            {
                for (int j = i; j < averageMoney.Count; j++)
                {
                    double current = (averageMoney[i].value - averageMoney[j].value) / averageMoney[i].value;
                    if (current > loss)
                        loss = current;
                }
            }

            return Math.Round(loss, 3);
        }

        // used via reflection
        public double computeMaxMoney()
        {
            double max = 0;
            foreach (Slice slice in averageMoney)
            {
                if (slice.value > max)
                    max = slice.value;
            }

            return max;
        }

        // used via reflection
        public double computeEndPeriodMoney()
        {
            return averageMoney.Last().value;
        }
    }
}
