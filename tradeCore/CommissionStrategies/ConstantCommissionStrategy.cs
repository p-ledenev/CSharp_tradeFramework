namespace core.CommissionStrategies
{
    internal abstract class ConstantCommissionStrategy : CommissionStrategy
    {
        protected double commission;

        protected ConstantCommissionStrategy(double commission) : base()
        {
            this.commission = commission;
        }
    }
}