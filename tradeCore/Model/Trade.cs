#region

using System;
using core.Model;

#endregion

namespace core.Model
{
    public class Trade
    {
        public enum Mode
        {
            Close,
            CloseAndOpen
        };

        public Position position { get; set; }
        public DateTime date { get; set; }
        public int dateIndex { get; set; }
        public Mode mode { get; set; }

        public static Trade createEmpty()
        {
            return new Trade(new DateTime(), 0, 0, Position.Direction.None, 0, Mode.Close);
        }

        public static Trade createSample()
        {
            return new Trade(new DateTime(), 0, 0, Position.Direction.None, 0, Mode.CloseAndOpen);
        }

        public Trade()
        {
            }

        public Trade(DateTime date, int dateIndex, double tradeValue, Position.Direction direction, int volume,
            Mode mode)
        {
            position = new Position(tradeValue, direction, volume);

            this.date = date;
            this.dateIndex = dateIndex;
            this.mode = mode;
        }

        public String print()
        {
            String result = dateIndex + ";" + date.ToString("dd.MM.yyyy HH:mm:ss") + ";";
            String operation = (position.isBuy()) ? position.tradeValue + "; " : " ;" + position.tradeValue;

            result += (isCloseAndOpenPosition()) ? operation + " ; " : " ; ;" + operation;

            return result + ";";
        }

        public String printPreview()
        {
            return dateIndex + ";" + date.ToString("dd.MM.yyyy HH:mm:ss") + ";";
        }

        public Double countSignedValue()
        {
            return position.computeSignedValue();
        }

        // Trade day: from 19.00 yesterday to 19.00 torday
        public bool isIntradayFor(DateTime date)
        {
            if (date.Hour > 19)
                return (this.date.Day == date.Day && this.date.Hour > 19);

            if (this.date.Day == date.Day)
                return true;

            return (this.date.Day == date.Day - 1 && this.date.Hour > 19);
        }

        public bool isSameDirectionAs(Position.Direction direction)
        {
            return position.isSameDirectionAs(direction);
        }

        public bool isSameDirectionAs(Trade trade)
        {
            return isSameDirectionAs(trade.getDirection());
        }

        public bool isBuy()
        {
            return position.isBuy();
        }

        public bool isSell()
        {
            return position.isSell();
        }

        public bool isNone()
        {
            return position.isNone();
        }

        public int getVolume()
        {
            return position.volume;
        }

        public DateTime getDate()
        {
            return date;
        }

        public double getTradeValue()
        {
            return position.tradeValue;
        }

        public Position.Direction getDirection()
        {
            return position.direction;
        }

        public bool isClosePosition()
        {
            return Mode.Close.Equals(mode);
        }

        public bool isCloseAndOpenPosition()
        {
            return Mode.CloseAndOpen.Equals(mode);
        }

        public void setPosition(Position.Direction direction, int volume)
        {
            position = new Position(0, direction, volume);
        }

        public void setTradeValue(double value)
        {
            position.tradeValue = value;
        }

        public void setVolume(int volume)
        {
            position.volume = volume;
        }

        public bool isEarlierThan(Trade trade)
        {
            return date.CompareTo(trade.getDate()) < 0;
        }
    }
}