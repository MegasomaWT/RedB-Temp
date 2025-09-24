using redb.Core;
using System.Diagnostics.Metrics;

namespace redb.WebApp.DataModels
{
    public class InfoSystem(IRedbService redbService)
    {
        public string dbVersion { get; private set; } = redbService.dbVersion;
        public string dbType { get; private set; } = redbService.dbType;
        public string dbMigration { get; private set; } = redbService.dbMigration;

        public int? dbSize { get; private set; } = redbService.dbSize;

    }
}
