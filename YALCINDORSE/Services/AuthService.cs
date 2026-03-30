using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class AuthService
    {
        private readonly DatabaseHelper _db;

        public bool IsAuthenticated { get; private set; }
        public string? CurrentUser { get; private set; }
        public string? FullName { get; private set; }

        public event Action? OnAuthStateChanged;

        public AuthService(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(bool success, string message)> LoginAsync(string username, string password)
        {
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(
                    "SELECT \"Id\", \"FullName\" FROM \"YLUsers\" WHERE \"Username\" = @user AND \"Password\" = @pass AND \"IsActive\" = true",
                    conn);
                cmd.Parameters.AddWithValue("user", username);
                cmd.Parameters.AddWithValue("pass", password);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    IsAuthenticated = true;
                    CurrentUser = username;
                    FullName = reader.GetString(1);
                    OnAuthStateChanged?.Invoke();
                    return (true, "Giriş başarılı");
                }

                return (false, "Kullanıcı adı veya şifre hatalı");
            }
            catch (Exception ex)
            {
                return (false, $"Bağlantı hatası: {ex.Message}");
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            CurrentUser = null;
            FullName = null;
            OnAuthStateChanged?.Invoke();
        }
    }
}
