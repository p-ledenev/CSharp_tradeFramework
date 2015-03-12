#region

using System;

#endregion

namespace core.Model
{
    public class Candle : Description, IValue
    {
        public DateTime date { get; set; }
        public int dateIndex { get; set; }
        public double value { get; set; }
        public double tradeValue { get; set; }
        public int volume { get; set; }

        public Candle()
        {
            this.value = 0;
            this.volume = 0;
            this.date = DateTime.MinValue;
            this.dateIndex = -1;
            this.tradeValue = 0;
        }

        public Candle(double value, int volume, DateTime date, int dateIndex)
        {
            this.value = value;
            this.tradeValue = 0;
            this.volume = volume;
            this.date = date;
            this.dateIndex = dateIndex;
        }

        public double getValue()
        {
            return value;
        }

        public bool isTimeInRange(TimeSpan from, TimeSpan to)
        {
            return date.TimeOfDay >= from && date.TimeOfDay <= to;
        }

        public DateTime getDate()
        {
            return date;
        }

        public double getTradeValue()
        {
            return tradeValue;
        }

        public int getVolume()
        {
            return volume;
        }

        public String printCandle(String separator = " ")
        {
            return date + separator + value;
        }
    }
}