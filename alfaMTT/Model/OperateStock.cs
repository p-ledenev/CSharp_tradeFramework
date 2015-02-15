using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using alfaMTT.Alfa;
using alfaMTT.Settings;

namespace alfaMTT.Model
{
    class OperateStock
    {
        public static String path { get; set; }

        public List<TerminalOrder> orders { get; set; }
        public List<GPortfolio> gPortfolios { get; set; }
        public TerminalGateway alfaDirectGateway { get; set; }

        public OperateStock()
        {
            orders = new List<TerminalOrder>();
        }

        public void clearOrdersList()
        {
            orders = new List<TerminalOrder>();
        }

        public void checkVolume()
        {
            foreach (GPortfolio gp in gPortfolios)
            {
                int alfaVolume = alfaDirectGateway.loadTicketVolume(gp.account, gp.ticket);
                int machineVolume = GPortfolio.countVolumeFor(gp.ticket, gPortfolios);

                if (alfaVolume != (int)-1E8 && alfaVolume != machineVolume)
                    TerminalGateway.printInfo(DateTime.Now,
                                                "Volume for ticket " + gp.ticket + " not equal. Expected " +
                                                machineVolume + ", got " + alfaVolume);
            }
        }

        public void checkBlocked()
        {
            foreach (GPortfolio gp in gPortfolios)
                foreach (Portfolio p in gp.pfts)
                    if (p.isBlocked)
                        TerminalGateway.printInfo(DateTime.Now, p, "this portfolio is blocked");
        }

        public void setNewExtremum()
        {
            foreach (GPortfolio gp in gPortfolios)
                gp.setMoneyMax();
        }

        public void trade()
        {
            while (true)
            {
                bool isCandleAdded = false;
                foreach (GPortfolio gpft in gPortfolios)
                {
                    bool addNewCandle = addTimeCande(gpft);

                    foreach (Portfolio pft in gpft.pfts)
                    {
                        if (!pft.isBlocked)
                        {
                            if (pft.alarmStop()) Ini.writeIni(gPortfolios, false);

                            if (pft.isTrade) pft.addOrderTo(orders);
                            else pft.addCloseOrderTo(orders);

                            if (addNewCandle || !pft.isTrade) isCandleAdded = true;
                        }
                    }
                }

                if (isCandleAdded)
                {
                    foreach (TerminalOrder order in orders)
                        order.printOrder();

                    Console.WriteLine("");
                    Console.WriteLine("\nCreate all orders success. Try to execute.");

                    TerminalOrder.executeOrders(orders);

                    if (orders.Count > 0)
                    {
                        setNewExtremum();
                        Ini.writeIni(gPortfolios, false);
                    }

                    Console.WriteLine("Execution finished.\n");
                }
                else
                {
                    Console.WriteLine("No new value added.\n");
                }

                clearOrdersList();
                fallASleep(DateTime.Now);
            }
        }

        public bool addTimeCande(GPortfolio gpft)
        {
            if (gpft.values.Count <= 0)
            {
                initTimeCandles(gpft);

                return true;
            }

            return addLastTimeCandles(gpft);
        }

        private bool addLastTimeCandles(GPortfolio gpft)
        {
            DateTime date = DateTime.Now;

            List<Candle> candles = alfaDirectGateway.loadFinInstrumentData(gpft.ticket, gpft.sifter.dt, date);

            if (candles.Count <= 0)
            {
                TerminalGateway.printInfo(date, gpft, "addLastTimeCandles: trade values for period (" + gpft.sifter.dt +
                    "-" + date + ") is absent");

                return false;
            }

            while (candles.Count > 0 && candles.Last().dt.Minute == date.Minute + 1)
                candles.Remove(candles.Last());

            foreach (Candle candle in candles)
                Console.WriteLine(candle.printCandle());

            int count = gpft.sifter.siftTo(gpft.values, candles);
            if (count > 0)
            {
                gpft.removeUnusedCandles(count);

                return true;
            }

            return false;
        }

        private void initTimeCandles(GPortfolio gpft)
        {
            Portfolio pft = gpft.getMaxNescessaryValuesDepthPortfilio();
            Trade lastDeal = pft.getLastTrade();
            int maxDepth = pft.getNecessaryValuesDepth();

            DateTime dateNow = DateTime.Now.AddDays(1);
            DateTime lastDealDate = DateTime.MinValue;

            List<Candle> candlesAfter = new List<Candle>();
            List<Candle> candlesBefore = new List<Candle>();

            while (candlesAfter.Count <= 0)
            {
                dateNow = getTradeTime(dateNow.AddDays(-1));
                lastDealDate = dateNow.AddMinutes(-1);

                if (lastDeal != null)
                {
                    int seconds = (lastDeal.stock.dt.Second > 40) ? lastDeal.stock.dt.Second : lastDeal.stock.dt.Second + 60;
                    lastDealDate = lastDeal.stock.dt.AddSeconds(-seconds);
                }

                candlesAfter = alfaDirectGateway.loadFinInstrumentData(gpft.ticket, lastDealDate, dateNow);
                candlesAfter = gpft.sifter.siftFromFirst(candlesAfter);

                if (candlesAfter.Count <= 0)
                    TerminalGateway.printInfo(DateTime.Now, gpft, "initTimeCandles: No trade data for period (" + lastDealDate + "-" + dateNow + ")");
            }
            candlesAfter.Remove(candlesAfter.First());

            for (int i = 1; candlesBefore.Count < maxDepth; i++)
            {
                DateTime dateFrom = lastDealDate.AddMinutes(-maxDepth * 5 * i);

                candlesBefore = alfaDirectGateway.loadFinInstrumentData(gpft.ticket, dateFrom, lastDealDate);
                candlesBefore.Reverse();
                candlesBefore = gpft.sifter.siftFromFirst(candlesBefore);
                candlesBefore.Reverse();

                if (candlesBefore.Count < maxDepth)
                    TerminalGateway.printInfo(DateTime.Now, gpft, "initTimeCandles: Not enough data for depth " + maxDepth);
            }
            candlesBefore.RemoveRange(0, candlesBefore.Count - maxDepth);

            gpft.values.AddRange(candlesBefore);
            gpft.values.AddRange(candlesAfter);

            gpft.setSifter();
        }

        private DateTime getTradeTime(DateTime dt)
        {
            TimeSpan beginTrade = TerminalGateway.tradeFrom;
            TimeSpan endTrade = TerminalGateway.tradeTo;

            if (dt.DayOfWeek.Equals(DayOfWeek.Sunday))
            {
                dt = dt.AddDays(-2);
                return dt.Date + endTrade;
            }

            if (dt.DayOfWeek.Equals(DayOfWeek.Saturday))
            {
                dt = dt.AddDays(-1);
                return dt.Date + endTrade;
            }

            if (dt.TimeOfDay > endTrade)
            {
                return dt.Date + endTrade;
            }

            if (dt.TimeOfDay < beginTrade)
            {
                return getTradeTime(dt.AddDays(-1).Date + endTrade);
            }

            return dt;
        }

        public void printPortfolios()
        {
            for (int i = 0; i < gPortfolios.Count; i++)
            {
                gPortfolios[i].printPortfolio();
                Console.WriteLine("");
            }
        }

        public void fallASleep(DateTime dt)
        {
            alfaDirectGateway.checkConnect();
            
            DateTime tradeBegin = new DateTime(dt.Year, dt.Month, dt.Day, 10, 04, 50);
            DateTime tradeEnd = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0) + TerminalGateway.tradeTo;

            DateTime tradePauseBegin = new DateTime(dt.Year, dt.Month, dt.Day, 18, 45, 00);
            DateTime tradePauseEnd = new DateTime(dt.Year, dt.Month, dt.Day, 19, 00, 00);

            TimeSpan rclock = new TimeSpan(0, 0, 0, 0);
            TimeSpan postfix = new TimeSpan(0, 10, 04, 50);
            TimeSpan prefix = new TimeSpan(0, 23 - dt.Hour, 59 - dt.Minute, 59 - dt.Second);

            bool weekend = (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday);

            if (dt.DayOfWeek == DayOfWeek.Saturday) rclock = new TimeSpan(0, 24, 0, 0);

            //if (dt.GetDayOfWeek() != 7) weekend = true;

            if (dt >= tradeBegin && dt <= tradeEnd && !weekend)
            {
                postfix = new TimeSpan(0, 0, 0, 0);

                int upSeconds = 50;
                int delta = (dt.Second >= upSeconds) ? 60 - dt.Second : -dt.Second;
                prefix = new TimeSpan(0, 0, 0, upSeconds + delta);
            }

            if (dt >= tradePauseBegin && dt <= tradePauseEnd)
            {
                prefix = new TimeSpan(0, 0, 61 - dt.Minute, 0);
            }

            if (dt < tradeBegin)
            {
                prefix = new TimeSpan(0, 0, 0, 0);
                postfix = new TimeSpan(0, 10 - dt.Hour, 04 - dt.Minute, 50 - dt.Second);
            }

            if (dt < tradePauseBegin || dt > tradePauseEnd)
                checkBlocked();

            if (dt < tradePauseBegin)
                checkVolume();

            Console.WriteLine("wake up " + (dt + rclock + prefix + postfix).ToString("dd.MM.yyyy HH:mm:ss"));

            Thread.Sleep((Int32)Math.Round((rclock + prefix + postfix).TotalMilliseconds));
        }
    }
}
