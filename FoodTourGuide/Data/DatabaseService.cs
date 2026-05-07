using SQLite;
using FoodTourGuide.Models;

namespace FoodTourGuide.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        public async Task Init()
        {
            if (_db != null) return;

            var path = Path.Combine(FileSystem.AppDataDirectory, "foodtour.db");
            _db = new SQLiteAsyncConnection(path);

            await _db.CreateTableAsync<PoI>();
        }

        public SQLiteAsyncConnection GetDb()
        {
            return _db;
        }
    }
}