using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using alfaMTT.Alfa;
using alfaMTT.DataSources;
using alfaMTT.Model;
using core.CommissionStrategies;
using core.Factories;
using core.Model;
using core.SiftCanldesStrategies;

namespace alfaMTT.Settings
{
    class PortfolioInitializator
    {
        public static String iniFile = "init.dat";
        public static String path = "dat";

        public String id { get; set; }
        public String account { get; set; }
        public String ticket { get; set; }
        public String strategy { get; set; }

        public int lot { get; set; }
        public double commission { get; set; }
        public double sieveParam { get; set; }

        public double initialMoney { get; set; }
        public double currentMoney { get; set; }
        public double maxMoney { get; set; }

        public bool isTrade { get; set; }
        public bool isBlocked { get; set; }

        public Position.Direction direction { get; set; }
        public double value { get; set; }
        public int volume { get; set; }
        public int depth { get; set; }

        public PortfolioInitializator()
        {
            id = null;
            account = null;
            ticket = null;
            strategy = null;

            commission = sieveParam = 0;
            initialMoney = maxMoney = currentMoney = 0;

            volume = 0;
            value = 0;
            lot = 0;
            isTrade = true;
            isBlocked = false;
            direction = Position.Direction.None;
        }

        public PortfolioInitializator(String id, String account, String ticket, String commission, String sieveParam, String initialMoney, String maxMoney,
            String lot, String isTrade, String isBlocked, String depth, String strategy, String currentMoney, String direction, String value, String volume)
        {
            double parseDouble;
            int parseInt;
            Position.Direction parseDirection;

            this.id = id;
            this.account = account;
            this.ticket = ticket;

            this.initialMoney = Double.TryParse(initialMoney, out parseDouble) ? parseDouble : 0;
            this.maxMoney = Double.TryParse(maxMoney, out parseDouble) ? parseDouble : 0;
            this.currentMoney = Double.TryParse(currentMoney, out parseDouble) ? parseDouble : 0;

            this.depth = Int32.TryParse(depth, out parseInt) ? parseInt : 0;

            this.volume = Int32.TryParse(volume, out parseInt) ? parseInt : 0;
            this.value = Double.TryParse(value, out parseDouble) ? parseDouble : 0;

            this.direction = Enum.TryParse(direction, out parseDirection) ? parseDirection : Position.Direction.None;

            this.isTrade = isTrade.Equals("1");
            this.isBlocked = isBlocked.Equals("1");
            this.lot = (Int32.TryParse(lot, out parseInt)) ? parseInt : 0;

            this.commission = (Double.TryParse(commission, out parseDouble)) ? parseDouble : 0;
            this.sieveParam = (Double.TryParse(sieveParam, out parseDouble)) ? parseDouble : 0;

            this.strategy = strategy;
        }

        public bool hasAccount()
        {
            return account != null && Char.IsDigit(account[0]);
        }

        public TradePortfolio createPortfolio()
        {
            CommissionStrategy commissionStrategy = CommissionStrategyFactory.createConstantCommissionStrategy(commission);
            SiftCandlesStrategy siftStrategy = SiftCandlesStrategyFactory.createSiftStrategie(sieveParam);

            TradePortfolio portfolio = new TradePortfolio(account, commissionStrategy, siftStrategy)
            {
                id = id,
                account = account,
                lot = lot,
                ticket = ticket,
                title = strategy,
                maxMoney = maxMoney
            };

            return portfolio;
        }

        public Machine createMachineWith(Portfolio portfolio)
        {
            TradeMachine machine = new TradeMachine(strategy, currentMoney, depth, portfolio)
            {
                id = id,
                isBlocked = isBlocked,
                isTrade = isTrade
            };

            machine.setPosition(direction, value, volume);

            TradeLogger logger = new TradeLogger();
            logger.initLastTradesFor(machine);

            return machine;
        }

        public static List<PortfolioInitializator> readIni()
        {
            String s = TradeLogger.path + path + "\\" + iniFile;
            StreamReader file = null;

            try
            {
                file = new StreamReader(s);
            }
            catch (Exception e)
            {
                Logger.printInfo(DateTime.Now, "readIni: " + e.Message);
                Environment.Exit(0);
            }

            List<PortfolioInitializator> inis = new List<PortfolioInitializator>();

            String line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Equals("\n") || line.Contains("\t\t") || line.Equals(""))
                    continue;

                // id[0], account[1], ticket[2], comissionCommon[3], sieveParam[4], initialMoney[5], maxMoney[6], 
                // lot[7], isTrade[8], isBlocked[9], param[10], strategy[11], currentMoney[12], direction[13], value[14], volume[15]

                String[] array = line.Split('\t', '\n');
                PortfolioInitializator ini = new PortfolioInitializator(array[0], array[1], array[2], array[3], array[4], array[5],
                    array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13], array[14], array[15]);

                inis.Add(ini);
            }

            file.Close();

            return inis;
        }

        public static PortfolioInitializator getGIniBy(String id, List<PortfolioInitializator> inis)
        {
            return inis.FirstOrDefault(ini => ini.id.Equals(id));
        }

        public static void writeIni(List<TradePortfolio> portfolios, bool backUp)
        {
            StreamWriter file = null;

            try
            {
                file = new StreamWriter(TradeLogger.path + path + "\\" + ((backUp) ? iniFile + ".back" : iniFile));
            }
            catch (Exception e)
            {
                Logger.printInfo(DateTime.Now, "writeIni: " + e.Message);
                Environment.Exit(0);
            }

            file.WriteLine("id\taccount\tticket\tcommission\tsieveParam\tinitialMoney\tmaxMoney\tlot\tisTrade\tisBlocked\t" +
                "depth\tstrategy\tcurrentMoney\tdirection\tvalue\tvolume");
            file.WriteLine("");

            foreach (TradePortfolio portfolio in portfolios)
            {
                file.WriteLine(portfolio.id + "\t" + portfolio.account + "\t" + portfolio.ticket + "\t" + portfolio.calculateCommission() + "\t" +
                               portfolio.getSieveParam() + "\t" + portfolio.calculateInitialMoney() + "\t" + portfolio.maxMoney + "\t" + portfolio.lot +
                               "\t.\t.\t.\t" + portfolio.getDescisionStrategyName() + "\t" + portfolio.calculateCurrentMoney() + "\t.\t.\t.");

                foreach (TradeMachine machine in portfolio.machines)
                {
                    file.WriteLine(machine.id + "\t.\t.\t.\t.\t" + machine.initialMoney + "\t" + machine.maxMoney + "\t.\t" + (machine.isTrade ? "1" : "0") +
                                   "\t" + (machine.isBlocked ? "1" : "0") + "\t" + machine.depth + "\t.\t" + machine.computeCurrentMoney() + "\t" +
                                   machine.getPositionDirection() + "\t" + machine.getPositionValue() + "\t" + machine.getPositionVolume() + "\t");
                }
                file.WriteLine("");
            }

            file.Close();
        }

        public static List<TradePortfolio> initPortfolios()
        {
            List<PortfolioInitializator> inis = readIni();
            inis.RemoveAt(0);

            List<TradePortfolio> portfolios = new List<TradePortfolio>();

            TradePortfolio portfolio = null;
            foreach (PortfolioInitializator ini in inis)
            {
                if (ini.hasAccount())
                {
                    portfolio = ini.createPortfolio();
                    portfolios.Add(portfolio);
                }
                else
                {
                    portfolio.addMachine(ini.createMachineWith(portfolio));
                }
            }

            return portfolios;
        }
    }
}

