#region

using System.Collections.Generic;
using core.Model;

#endregion

namespace core.SiftCanldesStrategies
{
    internal class NoSiftStrategy : SiftCandlesStrategy
    {
        public NoSiftStrategy()
            : base(0)
        {
        }

        public override List<Candle> sift(List<Candle> values)
        {
            return values;
        }
    }
}