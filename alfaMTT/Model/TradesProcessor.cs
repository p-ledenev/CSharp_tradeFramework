using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using alfaMTT.Alfa;
using alfaMTT.Settings;
using core.Model;

namespace alfaMTT.Model
{
    class TradesProcessor
    {
        public List<TradePortfolio> portfolios { get; set; }
        public TerminalGateway terminal { get; set; }
        
        protected void checkVolume()
        {
            foreach (TradePortfolio portfolio in portfolios)
            {
                int alfaVolume = terminal.loadTicketVolume(portfolio.account, portfolio.ticket);
                int machineVolume = countVolumeFor(portfolio.ticket);

                if (alfaVolume != (int)-1E8 && alfaVolume != machineVolume)
                    Logger.printInfo(DateTime.Now,
                        "Volume for ticket " + portfolio.ticket + " not equal. Expected " + machineVolume + ", got " + alfaVolume);
            }
        }

        protected void checkBlocked()
        {
            foreach (TradePortfolio portfolio in portfolios)
                foreach (TradeMachine machine in portfolio.machines)
                    if (machine.isBlocked)
                        Logger.printInfo(DateTime.Now, machine, "this portfolio is blocked");
        }

        protected void calculateMaxMoney()
        {
            foreach (TradePortfolio portfolio in portfolios)
                portfolio.calculateMaxMoney();
        }

        protected List<TerminalOrder> createTerminalOrders()
        {
            List<TerminalOrder> orders = new List<TerminalOrder>();
            foreach (TradePortfolio portfolio in portfolios)
            {
                bool isCandleAdded = addNextCandleTo(portfolio);

                orders.AddRange(portfolio.collectTerminalOrders());

                if (!isCandleAdded)
                    Console.WriteLine("No new candle added. for portfolio " + portfolio.id);
            }
            
            return orders;
        }

        public void trade()
        {
            while (true)
            {
                List<TerminalOrder> orders = createTerminalOrders();

                if (orders.Count <= 0)
                {
                    Console.WriteLine("No orders to execute.\n");

                    fallAsleep(DateTime.Now);
                    continue;
                }

                TerminalOrdersProcessor processor = new TerminalOrdersProcessor(orders);

                Console.WriteLine(processor.printOrders());
                Console.WriteLine("\nCreate all orders success. Try to execute.");
                
                processor.process();

                calculateMaxMoney();
                PortfolioInitializator.writeIni(portfolios, false);

                Console.WriteLine("Execution finished.\n");

                fallAsleep(DateTime.Now);
            }
        }

        protected bool addNextCandleTo(Portfolio portfolio)
        {
            try
            {
                if (portfolio.getCandles().Any())
                    addLastCandlesTo(portfolio);

                initCandles(portfolio);
            }
            catch (Exception e)
            {
                Logger.printInfo(DateTime.Now, portfolio, e.Message);

                return false;
            }

            return true;
        }

        private void addLastCandlesTo(Portfolio portfolio)
        {
            DateTime date = DateTime.Now;

            List<Candle> candles = terminal.loadCandles(portfolio.ticket, portfolio.getLastCandleDate(), date);

            if (candles.Count <= 0)
                throw new Exception("addLastCandles: trade values for period (" + portfolio.getLastCandleDate() +
                       "-" + date + ") is absent");

            while (candles.Count > 0 && candles.Last().date.Minute == date.Minute + 1)
                candles.Remove(candles.Last());

            foreach (Candle candle in candles)
                Console.WriteLine(candle.printCandle());

            int count = portfolio.addCandlesRange(candles);
            portfolio.removeUnusedCandles(count);

            if (count <= 0)
                throw new Exception("addLastCandles: no new candles added");
        }

        private void initCandles(Portfolio portfolio)
        {
            DateTime earliestTradeDate = portfolio.getEarliestTrade().getDate();
            int maxDepth = portfolio.getMaxDepth();

            DateTime dateTo = DateTime.Now;
            DateTime dateFrom = earliestTradeDate.AddMinutes(-1);

            List<Candle> candlesAfter = terminal.loadCandles(portfolio.ticket, dateFrom, dateTo);

            if (candlesAfter.Count <= 0)
                Logger.printInfo(DateTime.Now, portfolio, "initCandles: No trade data for period (" + dateFrom + " - " + dateTo + ")");

            List<Candle> candlesBefore = new List<Candle>();
            for (int i = 1; i < maxDepth; i++)
            {
                dateTo = earliestTradeDate;
                dateFrom = earliestTradeDate.AddMinutes(-maxDepth * 5 * i);

                candlesBefore = terminal.loadCandles(portfolio.ticket, dateFrom, dateTo);

                if (portfolio.countSiftedRange(candlesBefore) < maxDepth)
                    Logger.printInfo(DateTime.Now, portfolio, "initCandles: Not enough data for depth " + maxDepth);
            }

            portfolio.addCandlesRange(candlesBefore);
            portfolio.addCandlesRange(candlesAfter);
        }

        public void printPortfolios()
        {
            foreach (TradePortfolio portfolio in portfolios)
            {
                portfolio.printPortfolio();
                Console.WriteLine("");
            }
        }

        protected void fallAsleep(DateTime date)
        {
            terminal.checkConnect();

            DateTime tradeBegin = new DateTime(date.Year, date.Month, date.Day, 10, 04, 50);
            DateTime tradeEnd = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0) + TerminalGateway.tradeTo;

            DateTime tradePauseBegin = new DateTime(date.Year, date.Month, date.Day, 18, 45, 00);
            DateTime tradePauseEnd = new DateTime(date.Year, date.Month, date.Day, 19, 00, 00);

            TimeSpan rclock = new TimeSpan(0, 0, 0, 0);
            TimeSpan postfix = new TimeSpan(0, 10, 04, 50);
            TimeSpan prefix = new TimeSpan(0, 23 - date.Hour, 59 - date.Minute, 59 - date.Second);

            bool weekend = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);

            if (date.DayOfWeek == DayOfWeek.Saturday) rclock = new TimeSpan(0, 24, 0, 0);

            //if (dt.GetDayOfWeek() != 7) weekend = true;

            if (date >= tradeBegin && date <= tradeEnd && !weekend)
            {
                postfix = new TimeSpan(0, 0, 0, 0);

                int upSeconds = 50;
                int delta = (date.Second >= upSeconds) ? 60 - date.Second : -date.Second;
                prefix = new TimeSpan(0, 0, 0, upSeconds + delta);
            }

            if (date >= tradePauseBegin && date <= tradePauseEnd)
            {
                prefix = new TimeSpan(0, 0, 61 - date.Minute, 0);
            }

            if (date < tradeBegin)
            {
                prefix = new TimeSpan(0, 0, 0, 0);
                postfix = new TimeSpan(0, 10 - date.Hour, 04 - date.Minute, 50 - date.Second);
            }

            if (date < tradePauseBegin || date > tradePauseEnd)
                checkBlocked();

            if (date < tradePauseBegin)
                checkVolume();

            Console.WriteLine("wake up " + (date + rclock + prefix + postfix).ToString("dd.MM.yyyy HH:mm:ss"));

            Thread.Sleep((Int32)Math.Round((rclock + prefix + postfix).TotalMilliseconds));
        }

        protected int countVolumeFor(String ticket)
        {
            int volume = 0;
            foreach (TradePortfolio portfolio in portfolios)
            {
                if (portfolio.ticket == ticket)
                    volume += portfolio.getVolume();
            }

            return volume;
        }
    }
}
