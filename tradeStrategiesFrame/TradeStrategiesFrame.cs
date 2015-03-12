#region

using System;
using System.Collections.Generic;
using System.IO;
using core.CommissionStrategies;
using core.Factories;
using core.Model;
using core.SiftCanldesStrategies;
using strategiesFrame.DataSources;
using strategiesFrame.Factories;
using strategiesFrame.Model;
using strategiesFrame.Settings;

#endregion

namespace strategiesFrame
{
    internal class TradeStrategiesFrame
    {
        public static List<Candle> readCandles(String fileName)
        {
            DataSource source = DataSourceFactory.createDataSource();

            return source.readCandlesFrom(fileName);
        }

        public static List<InitialSettings> readSettings()
        {
            List<InitialSettings> settings = new List<InitialSettings>();

            StreamReader reader = new StreamReader("settings.txt");

            String line;
            while ((line = reader.ReadLine()) != null)
                settings.Add(InitialSettings.createFrom(line));

            return settings;
        }

        public static void Main(string[] args)
        {
            List<InitialSettings> tradeSettings = readSettings();

            foreach (InitialSettings settings in tradeSettings)
            {
                foreach (String year in settings.years)
                {
                    List<Candle> candles =  readCandles("sources\\" + year + "\\" + settings.ticket + "_" + settings.timeFrame + ".txt");

                    CommissionStrategy commissionStrategy =
                        CommissionStrategyFactory.createConstantCommissionStrategy(settings.commission);

                    SiftCandlesStrategy siftStrategy = SiftCandlesStrategyFactory.createSiftStrategie(settings.siftStep);

                    FramePortfolio portfolio = new FramePortfolio(settings.ticket, commissionStrategy, siftStrategy);

                    portfolio.initMachines(settings.decisionStrategyName, settings.depths);

                    portfolio.addCandlesRange(candles);

                    portfolio.trade(year);
                }
            }
        }
    }
}