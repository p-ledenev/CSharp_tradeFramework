#region

using System;
using System.Collections.Generic;
using core.CommissionStrategies;
using core.DecisionMakingStrategies;
using core.Factories;
using core.Model;

#endregion

namespace core.Model
{
    public abstract class Machine
    {
        public double initialMoney { get; set; }
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
            this.initialMoney = currentMoney;
            this.depth = depth;

            isTrade = true;
            this.portfolio = portfolio;

            currentPosition = new Position();

            decisionStrategy = DecisionStrategyFactory.createDecisionStrategie(decisionStrategyName, this);
            decisionStrategy.readParamsFrom(null);
        }

        public abstract Trade getLastTrade();

        public abstract Trade getLastOpenPositionTrade();

        public double getCandleValueFor(int start)
        {
            return portfolio.getCandleValueFor(start);
        }

        public String getTicket()
        {
            return portfolio.ticket;
        }

        public double computeCurrentMoney()
        {
            double value = Math.Round(currentMoney / 100000, 2);

            if (currentPosition.isNone())
                return value;

            return value + Math.Round(currentPosition.computeSignedValue() / 100000, 2);
        }

        public void closePosition(Candle candle)
        {
            closePosition(candle.getDate(), candle.getTradeValue());
        }

        public void closePosition(DateTime date, double tradeValue)
        {
            if (currentPosition.isNone())
                return;

            bool intraday = getLastOpenPositionTrade().isIntradayFor(date);
            double commission =
                portfolio.computeClosePositionCommission(new CommissionRequest(currentPosition.tradeValue,
                    currentPosition.volume, intraday));

            currentMoney += currentPosition.computeSignedValue(tradeValue) - commission;

            currentPosition = new Position();
        }

        public void openPosition(Candle candle, Position.Direction direction)
        {
            double tradeValue = candle.getTradeValue();
            int volume = (int)Math.Floor(currentMoney / tradeValue);

            openPosition(direction, volume, tradeValue);
        }

        public void openPosition(Position.Direction direction, int volume, double tradeValue)
        {
            if (!currentPosition.isNone())
                return;

            currentPosition = new Position(tradeValue, direction, volume);

            double commission = portfolio.computeOpenPositionCommission(new CommissionRequest(tradeValue, volume, false));

            currentMoney -= currentPosition.computeSignedValue() + commission;
        }

        public TradeSignal createSignalFor(int start, bool onlyCalculate = false)
        {
            TradeSignal signal = decisionStrategy.tradeSignalFor(start);

            if (onlyCalculate)
                throw new TradeSignalIgnored();

            if (signal.isNoneDirection())
                throw new TradeSignalIgnored();

            if (currentPosition.isSameDirectionAs(signal.direction))
                throw new TradeSignalIgnored();

            if (currentPosition.isNone() && signal.isClosePosition())
                throw new TradeSignalIgnored();

            return signal;
        }

        public List<Candle> getCandles()
        {
            return portfolio.getCandles();
        }

        public Candle[] getCandlesArray()
        {
            return getCandles().ToArray();
        }

        public int getCandlesLength()
        {
            return portfolio.getCandles().Count;
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

        public Position.Direction getPositionDirection()
        {
            return currentPosition.direction;
        }

        public double getPositionValue()
        {
            return currentPosition.tradeValue;
        }

        public int getPositionVolume()
        {
            return currentPosition.volume;
        }

        public void setPosition(Position.Direction direction, double value, int volume)
        {
            currentPosition.direction = direction;
            currentPosition.tradeValue = value;
            currentPosition.volume = volume;
        }
    }
}