#region

using System;
using System.Collections.Generic;
using System.Linq;
using core.AverageConstructors;
using core.Factories;
using core.Model;

#endregion

namespace core.DecisionMakingStrategies
{
    internal class AveraginingDecisionStrategy : DecisionStrategy
    {
        public Derivative[] derivatives { get; set; }

        public override string getName()
        {
            return "averagining";
        }

        protected override Position.Direction determineTradeDirection(int start)
        {
            if (start < 1)
                return Position.Direction.None;

            Derivative derivative = buildDerivative(start);

            return determineTradeDirection(derivative);
        }

        protected override void addAncillaryCandleRequisites(int start)
        {
            machine.addCandleRequisite("averageDerivative",
                Math.Round(derivatives[start].averageDerivative*1000, 4).ToString(), start);
        }

        protected Position.Direction determineTradeDirection(Derivative derivative)
        {
            if (derivative == null)
                return Position.Direction.None;

            if (derivative.averageDerivative > 0)
                return Position.Direction.Buy;

            if (derivative.averageDerivative < 0)
                return Position.Direction.Sell;

            return Position.Direction.None;
        }

        protected void addDerivative(int start, int depth)
        {
            AverageConstructor constructor = AverageConstructorFactory.createConstructor();

            derivatives[start].averageValue = constructor.average(machine.getCandlesArray(), start, depth);

            derivatives[start].derivative = (derivatives[start].averageValue - derivatives[start - 1].averageValue)/
                                            derivatives[start].averageValue;

            derivatives[start].averageDerivative = constructor.average(derivatives, start, depth);
        }

        protected Derivative buildDerivative(int start)
        {
            addDerivative(start, machine.depth);

            return derivatives[start];
        }

        public override void readParamsFrom(string xml)
        {
        }

        protected override void init()
        {
            derivatives = new Derivative[machine.getCandles().Count];

            for (int i = 0; i < derivatives.Length; i++)
                derivatives[i] = new Derivative();
        }
    }
}