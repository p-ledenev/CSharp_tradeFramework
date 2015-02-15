#region

using System;
using System.Collections.Generic;
using System.Linq;
using core.Model;

#endregion

namespace core.SiftCanldesStrategies
{
    internal class MinMaxSiftStrategy : SiftCandlesStrategy
    {
        public MinMaxSiftStrategy(double siftStep)
            : base(siftStep)
        {
        }

        public override List<Candle> sift(List<Candle> values)
        {
            List<Candle> sifted = new List<Candle>();
            Candle[] arrValues = values.ToArray();

            double maxValue = arrValues[0].value;
            double minValue = arrValues[0].value;

            for (int i = 0; i < arrValues.Length - 1; i++)
            {
                Candle value = arrValues[i];

                if (value.value > maxValue) maxValue = value.value;
                if (value.value < minValue) minValue = value.value;

                if (sifted.Count > 0 && computeVariance(maxValue, value.value) < siftStep &&
                    computeVariance(minValue, value.value) < siftStep)
                    continue;

                value.tradeValue = arrValues[i + 1].value;
                value.dateIndex = sifted.Count() + 1;

                sifted.Add(value);

                maxValue = value.value;
                minValue = value.value;
            }

            return sifted;
        }

        protected double computeVariance(double x1, double x2)
        {
            return Math.Abs(x1 - x2)/x1*100;
        }
    }
}