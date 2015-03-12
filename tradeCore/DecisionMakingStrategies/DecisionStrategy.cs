#region

using System;
using core.Factories;
using core.Model;
using core.TakeProfitStrategies;

#endregion

namespace core.DecisionMakingStrategies
{
    public abstract class DecisionStrategy
    {
        protected Machine machine { get; set; }
        protected TakeProfitStrategy takeProfitStrategy { get; set; }

        public void initWith(Machine machine)
        {
            this.machine = machine;
            takeProfitStrategy = TakeProfitStrategyFactory.createTakeProfitStrategy(machine);

            init();
        }

        public TradeSignal tradeSignalFor(int start)
        {
            Position.Direction direction = determineTradeDirection(start);
            addAncillaryCandleRequisites(start);

            Position.Direction lastDirection = machine.getLastOpenPositionTrade().getDirection();

            if (!direction.Equals(lastDirection))
                return TradeSignal.forClosingAndOpening(direction);

            if (takeProfitStrategy.shouldTakeProfit(start))
                return TradeSignal.forClosing(direction);

            if (takeProfitStrategy.shouldReopenPosition(start))
                return TradeSignal.forClosingAndOpening(direction);

            return TradeSignal.forClosing(Position.Direction.None);
        }

        public abstract String getName();

        protected abstract Position.Direction determineTradeDirection(int start);

        protected abstract void addAncillaryCandleRequisites(int start);

        public abstract void readParamsFrom(String xml);

        protected abstract void init();
    }
}