using System;
using System.Collections.Generic;
using System.Linq;
using alfaMTT.Alfa;
using alfaMTT.DataSources;
using alfaMTT.Model;
using alfaMTT.Settings;

namespace alfaMTT
{
    class AlfaMTT
    {
        public static TerminalGateway initADS()
        {
            Console.Write("login: ");
            String login = Console.ReadLine();

            Console.Write("password: ");
            String password = readPassword();

            TerminalGateway TerminalGateway = new TerminalGateway(login, password);

            return TerminalGateway;
        }

        public static void Main(string[] args)
        {
            TradeLogger.path = Environment.CurrentDirectory + "\\";

            TradesProcessor processor = new TradesProcessor
            {
                portfolios = PortfolioInitializator.initPortfolios(),
                terminal = initADS()
            };

            TerminalOrder.terminal = processor.terminal;
            TerminalOrder.maxCheckAttempts = 10;

            processor.printPortfolios();

            Console.WriteLine("Start trade\n");
            processor.trade();
        }

        public static String readPassword()
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr;

            while ((chr = Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push(chr);
                    Console.Write("*");
                }
            }

            Console.WriteLine();

            return new String(pass.Reverse().ToArray());
        }
    }
}
