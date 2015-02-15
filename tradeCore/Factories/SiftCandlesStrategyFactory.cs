#region

using core.SiftCanldesStrategies;

#endregion

namespace core.Factories
{
    public class SiftCandlesStrategyFactory
    {
        public static SiftCandlesStrategy createSiftStrategie(double siftStep)
        {
            return createMinMaxSiftStrategy(siftStep);
        }

        protected static SiftCandlesStrategy createMinMaxSiftStrategy(double siftStep)
        {
            return new MinMaxSiftStrategy(siftStep);
        }
    }
}