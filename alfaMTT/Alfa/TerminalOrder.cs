using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using alfaMTT.Model;
using core.Model;

namespace alfaMTT.Alfa
{
    internal enum OrderStatus
    {
        newest,
        executedPartly,
        submissionFailed,
        submissionSucceed,
        submissionBlocked,
        executionSucceed,
        statusNotObtained,
        deleted
    };

    class TerminalOrder
    {
        public static AlfaDirectGateway alfaDirectGateway { get; set; }
        public static String operationFile { get; set; }
        public static int maxCheckAttempts { get; set; }

        public TradeMachine machine { get; set; }
        public int orderNumber { get; set; }
        public OrderStatus status { get; set; }
        public Trade trade { get; set; }
        public int checkAttemptsCounter { get; set; }
        public TerminalOrder parent { get; set; }

        public TerminalOrder()
        {
            orderNumber = 0;
            status = OrderStatus.newest;
            trade = Trade.creatSample();

            machine = null;
            parent = null;

            checkAttemptsCounter = 1;
        }

        public TerminalOrder(Machine machine, Position.Direction direction, int volume)
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

            if (isParentSubmissionBlocked()) return;

            if (alfaDirectGateway.loadLastValue(this) == false)
            {
                status = OrderStatus.submissionBlocked;
                return;
            }

            if (trade.isBuy()) 
                trade.value += 0.002 * trade.value; // открытие
            
            if (trade.isSell())
                trade.value -= 0.002 * trade.value; // закрытие

            // если предыдущий не завершен
            if (!isParenetExecutionSucceed()) return;

            trade.volume = (trade.volume > 0) ? trade.volume : machine.parent.lot;

            if (trade.volume <= 0)
            {
                AlfaDirectGateway.printInfo(DateTime.Now, machine, "CreateOrder: Faild. Not enough money in Portfolio. Order volume less 0.");
                status = OrderStatus.tenderedBlock;

                return;
            }

            // подать ордер
            orderNumber = alfaDirectGateway.createOrder(this);

            status = (orderNumber > 0) ? OrderStatus.tenderedSuccess : OrderStatus.tenderedFail;
        }

        public bool check()
        {
            // если ордер не в работе
            if (status == OrderStatus.success) return true;

            if (orderNumber <= 0) return false;

            checkAttemptsCounter++;

            String adResult = alfaDirectGateway.loadResultFor(this);
            determStatusBy(adResult);

            return (status == OrderStatus.success);
        }

        public void apply()
        {
            machine.blockBy(status);

            if (isExecutionSucceed())
                machine.execute(this);

            if (!isExecutionSucceed() && orderNumber > 0)
            {
                bool successDrop = alfaDirectGateway.dropOrder(this, true);

                if (successDrop)
                {
                    status = OrderStatus.deleted;
                    machine.isBlocked = false;
                }
            }
        }

        public void determStatusBy(String adResult)
        {
            if (adResult == null)
            {
                status = OrderStatus.checkNotObtain;
                return;
            }

            String[] deals = adResult.Split(new String[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            double cValue = 0;
            int cVolume = 0;
            foreach (String strDeal in deals)
            {
                String[] deal = strDeal.Split(new char[] { '|' });

                cValue += Double.Parse(deal[0]) * Int32.Parse(deal[1]);
                cVolume += Int32.Parse(deal[1]);
            }

            if (trade.volume != cVolume)
            {
                String mInfo = "CheckOrder: State success. Order No " + orderNumber +
                    " not full complete. " + cVolume + " instead " + trade.volume + ".";
                AlfaDirectGateway.printInfo(DateTime.Now, machine, mInfo);

                status = OrderStatus.executePart;

                return;
            }

            trade.value = cValue / cVolume;
            trade.volume = cVolume;

            status = OrderStatus.success;
        }

        public void printStatusInfo()
        {
            String mInfo = null;
            if (status == OrderStatus.newest)
            {
                if (parent == null || parent.status == OrderStatus.success)
                    mInfo = "OperateStock: Can not operate stock. Not enough money.";
                else
                    mInfo = "OperateStock: Can not operate stock because of previose operation incomplitness.";

                AlfaDirectGateway.printInfo(DateTime.Now, machine, mInfo);
            }

            if (status == OrderStatus.executePart)
                mInfo = "OperateStock: Not full Volume for Order No " + orderNumber + ". This machine should be blocked.";

            if (status == OrderStatus.checkNotObtain)
                mInfo = "OperateStock: No status available for Order No " + orderNumber + ". This machine should be blocked.";

            if (status == OrderStatus.tenderedFail)
                mInfo = "OperateStock: Can not create order. This machine should be blocked.";

            if (status == OrderStatus.tenderedBlock)
                mInfo = "OperateStock: Can not create order. This machine continue working";

            if (status == OrderStatus.deleted)
                mInfo = "OperateStock: Order No " + orderNumber + " was deleted. This machine continue working";

            if (mInfo != null)
                AlfaDirectGateway.printInfo(DateTime.Now, machine, mInfo);

            StreamWriter file = null;
            try
            {
                file = new StreamWriter(OperateStock.path + operationFile, true);
            }
            catch (Exception e)
            {
                AlfaDirectGateway.printInfo(DateTime.Now, "pInfoADOrder: " + e.Message);
                Environment.Exit(0);
            }

            Trade trade = new Trade(this);
            file.WriteLine(trade.tradeString());
            file.Close();

            mInfo = "OperateStock: " + this.trade.mode + " " + this.trade.value + " " + this.trade.volume;
            AlfaDirectGateway.printInfo((this.trade.dt != DateTime.MinValue) ? this.trade.dt : DateTime.Now, machine, mInfo, (status == OrderStatus.success) ? true : false);
        }

        public void printOrder()
        {
            if (machine != null)
            {
                Console.WriteLine(machine.id + " " + machine.parent.ticket + " " +
                    machine.strategie.getName() + " " + machine.strategie.getParam() + " " + trade.mode);
            }
        }

        public bool allowExecute()
        {
            // если не исполнен, не удален, не неуспешно подан; если предшествующего нет или он исполнен; количество попыток проверки не превышено
            return (!isExecutionSucceed() && !isDeleted() && !isSubmissionFailed() && !isSubmissionBlocked() &&
                isParenetExecutionSucceed() && !isExeededMaxCheckAttemts());
        }

        public bool hasSameTicketWith(TerminalOrder order)
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
            return status == OrderStatus.submissionFailed;
        }

        public bool isSubmissionBlocked()
        {
            return status == OrderStatus.submissionBlocked;
        }

        public bool isExecutionSucceed()
        {
            return status == OrderStatus.executionSucceed;
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

        public bool isExeededMaxCheckAttemts()
        {
            return checkAttemptsCounter > maxCheckAttempts;
        }

        public void opositeOrder(TerminalOrder order)
        {
            this.trade.dt = DateTime.Now;
            order.trade.dt = this.trade.dt;

            Console.WriteLine("Oposite Orders: ");
            this.printOrder();
            order.printOrder();

            // получить цену последней сделки
            if (!alfaDirectGateway.loadLastValue(this))
            {
                alfaDirectGateway.loadLastValue(order);

                status = OrderStatus.tenderedBlock;
                order.status = OrderStatus.tenderedBlock;

                return;
            }

            trade.volume = (trade.volume > 0) ? trade.volume : machine.parent.lot;

            order.trade.value = trade.value;
            order.trade.volume = (order.trade.volume > 0) ? order.trade.volume : order.machine.parent.lot;

            // взаимозасчитывать только при совпадении объемов
            Console.WriteLine("Check oposite orders volume.");
            if (trade.volume == order.trade.volume)
            {
                this.status = OrderStatus.success;
                order.status = OrderStatus.success;

                Console.WriteLine("Opposite execution success");
            }
            else
            {
                // сбросить цену
                order.set(order.machine, order.trade.mode, order.machine.heap.volume);
                this.set(this.machine, this.trade.mode, this.machine.heap.volume);

                Console.WriteLine("Opposite execution fail. Volumes not equal");
            }
        }

        public static void opositeOrders(List<TerminalOrder> cOrds)
        {
            foreach (TerminalOrder order in cOrds)
                foreach (TerminalOrder oposit in cOrds)
                    if (order.hasSameTicketWith(oposit) && order.hasOpositPositionWith(oposit) &&
                        order.isNewest() && oposit.isNewest() &&
                        order.isParenetExecutionSucceed() && oposit.isParenetExecutionSucceed())
                    {
                        order.opositeOrder(oposit);
                    }
        }

        public static bool hasOrdersToExecute(List<TerminalOrder> orders)
        {
            foreach (TerminalOrder order in orders)
                if (order.allowExecute())
                    return true;

            return false;
        }

        public static bool executeOrders(List<TerminalOrder> orders)
        {
            bool successExecute = false;

            // учесть взаимопротивоположные заявки
            Console.WriteLine("Try do execute Oposite Orders.");
            opositeOrders(orders);

            Console.WriteLine("Try do execute all other Orders.");
            while (hasOrdersToExecute(orders))
            {
                // подать заявки
                bool m = createOrders(orders);

                if (m) Thread.Sleep(11 * 1000);

                // проверить исполнение ордеров 
                successExecute = checkOrders(orders);

                Thread.Sleep(2 * 1000);
            }

            // применить ордера к портфелям
            applyOrders(orders);

            // записать р-ты операций
            printOrdersInfo(orders);

            Thread.Sleep(3 * 1000);

            return successExecute;
        }

        public String getTicket()
        {
            return machine.getTicket();
        }

        public String getAccount()
        {
            return machine.getAccount();
        }

        public String getDirection()
        {
            if (trade.position.isBuy())
                return "Buy";

            if (trade.position.isSell())
                return "Sell";

            return "None";
        }

        public int getVolume()
        {
            return trade.getVolume();
        }

        public double getTradeValue()
        {
            return trade.getTradeValue();
        }

        public static void printOrdersInfo(List<TerminalOrder> orders)
        {
            Console.WriteLine("Try to write Orders results.");
            foreach (TerminalOrder order in orders)
                order.printStatusInfo();
        }

        public static bool createOrders(List<TerminalOrder> orders)
        {
            bool m = false;
            foreach (TerminalOrder order in orders)
            {
                order.submit();
                if (order.status == OrderStatus.tenderedSuccess) m = true;
            }

            return m;
        }

        public static bool checkOrders(List<TerminalOrder> orders)
        {
            bool m = false;
            foreach (TerminalOrder order in orders)
                if (order.check()) m = true;

            return m;
        }

        public static void applyOrders(List<TerminalOrder> orders)
        {
            foreach (TerminalOrder order in orders)
                order.apply();
        }
    }
}

