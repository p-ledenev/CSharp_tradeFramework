#region

using core.SiftCanldesStrategies;

#endregion

namespace core.Factories
{
    public class SiftCandlesStrategyFactory
    {
        public static SiftCandlesStrategy createSiftStrategie(double sieveParam)
        {
            return createMinMaxSiftStrategy(sieveParam);
        }

        protected static SiftCandlesStrategy createMinMaxSiftStrategy(double sieveParam)
        {
            return new MinMaxSiftStrategy(sieveParam);
        }
    }
}