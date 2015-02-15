#region

using System;
using core.Model;

#endregion

namespace core.TakeProfitStrategies
{
    internal class NoTakeProfitStrategy : TakeProfitStrategy
    {
        public NoTakeProfitStrategy(Machine machine)
            : base(machine)
        {
        }

        public override bool shouldTakeProfit(int start)
        {
            return false;
        }

        public override bool shouldReopenPosition(int start)
        {
            return true;
        }

        public override void readParamsFrom(String xml)
        {
        }
    }
}