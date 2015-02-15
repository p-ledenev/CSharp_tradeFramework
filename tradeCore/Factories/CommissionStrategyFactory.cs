using core.CommissionStrategies;

namespace core.Factories
{
    public class CommissionStrategyFactory
    {
        public static CommissionStrategy createConstantCommissionStrategy(double commission)
        {
            return createScaplingCommissionStrategy(commission);
        }

        protected static CommissionStrategy createScaplingCommissionStrategy(double commission)
        {
            return new ScalpingCommissionStrategy(commission);
        }
    }
}