using LinqToDB;
using LinqToDB.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace TMRAgent.MySQL.DBConnection
{
    public class ConnectionStringSettings : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public bool IsGlobal => false;
    }

    public class MySettings : ILinqToDBSettings
    {
        public IEnumerable<IDataProviderSettings> DataProviders
            => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySQL";
        public string DefaultDataProvider => "MySQL";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return
                    new ConnectionStringSettings
                    {
                        Name = "TMRAgent",
                        ProviderName = ProviderName.MySql,
                        ConnectionString = ConfigurationHandler.Instance.Configuration.ConnectionString!
                    };
            }
        }
    }
}
