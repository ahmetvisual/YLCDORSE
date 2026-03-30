using Npgsql;

namespace YALCINDORSE.Helpers
{
    public class DatabaseHelper
    {
        private string _connectionString;

        public DatabaseHelper()
        {
            string host = ReadHostFromConfiguration();
            BuildConnectionString(host);
        }

        private void BuildConnectionString(string host)
        {
            string database = "TRAILER2";
            string userId = "erpci";
            string password = "Guclu1579!_1";

            _connectionString = $"Host={host};Database={database};Username={userId};Password={password};SSL Mode=Disable;Timeout=5;";
        }

        private string ReadHostFromConfiguration()
        {
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "configuration.txt");

                if (File.Exists(configPath))
                {
                    string firstLine = File.ReadLines(configPath).FirstOrDefault() ?? "";
                    if (!string.IsNullOrWhiteSpace(firstLine))
                        return firstLine.Trim();
                }

                return GetDefaultHost();
            }
            catch
            {
                return GetDefaultHost();
            }
        }

        private static string GetDefaultHost()
        {
#if ANDROID
            // Android emulator -> host makinaya 10.0.2.2 ile ulasilir
            return "10.0.2.2:5432";
#else
            return "127.0.0.1:5432";
#endif
        }

        public void UpdateHost(string host)
        {
            BuildConnectionString(host);

            // configuration.txt'ye kaydet
            try
            {
                string configPath = Path.Combine(FileSystem.AppDataDirectory, "configuration.txt");
                File.WriteAllText(configPath, host);
            }
            catch { }
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
