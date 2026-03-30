using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserService
    {
        private readonly DatabaseHelper _db;

        public UserService(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            var users = new List<UserModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT \"Id\", \"Username\", \"FullName\", \"Email\", \"Phone\", \"IsActive\", \"CreatedAt\", \"LastLoginAt\" FROM \"YLUsers\" ORDER BY \"Id\"",
                conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserModel
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    FullName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6),
                    LastLoginAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }

            return users;
        }

        public async Task<(bool success, string message)> CreateUserAsync(UserModel user)
        {
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    "INSERT INTO \"YLUsers\" (\"Username\", \"Password\", \"FullName\", \"Email\", \"Phone\", \"IsActive\") VALUES (@u, @p, @fn, @e, @ph, @a)",
                    conn);
                cmd.Parameters.AddWithValue("u", user.Username);
                cmd.Parameters.AddWithValue("p", user.Password);
                cmd.Parameters.AddWithValue("fn", user.FullName);
                cmd.Parameters.AddWithValue("e", (object?)user.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ph", (object?)user.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("a", user.IsActive);

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
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                string sql = string.IsNullOrEmpty(user.Password)
                    ? "UPDATE \"YLUsers\" SET \"Username\"=@u, \"FullName\"=@fn, \"Email\"=@e, \"Phone\"=@ph, \"IsActive\"=@a WHERE \"Id\"=@id"
                    : "UPDATE \"YLUsers\" SET \"Username\"=@u, \"Password\"=@p, \"FullName\"=@fn, \"Email\"=@e, \"Phone\"=@ph, \"IsActive\"=@a WHERE \"Id\"=@id";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", user.Id);
                cmd.Parameters.AddWithValue("u", user.Username);
                cmd.Parameters.AddWithValue("fn", user.FullName);
                cmd.Parameters.AddWithValue("e", (object?)user.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ph", (object?)user.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("a", user.IsActive);

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

                using var cmd = new NpgsqlCommand("DELETE FROM \"YLUsers\" WHERE \"Id\"=@id", conn);
                cmd.Parameters.AddWithValue("id", id);

                await cmd.ExecuteNonQueryAsync();
                return (true, "Kullanici silindi");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }
    }
}
