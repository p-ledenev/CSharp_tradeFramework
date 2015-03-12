using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core.Model;

namespace strategiesFrame.Model
{
    class FrameMachine : Machine
    {
        public List<Trade> trades { get; set; }
        public List<Slice> averageMoney { get; set; }

        public FrameMachine(string decisionStrategyName, double currentMoney, int depth, Portfolio portfolio)
            : base(decisionStrategyName, currentMoney, depth, portfolio)
        {
            trades = new List<Trade> { Trade.createEmpty() };
            averageMoney = new List<Slice>();
        }

        public override Trade getLastTrade()
        {
            return trades.Last();
        }

        public override Trade getLastOpenPositionTrade()
        {
            for (int i = trades.Count - 1; i >= 0; i--)
                if (trades[i].isCloseAndOpenPosition())
                    return trades[i];

            return Trade.createEmpty();
        }

        public void processCandleWithIndex(int start, bool onlyCalculate = false)
        {
            TradeSignal signal = createSignalFor(start);
            
            Candle candle = portfolio.getCandleFor(start);
            int closeVolume = currentPosition.volume;

            closePosition(candle);

            if (signal.isCloseAndOpenPosition())
                openPosition(candle, signal.direction);

            trades.Add(new Trade(candle.date, candle.dateIndex, candle.tradeValue, signal.direction,
                currentPosition.volume + closeVolume, signal.mode));

            averageMoney.Add(new Slice(candle.date, candle.dateIndex, computeCurrentMoney()));

            getPortfolio().addAverageMoney(candle.date, candle.dateIndex);
        }

        public void writeTradeResult(String year)
        {
            List<String> collection = new List<String>
            {
                "dateIndex;date;candleValue;" + portfolio.getCandleFor(0).printDescriptionHead() +
                " ;dateIndex;date;openBuy;openSell;closeBuy;closeSell;" +
                " ;dateIndex;date;averageMoney;"
            };

            Trade[] arrTrades = trades.ToArray();
            Slice[] arrMoney = averageMoney.ToArray();

            int index = 0;
            foreach (Candle candle in portfolio.getCandles())
            {
                String values = candle.dateIndex + ";" + candle.date.ToString("dd.MM.yyyy HH:mm:ss") + ";" +
                                Math.Round(candle.value, 2) + ";" +
                                candle.printDescription();

                String tradesHistory = " ;";
                if (index < arrTrades.Length && index != 0)
                    tradesHistory += arrTrades[index].print();
                else
                    tradesHistory += ";;;;;";

                String moneyHistory = ";;";
                if (index < arrMoney.Length)
                    moneyHistory += arrMoney[index].print();

                collection.Add(values + tradesHistory + moneyHistory);
                index++;
            }

            File.WriteAllLines(FramePortfolio.resultFolder + "\\tradeResult_" + portfolio.ticket + "_" + year + "_" +
                               portfolio.title + "_" + depth + ".csv", collection);
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
                if (slice.value > max)
                    max = slice.value;

            return computeRelativeMoneyFor(max);
        }

        // used via reflection
        public double computeEndPeriodMoney()
        {
            double currentMoneyValue = (averageMoney.Count <= 0) ? currentMoney : averageMoney.Last().value;

            return computeRelativeMoneyFor(currentMoneyValue);
        }

        protected double computeRelativeMoneyFor(double value)
        {
            double beginMoneyValue = (averageMoney.Count <= 0) ? currentMoney : averageMoney.First().value;

            return 100 + Math.Round((value - beginMoneyValue) / beginMoneyValue * 100, 2);
        }

        public FramePortfolio getPortfolio()
        {
            return (FramePortfolio)portfolio;
        }
    }
}
