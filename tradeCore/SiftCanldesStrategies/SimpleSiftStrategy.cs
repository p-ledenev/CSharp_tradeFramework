#region

using System;
using System.Collections.Generic;
using System.Linq;
using core.Model;

#endregion

namespace core.SiftCanldesStrategies
{
    internal class SimpleSiftCandlesStrategy : SiftCandlesStrategy
    {
        public SimpleSiftCandlesStrategy(double sieveParam)
            : base(sieveParam)
        {
        }

        public override List<Candle> sift(List<Candle> values)
        {
            List<Candle> sifted = new List<Candle>();
            Candle[] arrValues = values.ToArray();

            double lastValue = arrValues[0].value;

            for (int i = 0; i < arrValues.Length - 1; i++)
            {
                Candle value = arrValues[i];

                if (sifted.Count <= 0 || Math.Abs(lastValue - value.value)/lastValue*100 >= sieveParam)
                {
                    value.tradeValue = arrValues[i + 1].value;
                    value.dateIndex = sifted.Count() + 1;

                    sifted.Add(value);

                    lastValue = value.value;
                }
            }

            return sifted;
        }
    }
}