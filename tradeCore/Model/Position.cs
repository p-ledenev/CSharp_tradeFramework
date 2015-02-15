namespace core.Model
{
    public class Position
    {
        public enum Direction
        {
            Buy,
            Sell,
            None
        };

        public double tradeValue { get; set; }
        public Direction direction { get; set; }
        public int volume { get; set; }

        public Position()
        {
            tradeValue = 0;
            direction = Direction.None;
            volume = 0;
        }

        public Position(double tradeValue, Direction direction, int volume)
        {
            this.tradeValue = tradeValue;
            this.direction = direction;
            this.volume = volume;
        }

        public bool isBuy()
        {
            return isSameDirectionAs(Direction.Buy);
        }

        public bool isSell()
        {
            return isSameDirectionAs(Direction.Sell);
        }

        public bool isNone()
        {
            return isSameDirectionAs(Direction.None);
        }

        public bool isSameDirectionAs(Direction direction)
        {
            return direction.Equals(this.direction);
        }

        public double computeValue()
        {
            return tradeValue*volume;
        }

        public double computeValue(double tradeValue)
        {
            return tradeValue*volume;
        }

        public double computeSignedValue(double tradeValue)
        {
            return computeSign()*computeValue(tradeValue);
        }

        public double computeSignedValue()
        {
            return computeSign()*computeValue();
        }

        protected int computeSign()
        {
            if (isBuy()) return 1;

            if (isSell()) return -1;

            return 0;
        }
    }
}