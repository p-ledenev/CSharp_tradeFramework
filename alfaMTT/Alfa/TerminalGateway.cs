using System;
using System.Collections.Generic;
using System.Threading;
using ADLite;
using core.Model;

namespace alfaMTT.Alfa
{
    class AlfaDirectGateway
    {
        public static TimeSpan tradeFrom { get; set; }
        public static TimeSpan tradeTo { get; set; }

        public AlfaDirect AD { get; set; }
        public String login { get; set; }
        public String password { get; set; }

        public AlfaDirectGateway(String login, String password)
        {
            this.login = login;
            this.password = password;

            setAD();
        }

        private void setAD()
        {
            AD = new AlfaDirect
            {
                UserName = login,
                Password = password,
                Connected = true
            };
        }

        public void checkConnect()
        {
            bool isLost = false;
            if (AD == null || !AD.Connected)
            {
                isLost = true;
                Logger.printInfo(DateTime.Now, "CheckConnect: Connection to AlfaDirect is lost");
            }

            while (AD == null || !AD.Connected)
            {
                Thread.Sleep(10 * 1000);
                setAD();

                Thread.Sleep(10 * 1000);
            }

            if (isLost)
                Logger.printInfo(DateTime.Now, "CheckConnect: Connection to AlfaDirect is set up");
        }

        public void dropOrder(TerminalOrder order, bool toScreen = true)
        {
            checkConnect();

            AD.DropOrder(order.orderNumber, null, null, null, null, null, 3);

            if (!isLastADOperationSucceed("DropOrder", order, false))
                throw new TerminalGatewayFailure();

            String resultMessage = AD.LastResultMsg;

            Logger.printInfo(DateTime.Now, order.machine, "DropOrder: State success. " + resultMessage + (resultMessage == null ? " No result recieved." : ""), toScreen);

            if (resultMessage == null || !resultMessage.Contains("удалена"))
                throw new TerminalGatewayFailure();
        }

        public double loadLastValue(TerminalOrder order)
        {
            checkConnect();

            String lastValue = AD.GetLocalDBData("fin_info", "last_price, status", "p_code in (\"" + order.getTicket() + "\")");

            if (!isLastADOperationSucceed("LoadLastValue", order))
                throw new TerminalGatewayFailure();

            if (lastValue == null)
            {
                Logger.printInfo(DateTime.Now, order.machine, "GetLastValue: State success. No data recieved");
                throw new TerminalGatewayFailure();
            }

            String[] data = lastValue.Split('|', '\r');

            if (!data[1].Equals("6"))
            {
                Logger.printInfo(DateTime.Now, order.machine, "GetLastValue: State success. No trades started.");
                throw new TerminalGatewayFailure();
            }

            return Double.Parse(data[0]);
        }

        public String loadResultFor(TerminalOrder order)
        {
            checkConnect();

            String result = AD.GetLocalDBData("trades", "price, qty", "ord_no = " + order.orderNumber);

            if (isLastADOperationSucceed("CheckOrder", order))
                throw new TerminalGatewayFailure();

            Logger.printInfo(DateTime.Now, order.machine, "CheckOrder: State success. " + AD.LastResultMsg + (result == null ? " No result recieved." : ""));

            if (result == null)
                throw new TerminalGatewayFailure();

            return result;
        }

        public int submitOrder(TerminalOrder order)
        {
            checkConnect();

            int orderNo = AD.CreateLimitOrder(order.getAccount(), "FORTS", order.getTicket(),
                                  DateTime.Now.AddDays(1), "", "RUR", order.getDirection(), order.getVolume(),
                                  order.getTradeValue(), null, null, null, null, null, null, null, null, null,
                                  null, null, null, null, null, null, null, 11);

            if (isLastADOperationSucceed("CreateOrder", order))
                throw new TerminalGatewayFailure();

            Logger.printInfo(DateTime.Now, order.machine, "CreateOrder: State success. " + AD.LastResultMsg +
                (orderNo <= 0 ? " No result recieved. Machine will be blocked." : ""));

            if (orderNo <= 0)
                throw new OrderSubmissionFailure();

            return orderNo;
        }

        public List<Candle> loadCandles(String ticket, DateTime dateFrom, DateTime dateTo)
        {
            checkConnect();

            /*
            0 – 1 minute; 1 – 5 minutes; 2 – 10 minutes; 3 – 15 minutes; 4 – 30 minutes; 5 – 1 hour;
            6 – 1 day; 7 – 1 week; 8 – 1 month; 9 – 1 year.
            */

            String result = AD.GetArchiveFinInfo("FORTS", ticket, 0, dateFrom, dateTo, 3, 20);

            Console.WriteLine(ticket + " (" + dateFrom + "-" + dateTo + ") read data " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

            if (isLastADOperationSucceed("LoadCandles") || result == null)
                return new List<Candle>();

            return parseFinInstrumentData(result);
        }

        public int loadTicketVolume(String account, String ticket)
        {
            checkConnect();

            String result = AD.GetLocalDBData("balance", "real_rest", "acc_code in (\"" + account + "\") and p_code in (\"" + ticket + "\")");

            if (isLastADOperationSucceed("loadTicketVolume"))
                throw new TerminalGatewayFailure();

            if (result == null)
            {
                Logger.printInfo(DateTime.Now, "loadTicketVolume: " + AD.LastResultMsg + " " + (AD.LastResultMsg == null ? "No message recieved" : ""));
                throw new TerminalGatewayFailure();
            }

            String[] volume = result.Split(new[] { "\n", "\r", "|" }, StringSplitOptions.RemoveEmptyEntries);

            return Int32.Parse(volume[0]);
        }

        private List<Candle> parseFinInstrumentData(String data)
        {
            String[] result = data.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<Candle> candles = new List<Candle>();

            foreach (String line in result)
            {
                String[] strCandle = line.Split('|');
                Candle canlde = new Candle
                {
                    value = Double.Parse(strCandle[4]),
                    date = DateTime.Parse(strCandle[0])
                };

                if (canlde.isTimeInRange(tradeFrom, tradeTo))
                    candles.Add(canlde);
            }

            return candles;
        }

        protected bool isLastADOperationSucceed(String operation, TerminalOrder order, bool toScreen = true)
        {
            if (AD.LastResult == StateCodes.stcSuccess)
                return true;

            String message = operation + ": " + getLastOperationMessage();

            Logger.printInfo(DateTime.Now, order.machine, message, toScreen);

            return false;
        }

        protected bool isLastADOperationSucceed(String operation, bool toScreen = true)
        {
            if (AD.LastResult == StateCodes.stcSuccess)
                return true;

            String mInfo = operation + ": " + getLastOperationMessage();

            Logger.printInfo(DateTime.Now, mInfo, toScreen);

            return false;
        }

        protected String getLastOperationMessage()
        {
            String message;
            if (AD.LastResultMsg != null)
                message = "State not success. " + AD.LastResultMsg;
            else
                message = "State not success. No message.";

            return message;
        }
    }
}
