using System;
using System.Text.RegularExpressions;
using core.Model;

namespace alfaMTT.Model
{
    class TradeMachine : Machine
    {
        public TradeMachine(string decisionStrategyName, double currentMoney, int depth, Portfolio portfolio) : base(decisionStrategyName, currentMoney, depth, portfolio)
        {
        }

        public override Trade getLastTrade()
        {
            throw new NotImplementedException();
        }

        public override Trade getLastOpenPositionTrade()
        {
            throw new NotImplementedException();
        }

        public TradePortfolio getPortfolio()
        {
            return (TradePortfolio)  portfolio;
        }

        public String getAccount()
        {
            return getPortfolio().account;
        }
    }
}
