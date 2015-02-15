#region

using System;
using core.Model;

#endregion

namespace core.TakeProfitStrategies
{
    public abstract class TakeProfitStrategy
    {
        public Machine machine { get; set; }

        public TakeProfitStrategy(Machine machine)
        {
            this.machine = machine;
        }

        public abstract bool shouldTakeProfit(int start);

        public abstract bool shouldReopenPosition(int start);

        public abstract void readParamsFrom(String xml);
    }
}