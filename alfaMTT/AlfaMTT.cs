using System;
using System.Collections.Generic;
using System.Linq;
using alfaMTT.Alfa;
using alfaMTT.Model;
using alfaMTT.Settings;

namespace alfaMTT
{
    class AlfaMTT
    {
        public static AlfaDirectGateway initADS()
        {
            Console.Write("login: ");
            String login = Console.ReadLine();

            Console.Write("password: ");
            String password = readPassword();

            AlfaDirectGateway alfaDirectGateway = new AlfaDirectGateway(login, password);

            return alfaDirectGateway;
        }

        public static void Main(string[] args)
        {
            OperateStock.path = Environment.CurrentDirectory + "\\";

            Logger.infoFile = "messages.log";
            TerminalOrder.operationFile = "operations.log";
            Ini.iniFile = "init.dat";
            GPortfolio.traceFile = "trace.prn";

            AlfaDirectGateway.tradeFrom = new TimeSpan(10, 0, 0);
            AlfaDirectGateway.tradeTo = new TimeSpan(23, 45, 0);

            OperateStock operate = new OperateStock();
            operate.gPortfolios = Ini.initPortfolio();
            operate.alfaDirectGateway = initADS();

            TerminalOrder.alfaDirectGateway = operate.alfaDirectGateway;
            TerminalOrder.maxCheckAttempts = 10;

            operate.printPortfolios();

            Console.WriteLine("Start trade\n");
            operate.trade();
        }

        public static String readPassword()
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write("*");
                }
            }

            System.Console.WriteLine();

            return new String(pass.Reverse().ToArray());
        }
    }
}
