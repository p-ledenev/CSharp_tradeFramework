namespace core.CommissionStrategies
{
    public abstract class CommissionStrategy
    {
        protected CommissionStrategy()
        {
        }

        public abstract double computeOpenPositionCommission(CommissionRequest request);

        public abstract double computeClosePositionCommission(CommissionRequest request);
    }
}