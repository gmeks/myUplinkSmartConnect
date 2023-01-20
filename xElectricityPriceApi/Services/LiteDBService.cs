using LiteDB;
using xElectricityPriceApi.Models;
using xElectricityPriceApiShared.Model;

namespace xElectricityPriceApi.Services
{
    public class LiteDBService
    {
        readonly LiteDatabase _databaseInstance;
        public LiteDBService(string fileName)
        {
            var con = new ConnectionString()
            {
                Filename = fileName,
            };
            string constr = $"Filename={fileName}"; // Filename=C:\database.db;Password=1234;
            _databaseInstance = new LiteDatabase(con);

            var images = _databaseInstance.GetCollection<PriceInformation>(nameof(PriceInformation));
            images.EnsureIndex(x => x.Id);
            images.EnsureIndex(x => x.Start);

            var avaregeprice = _databaseInstance.GetCollection<AveragePrice>(nameof(AveragePrice));
            avaregeprice.EnsureIndex(x => x.Id);
            avaregeprice.EnsureIndex(x => x.Point);
        }

        public LiteDatabase DatabaseInstance
        {
            get { return _databaseInstance; }
        }

        public ILiteCollection<T> GetCollection<T>() where T : class
        {
            if (_databaseInstance is null)
                throw new ArgumentNullException(nameof(_databaseInstance));

            return _databaseInstance.GetCollection<T>();
        }

        public IBsonDataReader Execute(string v)
        {
            if (_databaseInstance is null)
                throw new ArgumentNullException(nameof(_databaseInstance));

            return _databaseInstance.Execute(v);
        }
    }
}
