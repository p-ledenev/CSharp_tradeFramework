#region

using core.Model;
using core.TakeProfitStrategies;

#endregion

namespace core.Factories
{
    internal class TakeProfitStrategyFactory
    {
        public static TakeProfitStrategy createTakeProfitStrategy(Machine machine)
        {
            return createNoTakeProfitStrategy(machine);
        }

        protected static TakeProfitStrategy createNoTakeProfitStrategy(Machine machine)
        {
            return new NoTakeProfitStrategy(machine);
        }
    }
}