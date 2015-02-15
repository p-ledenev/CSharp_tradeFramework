#region

using System;

#endregion

namespace core.Model
{
    public class Slice
    {
        public DateTime date { get; set; }
        public int dateIndex { get; set; }
        public double value { get; set; }

        public Slice(DateTime date, int dateIndex, double value)
        {
            this.date = date;
            this.dateIndex = dateIndex;
            this.value = value;
        }

        public String print()
        {
            return dateIndex + ";" + date.ToString("dd.MM.yyyy HH:mm:ss") + ";" + value;
        }

        public Boolean hasEqualDate(Slice slice)
        {
            return date.Equals(slice.date);
        }
    }
}