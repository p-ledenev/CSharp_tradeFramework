using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace alfaMTT.Alfa
{
    class TerminalOrdersProcessor
    {
        public List<TerminalOrder> orders { get; set; }

        public TerminalOrdersProcessor(List<TerminalOrder> orders)
        {
            this.orders = orders;
        }

        protected bool submit()
        {
            bool anySubmission = false;
            foreach (TerminalOrder order in orders)
            {
                order.submit();
                if (order.isSubmissionSucceed())
                    anySubmission = true;
            }

            return anySubmission;
        }

        protected bool isAnyOrderExecutionSucceed()
        {
            return orders.Any(order => order.isExecutionSucceed());
        }

        protected void executeOposites()
        {
            foreach (TerminalOrder order in orders)
                foreach (TerminalOrder oposit in orders)
                    if (order.hasSameTicketAs(oposit) && order.hasOpositPositionWith(oposit) &&
                        order.isNewest() && oposit.isNewest() &&
                        order.isParenetExecutionSucceed() && oposit.isParenetExecutionSucceed())
                    {
                        order.executeAsOposite(oposit);
                    }
        }

        protected bool hasOrdersToExecute()
        {
            return orders.Any(order => order.allowExecution());
        }

        public void process()
        {
            Console.WriteLine("Try to execute Oposite Orders.");
            executeOposites();

            Console.WriteLine("Try to execute all other Orders.");
            while (hasOrdersToExecute())
            {
                if (submit())
                    Thread.Sleep(11 * 1000);

                orders.ForEach(order => order.loadStatus());

                Thread.Sleep(2 * 1000);
            }

            orders.ForEach(order => order.validateStatus());

            orders.ForEach(order => order.applayToMachine());

            orders.ForEach(order => order.logStatus());

            Thread.Sleep(3 * 1000);
        }

        public String printOrders()
        {
            String response = "";
            foreach (TerminalOrder order in orders)
                response += order.printOrder();

            return response;
        }
    }
}
