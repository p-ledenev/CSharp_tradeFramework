#region

using System;
using System.Linq;

#endregion

namespace strategiesFrame.Settings
{
    internal class InitialSettings
    {
        public String ticket { get; set; }
        public String timeFrame { get; set; }
        public String[] years { get; set; }
        public int[] depths { get; set; }
        public double commission { get; set; }
        public double siftStep { get; set; }
        public String decisionStrategyName { get; set; }

        public static InitialSettings createFrom(String line)
        {
            String[] data = line.Split(new[] {"~&~"}, StringSplitOptions.None);

            InitialSettings settings = new InitialSettings()
            {
                ticket = data[0],
                timeFrame = data[1],
                decisionStrategyName = data[2],
                commission = Double.Parse(data[3]),
                siftStep = Double.Parse(data[4]),
                years = data[5].Split(';'),
                depths = data[6].Split(';').Select(int.Parse).ToArray()
            };

            return settings;
        }
    }
}