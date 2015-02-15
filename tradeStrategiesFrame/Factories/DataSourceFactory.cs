using strategiesFrame.DataSources;

namespace strategiesFrame.Factories
{
    internal class DataSourceFactory
    {
        public static DataSource createDataSource()
        {
            return createFinamFileDataSource();
        }

        protected static DataSource createCsvDataSource()
        {
            return new CsvDataSource();
        }

        protected static DataSource createFinamFileDataSource()
        {
            return new FinamFileDataSource();
        }
    }
}