using System;
using System.Collections.Generic;
using System.Threading;
using ADLite;
using core.Model;

namespace alfaMTT.Alfa
{
    class TerminalGateway
    {
        public static TimeSpan tradeFrom =  new TimeSpan(10, 0, 0);
        public static TimeSpan tradeTo = new TimeSpan(23, 45, 0);
        
        public AlfaDirect connector { get; set; }
        public String login { get; set; }
        public String password { get; set; }

        public TerminalGateway(String login, String password)
        {
            this.login = login;
            this.password = password;

            setConnector();
        }

        private void setConnector()
        {
            connector = new AlfaDirect
            {
                UserName = login,
                Password = password,
                Connected = true
            };
        }

        public void checkConnect()
        {
            bool isLost = false;
            if (connector == null || !connector.Connected)
            {
                isLost = true;
                Logger.printInfo(DateTime.Now, "CheckConnect: Connection to AlfaDirect is lost");
            }

            while (connector == null || !connector.Connected)
            {
                Thread.Sleep(10 * 1000);
                setConnector();

                Thread.Sleep(10 * 1000);
            }

            if (isLost)
                Logger.printInfo(DateTime.Now, "CheckConnect: Connection to AlfaDirect is set up");
        }

        public void dropOrder(TerminalOrder order, bool toScreen = true)
        {
            checkConnect();

            connector.DropOrder(order.orderNumber, null, null, null, null, null, 3);

            if (!isLastADOperationSucceed("DropOrder", order, false))
                throw new TerminalGatewayFailure();

            String resultMessage = connector.LastResultMsg;

            Logger.printInfo(DateTime.Now, order.machine, "DropOrder: State success. " + resultMessage + (resultMessage == null ? " No result recieved." : ""), toScreen);

            if (resultMessage == null || !resultMessage.Contains("удалена"))
                throw new TerminalGatewayFailure();
        }

        public double loadLastValue(TerminalOrder order)
        {
            checkConnect();

            String lastValue = connector.GetLocalDBData("fin_info", "last_price, status", "p_code in (\"" + order.getTicket() + "\")");

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

        public String obtainStatusFor(TerminalOrder order)
        {
            checkConnect();

            String result = connector.GetLocalDBData("trades", "price, qty", "ord_no = " + order.orderNumber);

            if (isLastADOperationSucceed("CheckOrder", order))
                throw new TerminalGatewayFailure();

            Logger.printInfo(DateTime.Now, order.machine, "CheckOrder: State success. " + connector.LastResultMsg + (result == null ? " No result recieved." : ""));

            if (result == null)
                throw new OrderStatusObtainingFailure();

            return result;
        }

        public int submitOrder(TerminalOrder order)
        {
            checkConnect();

            int orderNo = connector.CreateLimitOrder(order.getAccount(), "FORTS", order.getTicket(),
                                  DateTime.Now.AddDays(1), "", "RUR", order.getTerminalDirection(), order.getVolume(),
                                  order.getTradeValue(), null, null, null, null, null, null, null, null, null,
                                  null, null, null, null, null, null, null, 11);

            if (isLastADOperationSucceed("CreateOrder", order))
                throw new TerminalGatewayFailure();

            Logger.printInfo(DateTime.Now, order.machine, "CreateOrder: State success. " + connector.LastResultMsg +
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

            String result = connector.GetArchiveFinInfo("FORTS", ticket, 0, dateFrom, dateTo, 3, 20);

            Console.WriteLine(ticket + " (" + dateFrom + "-" + dateTo + ") read data " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

            if (isLastADOperationSucceed("LoadCandles") || result == null)
                return new List<Candle>();

            return parseFinInstrumentData(result);
        }

        public int loadTicketVolume(String account, String ticket)
        {
            checkConnect();

            String result = connector.GetLocalDBData("balance", "real_rest", "acc_code in (\"" + account + "\") and p_code in (\"" + ticket + "\")");

            if (isLastADOperationSucceed("loadTicketVolume"))
                throw new TerminalGatewayFailure();

            if (result == null)
            {
                Logger.printInfo(DateTime.Now, "loadTicketVolume: " + connector.LastResultMsg + " " + (connector.LastResultMsg == null ? "No message recieved" : ""));
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
            if (connector.LastResult == StateCodes.stcSuccess)
                return true;

            String message = operation + ": " + getLastOperationMessage();

            Logger.printInfo(DateTime.Now, order.machine, message, toScreen);

            return false;
        }

        protected bool isLastADOperationSucceed(String operation, bool toScreen = true)
        {
            if (connector.LastResult == StateCodes.stcSuccess)
                return true;

            String mInfo = operation + ": " + getLastOperationMessage();

            Logger.printInfo(DateTime.Now, mInfo, toScreen);

            return false;
        }

        protected String getLastOperationMessage()
        {
            String message;
            if (connector.LastResultMsg != null)
                message = "State not success. " + connector.LastResultMsg;
            else
                message = "State not success. No message.";

            return message;
        }
    }
}
