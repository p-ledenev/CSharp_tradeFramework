using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using alfaMTT.DataSources;
using alfaMTT.Model;
using core.Model;

namespace alfaMTT.Alfa
{
    internal enum OrderStatus
    {
        newest,
        executedPartly,
        submissionStatusNotObtained,
        submissionSucceed,
        submissionBlocked,
        executionSucceed,
        executionStatusNotObtained,
        deleted
    };

    class TerminalOrder
    {
        public static TerminalGateway terminal { get; set; }
        public static int maxCheckAttempts { get; set; }

        public TradeMachine machine { get; set; }
        public int orderNumber { get; set; }
        public OrderStatus status { get; set; }
        public Trade trade { get; set; }
        public int loadStatusAttemptsCounter { get; set; }
        public TerminalOrder parent { get; set; }

        public static TerminalOrder withParent(TerminalOrder parent)
        {
            TerminalOrder order = new TerminalOrder(parent.machine, parent.getDirection(), parent.getVolume())
            {
                parent = parent
            };

            return order;
        }

        public TerminalOrder()
        {
            orderNumber = 0;
            status = OrderStatus.newest;
            trade = Trade.createSample();

            machine = null;
            parent = null;

            loadStatusAttemptsCounter = 1;
        }

        public TerminalOrder(TradeMachine machine, Position.Direction direction, int volume)
            : this()
        {
            this.machine = machine;
            trade.setPosition(direction, volume);
        }

        public void submit()
        {
            if (status != OrderStatus.newest)
                return;

            trade.date = DateTime.Now;

            if (isParentSubmissionBlocked())
                return;

            try
            {
                double value = terminal.loadLastValue(this);
                int sign = trade.isBuy() ? 1 : -1;
                trade.setTradeValue((1 + sign * 0.002) * value);
            }
            catch (TerminalGatewayFailure)
            {
                status = OrderStatus.submissionBlocked;
                return;
            }

            if (!isParenetExecutionSucceed())
                return;

            try
            {
                orderNumber = terminal.submitOrder(this);
                status = OrderStatus.submissionSucceed;
            }
            catch (OrderSubmissionFailure)
            {
                status = OrderStatus.submissionStatusNotObtained;
            }
            catch (TerminalGatewayFailure)
            {
            }
        }

        public void loadStatus()
        {
            loadStatusAttemptsCounter++;

            try
            {
                String result = terminal.obtainStatusFor(this);

                String[] deals = result.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                double value = 0;
                int volume = 0;
                foreach (String strDeal in deals)
                {
                    String[] deal = strDeal.Split('|');

                    value += Double.Parse(deal[0]) * Int32.Parse(deal[1]);
                    volume += Int32.Parse(deal[1]);
                }

                if (trade.getVolume() != volume)
                {
                    String mInfo = "CheckOrder: State success. Order No " + orderNumber +
                        " not full complete. " + volume + " instead " + trade.getVolume() + ".";
                    Logger.printInfo(DateTime.Now, machine, mInfo);

                    status = OrderStatus.executedPartly;

                    return;
                }

                trade.setTradeValue(value / volume);
                trade.setVolume(volume);

                status = OrderStatus.executionSucceed;
            }
            catch (OrderStatusObtainingFailure)
            {
                status = OrderStatus.executionStatusNotObtained;
            }
            catch (TerminalGatewayFailure)
            {
            }
        }

        public void validateStatus()
        {
            if (shouldBeBlocked())
                machine.isBlocked = true;

            if (!shouldBeDeleted())
                return;

            try
            {
                terminal.dropOrder(this);

                status = OrderStatus.deleted;
                machine.isBlocked = false;
            }
            catch (TerminalGatewayFailure)
            {
            }
        }

        public void logStatus()
        {
            String message = null;
            if (status == OrderStatus.newest)
                message = isParenetExecutionSucceed()
                    ? "OperateStock: Can not execute order. Not enough money."
                    : "OperateStock: Can not execute order. Parent order not success.";

            if (status == OrderStatus.executedPartly)
                message = "OperateStock: Order No " + orderNumber + " executed partly. This machine should be blocked.";

            if (status == OrderStatus.executionStatusNotObtained)
                message = "OperateStock: Order No " + orderNumber + " status not obtained. This machine should be blocked.";

            if (status == OrderStatus.submissionStatusNotObtained)
                message = "OperateStock: Can not create order. Order Id was not obtained during submission. This machine should be blocked.";

            if (status == OrderStatus.submissionBlocked)
                message = "OperateStock: Can not create order. Order submission blocked. This machine continue working";

            if (status == OrderStatus.deleted)
                message = "OperateStock: Order No " + orderNumber + " was deleted. This machine continue working";

            if (status == OrderStatus.executionSucceed)
                message = "OperateStock: " + printOrder() + " succeed";

            if (message != null)
                Logger.printInfo(DateTime.Now, machine, message);

            TradeLogger logger = new TradeLogger();
            logger.writeOrder(this);
        }

        public String printOrder()
        {
            return machine.id + " " + machine.getTicket() + " " + machine.getDecisionStrategyName() + " " +
                machine.getDepth() + " " + trade.getDirection() + " " + trade.getTradeValue() + " " + trade.getVolume();
        }

        public bool allowExecution()
        {
            if (!isParenetExecutionSucceed())
                return false;

            if (shouldBeBlocked() || isExecutionSucceed())
                return false;

            if (isMaxCheckAttemtsExeeded())
                return false;

            return true;
        }

        public bool hasSameTicketAs(TerminalOrder order)
        {
            return (order.machine.getTicket().Equals(machine.getTicket()));
        }

        public bool hasOpositPositionWith(TerminalOrder order)
        {
            return (!trade.isNone() && !trade.isSameDirectionAs(order.trade));
        }

        public bool isNewest()
        {
            return status == OrderStatus.newest;
        }

        public bool isSubmissionFailed()
        {
            return status == OrderStatus.submissionStatusNotObtained;
        }

        public bool isSubmissionBlocked()
        {
            return status == OrderStatus.submissionBlocked;
        }

        public bool isSubmissionSucceed()
        {
            return status == OrderStatus.submissionSucceed;
        }

        public bool isExecutionSucceed()
        {
            return status == OrderStatus.executionSucceed;
        }

        public bool shouldBeDeleted()
        {
            OrderStatus[] faildStatuses = { OrderStatus.executedPartly, OrderStatus.executionStatusNotObtained };

            if (Array.IndexOf(faildStatuses, status) > -1)
                return true;

            return false;
        }

        public bool shouldBeBlocked()
        {
            return (shouldBeDeleted() || OrderStatus.submissionStatusNotObtained.Equals(status));
        }

        public bool isDeleted()
        {
            return status == OrderStatus.deleted;
        }

        public bool isParenetExecutionSucceed()
        {
            return (parent == null || parent.status == OrderStatus.executionSucceed);
        }

        public bool isParentSubmissionBlocked()
        {
            return (parent != null && parent.status == OrderStatus.submissionBlocked);
        }

        public bool isMaxCheckAttemtsExeeded()
        {
            return loadStatusAttemptsCounter > maxCheckAttempts;
        }

        public void executeAsOposite(TerminalOrder order)
        {
            trade.date = DateTime.Now;
            order.setDate(trade.date);

            Console.WriteLine("Oposite Orders: ");
            Console.WriteLine(printOrder());
            Console.WriteLine(order.printOrder());

            Console.WriteLine("Check oposite orders volume.");
            if (!hasSameVolume(order))
            {
                Console.WriteLine("Opposite execution fail. Volumes not equal");
                return;
            }

            double value;
            try
            {
                value = terminal.loadLastValue(this);
            }
            catch (TerminalGatewayFailure e)
            {
                status = OrderStatus.submissionBlocked;
                order.status = OrderStatus.submissionBlocked;

                Console.WriteLine("Opposite execution blocked");
                return;
            }

            setTradeValue(value);
            order.setTradeValue(value);

            status = OrderStatus.executionSucceed;
            order.status = OrderStatus.executionSucceed;

            Console.WriteLine("Opposite execution succeed");
        }

        public String getTicket()
        {
            return machine.getTicket();
        }

        public String getAccount()
        {
            return machine.getAccount();
        }

        public Position.Direction getDirection()
        {
            return trade.getDirection();
        }

        public String getTerminalDirection()
        {
            if (trade.position.isBuy())
                return "Buy";

            if (trade.position.isSell())
                return "Sell";

            return "None";
        }

        public bool hasSameVolume(TerminalOrder order)
        {
            return getVolume() == order.getVolume();
        }

        public int getVolume()
        {
            return trade.getVolume();
        }

        public Trade.Mode getMode()
        {
            return trade.mode;
        }

        public void setVolume(int volume)
        {
            trade.setVolume(volume);
        }

        public double getTradeValue()
        {
            return trade.getTradeValue();
        }

        public void setTradeValue(double value)
        {
            trade.setTradeValue(value);
        }

        public void setDate(DateTime date)
        {
            trade.date = date;
        }

        public void setMode(Trade.Mode mode)
        {
            trade.mode = mode;
        }

        public bool isCloseAndOpenPosition()
        {
            return trade.isCloseAndOpenPosition();
        }

        public DateTime getDate()
        {
            return trade.getDate();
        }

        public void applayToMachine()
        {
            machine.applyTerminalOrder(this);
        }
    }
}

