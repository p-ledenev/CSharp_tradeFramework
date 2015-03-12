using System;
using System.IO;
using alfaMTT.Alfa;
using alfaMTT.Model;
using core.Model;

namespace alfaMTT.DataSources
{
    // operation file format
    // date[0]|id[1]|ticket[2]|OperateStock:[3]|direction[4]|value[5]|volume[6]|mode[7]|success[8]

    class TradeLogger
    {
        public static String path { get; set; }
        public static String operationFile = "operations.log";

        public void initLastTradesFor(TradeMachine machine)
        {
            StreamReader file = null;

            try
            {
                file = new StreamReader(path + operationFile);
            }
            catch (Exception e)
            {
                Logger.printInfo(DateTime.Now, machine, "initLastTrades: " + e.Message);
                throw;
            }

            String line;
            while ((line = file.ReadLine()) != null)
            {
                String[] operation = line.Split('|');

                if (!operation[1].Equals(machine.id) || !OrderStatus.executionSucceed.ToString().Equals(operation[8]))
                    continue;

                machine.lastTrade = readTradeFrom(operation);

                if (operation[7].Equals(Trade.Mode.CloseAndOpen.ToString()))
                    machine.lastOpenPositionTrade = machine.lastTrade;
            }

            file.Close();
        }

        public void writeOrder(TerminalOrder order)
        {
            Trade trade = order.trade;
            TradeMachine machine = order.machine;

            StreamWriter file = null;

            try
            {
                file = new StreamWriter(path + operationFile, true);
            }
            catch (Exception e)
            {
                Logger.printInfo(DateTime.Now, "writeTrade: " + e.Message);
                throw;
            }

            file.WriteLine(trade.getDate().ToString("dd.MM.yyyy HH:mm:ss") + "|" + machine.id + "|" + machine.getTicket() + "|Operatation:" +
                "|" + trade.getDirection() + "|" + trade.getTradeValue() + "|" + trade.getVolume() + "|" + order.status);
        }

        protected Trade readTradeFrom(String[] operation)
        {
            Trade.Mode mode;
            Enum.TryParse(operation[7], out mode);

            Position.Direction direction;
            Enum.TryParse(operation[4], out direction);

            Trade trade = new Trade
            {
                date = DateTime.Parse(operation[0]),
                mode = mode,
                position = new Position(Double.Parse(operation[5]), direction, Int32.Parse(operation[6]))
            };

            return trade;
        }
    }
}
