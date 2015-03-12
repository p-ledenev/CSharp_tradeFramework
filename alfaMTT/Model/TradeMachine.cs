using System;
using System.Collections.Generic;
using alfaMTT.Alfa;
using core.Model;

namespace alfaMTT.Model
{
    internal class TradeMachine : Machine
    {
        public String id { get; set; }
        public bool isBlocked { get; set; }
        public Trade lastTrade { get; set; }
        public Trade lastOpenPositionTrade { get; set; }

        public TradeMachine(string decisionStrategyName, double currentMoney, int depth, Portfolio portfolio)
            : base(decisionStrategyName, currentMoney, depth, portfolio)
        {
        }

        public override Trade getLastTrade()
        {
            return lastTrade;
        }

        public override Trade getLastOpenPositionTrade()
        {
            return lastOpenPositionTrade;
        }

        public TradePortfolio getPortfolio()
        {
            return (TradePortfolio)portfolio;
        }

        public String getAccount()
        {
            return getPortfolio().account;
        }

        public int getLot()
        {
            return getPortfolio().lot;
        }

        public TerminalOrder processCandleWithIndex(int start)
        {
            TradeSignal signal = createSignalFor(start);
            int closeVolume = currentPosition.volume;

            TerminalOrder order = new TerminalOrder(this, signal.direction, closeVolume);

            if (signal.isCloseAndOpenPosition())
                return TerminalOrder.withParent(order);

            order.setMode(Trade.Mode.Close);

            return order;
        }

        public void applyTerminalOrder(TerminalOrder order)
        {
            if (currentPosition.isNone())
                openPosition(order.getDirection(), order.getVolume(), order.getTradeValue());
            else
                closePosition(order.getDate(), order.getTradeValue());

            lastTrade = new Trade(order.getDate(), 0, order.getTradeValue(), order.getDirection(), order.getVolume(), order.getMode());

            if (order.isCloseAndOpenPosition())
                lastOpenPositionTrade = lastTrade;
        }

    }
}
