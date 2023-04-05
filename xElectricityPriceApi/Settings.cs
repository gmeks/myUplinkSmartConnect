using Microsoft.AspNetCore.Mvc.Rendering;

namespace xElectricityPriceApi
{
    public class Settings
    {
        public static string GetSqlLightDatabaseConStr()
        {
            //Data Source=C:\SQLITEDATABASES\SQLITEDB1.sqlite;Version=3;
            if (!Directory.Exists(DatabasePath))
                Directory.CreateDirectory(DatabasePath);

            string databseName = System.IO.Path.Combine(DatabasePath, "Database.db");
            return $"Data Source={databseName}";
        }

        public static string DatabasePath { get; set; } = "d:\\temp\\";
    }
}
