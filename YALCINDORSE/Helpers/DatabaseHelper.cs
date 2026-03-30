using Npgsql;

namespace YALCINDORSE.Helpers
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            string host = ReadHostFromConfiguration();
            string database = "TRAILER2";
            string userId = "erpci";
            string password = "Guclu1579!_1";

            _connectionString = $"Host={host};Database={database};Username={userId};Password={password};SSL Mode=Disable;";
        }

        private string ReadHostFromConfiguration()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "configuration.txt");

                if (File.Exists(configPath))
                {
                    string firstLine = File.ReadLines(configPath).FirstOrDefault() ?? "127.0.0.1:5432";
                    return firstLine.Trim();
                }

                // Varsayılan
                return "127.0.0.1:5432";
            }
            catch
            {
                return "127.0.0.1:5432";
            }
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetConnectionString() => _connectionString;
    }
}
