using System;
using System.Collections.Generic;
using System.IO;
using alfaMTT.Model;

namespace alfaMTT.Settings
{
    class Ini
    {
        public static String iniFile { get; set; }

        public String id { get; set; }
        public String account { get; set; }
        public String ticket { get; set; }
        public String strategie { get; set; }

        public int lot { get; set; }
        public double comissionIntraday { get; set; }
        public double comissionCommon { get; set; }
        public double sieveParam { get; set; }

        public double moneyBegin { get; set; }
        public double moneyValue { get; set; }
        public double moneyMax { get; set; }

        public bool isTrade { get; set; }
        public bool isBlocked { get; set; }

        public String mode { get; set; }
        public double value { get; set; }
        public int volume { get; set; }

        public int param { get; set; }

        public static String approximationStrategie = "App";
        public static String deviationStrategie = "Dev";

        public Ini()
        {
            id = null;
            account = null;
            ticket = null;
            strategie = null;

            comissionCommon = comissionIntraday = sieveParam = 0;
            moneyBegin = moneyMax = moneyValue = 0;

            volume = 0;
            value = 0;
            lot = 0;
            isTrade = true;
            isBlocked = false;
            mode = Stock.none;
        }

        public void setIni(String id, String account, String ticket, String comissionCommon, String comissionIntraday,
                           String sieveParam, String moneyBegin, String moneyMax, String lot, String isTrade, String isBlocked,
                           String param, String strategie, String moneyValue, String mode, String value, String volume)
        {
            double parseDouble = 0;
            int parseInt = 0;

            this.id = id;
            this.account = account;
            this.ticket = ticket;

            this.moneyBegin = (Double.TryParse(moneyBegin, out parseDouble)) ? parseDouble : 0;
            this.moneyMax = (Double.TryParse(moneyMax, out parseDouble)) ? parseDouble : 0;
            this.moneyValue = (Double.TryParse(moneyValue, out parseDouble)) ? parseDouble : 0;

            this.param = (Int32.TryParse(param, out parseInt)) ? parseInt : 0;

            this.volume = (Int32.TryParse(volume, out parseInt)) ? parseInt : 0;
            this.value = (Double.TryParse(value, out parseDouble)) ? parseDouble : 0;
            this.mode = mode;

            this.isTrade = isTrade.Equals("1");
            this.isBlocked = isBlocked.Equals("1");
            this.lot = (Int32.TryParse(lot, out parseInt)) ? parseInt : 0;

            this.comissionIntraday = (Double.TryParse(comissionIntraday, out parseDouble)) ? parseDouble : 0;
            this.comissionCommon = (Double.TryParse(comissionCommon, out parseDouble)) ? parseDouble : 0;
            this.sieveParam = (Double.TryParse(sieveParam, out parseDouble)) ? parseDouble : 0;

            this.strategie = strategie;
        }

        public static List<Ini> readIni()
        {
            String s = OperateStock.path + "dat\\" + iniFile;
            StreamReader file = null;

            try
            {
                file = new StreamReader(s);
            }
            catch (Exception e)
            {
                TerminalGateway.printInfo(DateTime.Now, "readIni: " + e.Message);
                Environment.Exit(0);
            }

            List<Ini> dat = new List<Ini>();

            String line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Equals("\n") || line.Contains("\t\t") || line.Equals(""))
                    continue;

                // id[0], account[1], ticket[2], comissionCommon[3], comissionIntraday[4], sieveParam[5], moneyBegin[6], moneyMax[7], 
                // lot[8], isTrade[9], isBlocked[10], param[11], strategie[12], moneyValue[13], mode[14], value[15], volume[16]
                String[] inis = line.Split(new char[] { '\t', '\n' });

                Ini ini = new Ini();
                ini.setIni(inis[0], inis[1], inis[2], inis[3], inis[4], inis[5], inis[6], inis[7], inis[8],
                           inis[9], inis[10], inis[11], inis[12], inis[13], inis[14], inis[15], inis[16]);

                dat.Add(ini);
            }

            file.Close();

            return dat;
        }

        public static Ini getGIniBy(String id, List<Ini> dat)
        {
            foreach (Ini ini in dat)
                if (ini.id.Equals(id))
                    return ini;

            return null;
        }

        public static void writeIni(List<GPortfolio> gpfts, bool backUp)
        {
            StreamWriter file = null;

            try
            {
                file = new StreamWriter(OperateStock.path + "dat\\" + ((backUp) ? iniFile + ".back" : iniFile));
            }
            catch (Exception e)
            {
                TerminalGateway.printInfo(DateTime.Now, "writeIni: " + e.Message);
                Environment.Exit(0);
            }

            file.WriteLine("id\taccount\tticket\tcomissionCommon\tcomissionIntraday\tsieveParam\tmoneyBegin\tmoneyMax\tlot\tisTrade\tisBlocked\t" +
                "param\tstrategie\tmoneyValue\tmode\tvalue\tvolume");
            file.WriteLine("");

            foreach (GPortfolio gpft in gpfts)
            {
                file.WriteLine(gpft.id + "\t" + gpft.account + "\t" + gpft.ticket + "\t" + gpft.comissionCommon + "\t" +
                               gpft.comissionIntraday + "\t" + gpft.sifter.param + "\t" + gpft.moneyBegin + "\t" + gpft.moneyMax + "\t" + gpft.lot +
                               "\t" + (gpft.isTrade ? "1" : "0") + "\t.\t.\t" + gpft.strategie + "\t" + gpft.countStock() + "\t.\t.\t.");

                foreach (Portfolio pft in gpft.pfts)
                {
                    file.WriteLine(pft.id + "\t.\t.\t.\t.\t.\t" + pft.moneyBegin + "\t" + pft.moneyMax + "\t.\t" + (pft.isTrade ? "1" : "0") +
                                   "\t" + (pft.isBlocked ? "1" : "0") + "\t" + pft.strategie.getParam() + "\t.\t" + pft.countStock() + "\t" +
                                   pft.heap.mode + "\t" + pft.heap.value + "\t" + pft.heap.volume + "\t");
                }
                file.WriteLine("");
            }

            file.Close();
        }

        public static List<GPortfolio> initPortfolio()
        {
            List<Ini> dat = Ini.readIni();
            dat.RemoveAt(0);

            List<GPortfolio> gpfts = new List<GPortfolio>();

            GPortfolio gpft = null;
            foreach (Ini ini in dat)
            {
                if (Char.IsDigit(ini.account[0]))
                {
                    gpft = new GPortfolio(ini);
                    gpfts.Add(gpft);
                }
                else
                {
                    gpft.pfts.Add(new Portfolio(ini, gpft));
                }
            }

            return gpfts;
        }
    }
}

