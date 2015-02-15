#region

using System;
using core.ApproximationConstructors;
using core.Factories;
using core.Model;

#endregion

namespace core.DecisionMakingStrategies
{
    internal class ApproximationDecisionStrategy : DecisionStrategy
    {
        public Approximation[] approximations { get; set; }

        public override string getName()
        {
            return "approximation";
        }

        protected override Position.Direction determineTradeDirection(int start)
        {
            ApproximationConstructor constructor = ApproximationConstructorFactory.createConstructor();
            Approximation ap = constructor.approximate(machine.getCandles(), start, machine.depth);

            approximations[start] = ap;

            return determineTradeDirection(ap);
        }

        protected override void addAncillaryCandleRequisites(int start)
        {
            Approximation ap = approximations[start];

            machine.addCandleRequisite("k", Math.Round(ap.k[0], 4).ToString(), start);
            machine.addCandleRequisite("kx+b", ap.printPowerFunc(), start);
            machine.addCandleRequisite("approximatedValue", ap.countFunctionFor().ToString(), start);
        }

        public override void readParamsFrom(string xml)
        {
        }

        protected override void init()
        {
            approximations = new Approximation[machine.portfolio.candles.Length];

            for (int i = 0; i < approximations.Length; i++)
                approximations[i] = new Approximation();
        }

        protected Position.Direction determineTradeDirection(Approximation ap)
        {
            if (ap == null || ap.k == null)
                return Position.Direction.None;

            if (ap.k[0] > 0) return Position.Direction.Buy;
            if (ap.k[0] < 0) return Position.Direction.Sell;

            return Position.Direction.None;
        }
    }
}