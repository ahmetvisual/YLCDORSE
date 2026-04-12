using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class UserModel
    {
        public int      Id         { get; set; }
        public string   Username   { get; set; } = "";
        public string   Password   { get; set; } = "";
        public string   FullName   { get; set; } = "";
        public string?  Email      { get; set; }
        public string?  Phone      { get; set; }
        public string?  Departman  { get; set; }
        public string   Rol        { get; set; } = "user";   // "admin" | "user"
        public bool     IsActive   { get; set; } = true;
        public DateTime CreatedAt  { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // Departman seçenekleri — şirket organizasyon yapısına göre
    public static class Departmanlar
    {
        public static readonly string[] Liste =
        {
            "Müdür",
            "Yönetim",
            "Satış",
            "Muhasebe / Finans",
            "İnsan Kaynakları",
            "Santral / Info",
            "Satınalma",
            "Kalite",
            "Yurtdışı / İhracat",
            "Teknik Ofis",
        };
    }

    public class UserService
    {
        private readonly DatabaseHelper _db;
        private bool _schemaEnsured;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);

        public UserService(DatabaseHelper db)
        {
            _db = db;
        }

        // ── Schema migration: Departman + Rol kolonları (idempotent) ────
        private async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured) return;
            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured) return;
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                const string sql = """
                    ALTER TABLE "YLUsers"
                        ADD COLUMN IF NOT EXISTS "Departman" VARCHAR(100),
                        ADD COLUMN IF NOT EXISTS "Rol"       VARCHAR(20) DEFAULT 'user';
                    UPDATE "YLUsers" SET "Rol" = 'user' WHERE "Rol" IS NULL;
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
                _schemaEnsured = true;
            }
            catch { _schemaEnsured = true; /* kolon zaten varsa hata vermez */ }
            finally { _schemaLock.Release(); }
        }

        // ── CRUD ─────────────────────────────────────────────────────────

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            await EnsureSchemaAsync();
            var users = new List<UserModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                """
                SELECT "Id","Username","FullName","Email","Phone",
                       "IsActive","CreatedAt","LastLoginAt",
                       "Departman","Rol"
                FROM "YLUsers" ORDER BY "FullName"
                """, conn);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                users.Add(new UserModel
                {
                    Id          = r.GetInt32(0),
                    Username    = r.GetString(1),
                    FullName    = r.GetString(2),
                    Email       = r.IsDBNull(3)  ? null : r.GetString(3),
                    Phone       = r.IsDBNull(4)  ? null : r.GetString(4),
                    IsActive    = r.GetBoolean(5),
                    CreatedAt   = r.GetDateTime(6),
                    LastLoginAt = r.IsDBNull(7)  ? null : r.GetDateTime(7),
                    Departman   = r.IsDBNull(8)  ? null : r.GetString(8),
                    Rol         = r.IsDBNull(9)  ? "user" : r.GetString(9),
                });
            }
            return users;
        }

        public async Task<(bool success, string message)> CreateUserAsync(UserModel user)
        {
            await EnsureSchemaAsync();
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO "YLUsers"
                        ("Username","Password","FullName","Email","Phone","IsActive","Departman","Rol")
                    VALUES (@u, @p, @fn, @e, @ph, @a, @dep, @rol)
                    """, conn);
                cmd.Parameters.AddWithValue("u",   user.Username);
                cmd.Parameters.AddWithValue("p",   user.Password);
                cmd.Parameters.AddWithValue("fn",  user.FullName);
                cmd.Parameters.AddWithValue("e",   (object?)user.Email     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ph",  (object?)user.Phone     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("a",   user.IsActive);
                cmd.Parameters.AddWithValue("dep", (object?)user.Departman ?? DBNull.Value);
                cmd.Parameters.AddWithValue("rol", user.Rol);

                await cmd.ExecuteNonQueryAsync();
                return (true, "Kullanici basariyla olusturuldu");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return (false, "Bu kullanici adi zaten mevcut");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateUserAsync(UserModel user)
        {
            await EnsureSchemaAsync();
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                var sql = string.IsNullOrEmpty(user.Password)
                    ? """
                      UPDATE "YLUsers"
                      SET "Username"=@u,"FullName"=@fn,"Email"=@e,"Phone"=@ph,
                          "IsActive"=@a,"Departman"=@dep,"Rol"=@rol
                      WHERE "Id"=@id
                      """
                    : """
                      UPDATE "YLUsers"
                      SET "Username"=@u,"Password"=@p,"FullName"=@fn,"Email"=@e,"Phone"=@ph,
                          "IsActive"=@a,"Departman"=@dep,"Rol"=@rol
                      WHERE "Id"=@id
                      """;

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id",  user.Id);
                cmd.Parameters.AddWithValue("u",   user.Username);
                cmd.Parameters.AddWithValue("fn",  user.FullName);
                cmd.Parameters.AddWithValue("e",   (object?)user.Email     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ph",  (object?)user.Phone     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("a",   user.IsActive);
                cmd.Parameters.AddWithValue("dep", (object?)user.Departman ?? DBNull.Value);
                cmd.Parameters.AddWithValue("rol", user.Rol);
                if (!string.IsNullOrEmpty(user.Password))
                    cmd.Parameters.AddWithValue("p", user.Password);

                await cmd.ExecuteNonQueryAsync();
                return (true, "Kullanici basariyla guncellendi");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return (false, "Bu kullanici adi zaten mevcut");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteUserAsync(int id)
        {
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand("""DELETE FROM "YLUsers" WHERE "Id"=@id""", conn);
                cmd.Parameters.AddWithValue("id", id);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Kullanici silindi");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword)
        {
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                // Mevcut şifreyi doğrula
                using var verifyCmd = new NpgsqlCommand(
                    """SELECT COUNT(*) FROM "YLUsers" WHERE "Id"=@id AND "Password"=@pass""", conn);
                verifyCmd.Parameters.AddWithValue("id",   userId);
                verifyCmd.Parameters.AddWithValue("pass", currentPassword);
                var count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());

                if (count == 0)
                    return (false, "Mevcut sifre hatali");

                using var updateCmd = new NpgsqlCommand(
                    """UPDATE "YLUsers" SET "Password"=@np WHERE "Id"=@id""", conn);
                updateCmd.Parameters.AddWithValue("id", userId);
                updateCmd.Parameters.AddWithValue("np", newPassword);
                await updateCmd.ExecuteNonQueryAsync();

                return (true, "Sifre basariyla degistirildi");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }
    }
}
