namespace core.CommissionStrategies
{
    public class CommissionRequest
    {
        public double value { get; set; }
        public int volume { get; set; }
        public bool intraday { get; set; }

        public static CommissionRequest forOneLot()
        {
            return new CommissionRequest(0, 1, false);
        }

        public CommissionRequest(double value, int volume, bool intraday)
        {
            this.value = value;
            this.volume = volume;
            this.intraday = intraday;
        }
        }
}