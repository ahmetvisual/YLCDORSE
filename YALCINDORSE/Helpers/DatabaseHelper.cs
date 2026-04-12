using Npgsql;
using Microsoft.Data.SqlClient;

namespace YALCINDORSE.Helpers
{
    public class DatabaseHelper
    {
        /* ─── PostgreSQL ─────────────────────────────── */
        private string _pgConnectionString = "";
        private string[] _pgHosts = Array.Empty<string>();
        private int _activePgIdx;

        /* ─── MSSQL (Zirve) ─────────────────────────── */
        private string[] _mssqlServers = Array.Empty<string>();
        private string _mssqlDatabase = "";
        private string _mssqlUser = "sa";
        private string _mssqlPassword = "";
        private int _activeMssqlIdx;

        public bool HasMssqlConfig =>
            _mssqlServers.Length > 0
            && !string.IsNullOrWhiteSpace(_mssqlDatabase)
            && !string.IsNullOrWhiteSpace(_mssqlUser);

        public string MssqlUser => _mssqlUser;
        public string MssqlDatabase => _mssqlDatabase;
        public string[] MssqlServers => _mssqlServers;

        /* ─── Constructor ────────────────────────────── */
        public DatabaseHelper()
        {
            var lines = ReadConfigurationLines();
            ParsePgConfig(lines);
            ParseMssqlConfig(lines);
            BuildPgConnectionString(_pgHosts[_activePgIdx]);
        }

        /* ═══════════════════════════════════════════════
           POSTGRESQL
           ═══════════════════════════════════════════════ */

        private void BuildPgConnectionString(string host)
        {
            _pgConnectionString =
                $"Host={host};Database=TRAILER2;Username=erpci;Password=Guclu1579!_1;SSL Mode=Disable;Timeout=5;";
        }

        public NpgsqlConnection GetConnection() => new NpgsqlConnection(_pgConnectionString);

        /// <summary>
        /// Baglanti testi — Tailscale/lokal IP'leri otomatik dener.
        /// Calisan IP'yi aktif yapar.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            foreach (var host in Rotated(_pgHosts, _activePgIdx))
            {
                try
                {
                    BuildPgConnectionString(host);
                    using var conn = GetConnection();
                    await conn.OpenAsync();
                    _activePgIdx = Array.IndexOf(_pgHosts, host);
                    return true;
                }
                catch { /* sonraki host'u dene */ }
            }

            // Hepsi basarisiz — ilk host'a geri don
            BuildPgConnectionString(_pgHosts[0]);
            _activePgIdx = 0;
            return false;
        }

        public string GetConnectionString() => _pgConnectionString;

        public void UpdateHost(string host)
        {
            _pgHosts = host.Contains('|')
                ? host.Split('|').Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => s.Trim()).ToArray()
                : new[] { host.Trim() };

            _activePgIdx = 0;
            BuildPgConnectionString(_pgHosts[0]);
            SaveConfiguration();
        }

        /* ═══════════════════════════════════════════════
           MSSQL (ZİRVE)
           ═══════════════════════════════════════════════ */

        private string BuildMssqlCs(string server)
        {
            return $"Server={server};Database={_mssqlDatabase};"
                 + $"User Id={_mssqlUser};Password={_mssqlPassword};"
                 + "TrustServerCertificate=True;Encrypt=False;Connect Timeout=5;";
        }

        /// <summary>
        /// Zirve MSSQL'e baglan — tum sunuculari otomatik dener.
        /// Baglanti AÇIK döner — çağıran kapatacak (using).
        /// </summary>
        public async Task<SqlConnection?> GetMssqlConnectionAsync()
        {
            if (!HasMssqlConfig) return null;

            foreach (var server in Rotated(_mssqlServers, _activeMssqlIdx))
            {
                try
                {
                    var conn = new SqlConnection(BuildMssqlCs(server));
                    await conn.OpenAsync();
                    _activeMssqlIdx = Array.IndexOf(_mssqlServers, server);
                    return conn;
                }
                catch { /* sonraki sunucu */ }
            }
            return null;
        }

        /// <summary>
        /// MSSQL baglanti testi — sonuç + mesaj döner.
        /// </summary>
        public async Task<(bool ok, string message)> TestMssqlAsync()
        {
            if (!HasMssqlConfig)
                return (false, "MSSQL yapilandirmasi bulunamadi. configuration.txt 2. satiri kontrol edin.");

            if (string.IsNullOrWhiteSpace(_mssqlPassword))
                return (false, "MSSQL sifresi bos. Lutfen sifre girin.");

            foreach (var server in _mssqlServers)
            {
                try
                {
                    using var conn = new SqlConnection(BuildMssqlCs(server));
                    await conn.OpenAsync();
                    return (true, $"Zirve baglantisi basarili ({server})");
                }
                catch (Exception ex)
                {
                    // Sonraki sunucuyu dene
                    if (server == _mssqlServers[^1])
                        return (false, $"Baglanti basarisiz: {ex.Message}");
                }
            }
            return (false, "Tum sunuculara baglanti basarisiz.");
        }

        /// <summary>MSSQL kullanici adi ve sifre güncelle, configuration.txt'ye kaydet.</summary>
        public void UpdateMssqlCredentials(string user, string password)
        {
            _mssqlUser = user;
            _mssqlPassword = password;
            SaveConfiguration();
        }

        /* ═══════════════════════════════════════════════
           CONFIGURATION I/O
           ═══════════════════════════════════════════════ */

        private string[] ReadConfigurationLines()
        {
            try
            {
                // 1) Kullanicinin runtime'da kaydettiği dosya
                string appDataPath = Path.Combine(FileSystem.AppDataDirectory, "configuration.txt");
                if (File.Exists(appDataPath))
                    return File.ReadAllLines(appDataPath);

                // 2) Build çıktısındaki dosya
                string exePath = Path.Combine(AppContext.BaseDirectory, "configuration.txt");
                if (File.Exists(exePath))
                    return File.ReadAllLines(exePath);
            }
            catch { }
            return Array.Empty<string>();
        }

        private void ParsePgConfig(string[] lines)
        {
            if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
            {
                _pgHosts = lines[0].Trim()
                    .Split('|')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToArray();
            }

            if (_pgHosts.Length == 0)
                _pgHosts = new[] { GetDefaultHost() };
        }

        /// <summary>
        /// Satır 2 formatı: server1|server2|database|user|password
        /// (password pipe içerebilir — sondan birleştirilir)
        /// </summary>
        private void ParseMssqlConfig(string[] lines)
        {
            if (lines.Length < 2 || string.IsNullOrWhiteSpace(lines[1]))
                return;

            var parts = lines[1].Trim().Split('|');

            // 2-sunucu: server1|server2|db|user|password...
            if (parts.Length >= 5)
            {
                _mssqlServers = new[] { parts[0].Trim(), parts[1].Trim() };
                _mssqlDatabase = parts[2].Trim();
                _mssqlUser = parts[3].Trim();
                _mssqlPassword = string.Join("|", parts.Skip(4)); // sifrede | olabilir
            }
            // 1-sunucu: server|db|user|password...
            else if (parts.Length >= 4)
            {
                _mssqlServers = new[] { parts[0].Trim() };
                _mssqlDatabase = parts[1].Trim();
                _mssqlUser = parts[2].Trim();
                _mssqlPassword = string.Join("|", parts.Skip(3));
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var pgLine = string.Join("|", _pgHosts);

                string mssqlLine = "";
                if (_mssqlServers.Length >= 2)
                    mssqlLine = $"{_mssqlServers[0]}|{_mssqlServers[1]}|{_mssqlDatabase}|{_mssqlUser}|{_mssqlPassword}";
                else if (_mssqlServers.Length == 1)
                    mssqlLine = $"{_mssqlServers[0]}|{_mssqlDatabase}|{_mssqlUser}|{_mssqlPassword}";

                var configPath = Path.Combine(FileSystem.AppDataDirectory, "configuration.txt");
                File.WriteAllLines(configPath, new[] { pgLine, mssqlLine });
            }
            catch { }
        }

        private static string GetDefaultHost()
        {
#if ANDROID
            return "10.0.2.2:5432";
#else
            return "127.0.0.1:5432";
#endif
        }

        /// <summary>Dizi elemanlarını verilen index'ten başlayarak döndürür (round-robin).</summary>
        private static IEnumerable<string> Rotated(string[] arr, int start)
        {
            for (int i = 0; i < arr.Length; i++)
                yield return arr[(start + i) % arr.Length];
        }
    }
}
