#region

using System;
using System.Collections.Generic;
using core.Model;

#endregion

namespace core.SiftCanldesStrategies
{
    public abstract class SiftCandlesStrategy
    {
        protected double siftStep { get; set; }

        protected SiftCandlesStrategy(Double siftStep)
        {
            this.siftStep = siftStep;
        }

        public abstract List<Candle> sift(List<Candle> values);
    }
}