#region

using System;
using System.Collections.Generic;
using core.Model;

#endregion

namespace strategiesFrame.DataSources
{
    internal abstract class DataSource
    {
        public abstract List<Candle> readCandlesFrom(String fileName);
    }
}