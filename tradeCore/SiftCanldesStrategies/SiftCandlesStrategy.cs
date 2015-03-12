#region

using System;
using System.Collections.Generic;
using core.Model;

#endregion

namespace core.SiftCanldesStrategies
{
    public abstract class SiftCandlesStrategy
    {
        public double sieveParam { get; set; }

        protected SiftCandlesStrategy(Double sieveParam)
        {
            this.sieveParam = sieveParam;
        }

        public abstract List<Candle> sift(List<Candle> values);
    }
}