#region

using System;
using core.CommissionStrategies;
using core.DecisionMakingStrategies;
using core.Factories;
using core.Model;

#endregion

namespace core.Model
{
    public abstract class Machine
    {
        public double currentMoney { get; set; }
        public double maxMoney { get; set; }
        public bool isTrade { get; set; }
        public DecisionStrategy decisionStrategy { get; set; }
        public Position currentPosition { get; set; }
        public int depth { get; set; }
        protected Portfolio portfolio;

        protected Machine(String decisionStrategyName, double currentMoney, int depth, Portfolio portfolio)
        {
            this.currentMoney = currentMoney;
            this.maxMoney = currentMoney;
            this.depth = depth;

            isTrade = true;
            this.portfolio = portfolio;

            currentPosition = new Position();

            decisionStrategy = DecisionStrategyFactory.createDecisionStrategie(decisionStrategyName, this);
            decisionStrategy.readParamsFrom(null);
        }

        public abstract Trade getLastTrade();

        public double getCandleValueFor(int start)
        {
            return portfolio.getCandleValueFor(start);
        }

        public String getTicket()
        {
            return portfolio.ticket;
        }

        public abstract Trade getLastOpenPositionTrade();

        public double computeCurrentMoney()
        {
            double value = Math.Round(currentMoney / 100000, 2);

            if (currentPosition.isNone())
                return value;

            return value + Math.Round(currentPosition.computeSignedValue() / 100000, 2);
        }

        private void closePosition(Candle candle, Position.Direction direction)
        {
            if (currentPosition.isEmpty())
                return;

            bool intraday = getLastOpenPositionTrade().isIntradayFor(candle.date);
            double commission =
                portfolio.computeClosePositionCommission(new CommissionRequest(currentPosition.tradeValue,
                    currentPosition.volume, intraday));

            currentMoney += currentPosition.computeSignedValue(candle.tradeValue) - commission;

            currentPosition = new Position();
        }

        private void openPosition(Candle candle, Position.Direction direction)
        {
            if (!currentPosition.isEmpty())
                return;

            int volume = (int)Math.Floor(currentMoney / candle.tradeValue);
            currentPosition = new Position(candle.tradeValue, direction, volume);

            double commission = portfolio.computeOpenPositionCommission(new CommissionRequest(candle.tradeValue, volume, false));

            currentMoney -= currentPosition.computeSignedValue() + commission;
        }

        public virtual void operate(TradeSignal signal, int start) 
        {
            if (signal.isNoneDirection())
                throw new TradeSignalIgnored();

            if (currentPosition.isSameDirectionAs(signal.direction))
                throw new TradeSignalIgnored();

            if (currentPosition.isEmpty() && signal.isClosePosition())
                throw new TradeSignalIgnored();

            Candle candle = portfolio.candles[start];

            closePosition(candle, signal.direction);

            if (signal.isCloseAndOpenPosition())
                openPosition(candle, signal.direction);
        }

        public void trade(int start, bool onlyCalculate)
        {
            TradeSignal signal = decisionStrategy.tradeSignalFor(start);

            if (!onlyCalculate)
                operate(signal, start);

            if (portfolio.isDayChanged(start))
                Console.WriteLine(portfolio.ticket + ": depth: " + depth + "; " +
                                  portfolio.candles[start].printDescription() + " " + portfolio.candles[start].date +
                                  " " + DateTime.Now);
        }



        public Candle[] getCandles()
        {
            return portfolio.candles;
        }

        public int getCandlesLength()
        {
            return portfolio.candles.Length;
        }

        public void addCandleRequisite(String key, String value, int start)
        {
            portfolio.getCandleFor(start).addRequisite(key, value);
        }

        public String getDecisionStrategyName()
        {
            return decisionStrategy.getName();
        }

        // used via reflection
        public double getDepth()
        {
            return depth;
        }
    }
}