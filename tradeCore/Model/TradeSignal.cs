#region

using System;
using core.Model;

#endregion

namespace core.Model
{
    public class TradeSignal
    {
        public Position.Direction direction { set; get; }
        public Trade.Mode mode { set; get; }

        public TradeSignal(Position.Direction direction, Trade.Mode mode)
        {
            this.mode = mode;
            this.direction = direction;
        }

        public static TradeSignal forClosePosition(Position.Direction direction)
        {
            Position.Direction reverseDirection = Position.Direction.None;

            if (Position.Direction.Buy.Equals(direction)) reverseDirection = Position.Direction.Sell;
            if (Position.Direction.Sell.Equals(direction)) reverseDirection = Position.Direction.Buy;

            return new TradeSignal(reverseDirection, Trade.Mode.Close);
        }

        public static TradeSignal forCloseAndOpenPosition(Position.Direction direction)
        {
            return new TradeSignal(direction, Trade.Mode.CloseAndOpen);
        }

        public Boolean isNoneDirection()
        {
            return Position.Direction.None.Equals(direction);
        }

        public Boolean isClosePosition()
        {
            return Trade.Mode.Close.Equals(mode);
        }

        public Boolean isCloseAndOpenPosition()
        {
            return Trade.Mode.CloseAndOpen.Equals(mode);
        }
    }
}