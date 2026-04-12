using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class AuthService
    {
        private readonly DatabaseHelper _db;

        public bool    IsAuthenticated { get; private set; }
        public string? CurrentUser     { get; private set; }
        public string? FullName        { get; private set; }
        public int?    CurrentUserId   { get; private set; }
        public string  Rol             { get; private set; } = "user";
        public string? Departman       { get; private set; }

        public bool IsAdmin => Rol == "admin";

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
                    """
                    SELECT "Id","FullName","Rol","Departman"
                    FROM "YLUsers"
                    WHERE "Username"=@user AND "Password"=@pass AND "IsActive"=true
                    """, conn);
                cmd.Parameters.AddWithValue("user", username);
                cmd.Parameters.AddWithValue("pass", password);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    IsAuthenticated = true;
                    CurrentUser     = username;
                    CurrentUserId   = reader.GetInt32(0);
                    FullName        = reader.GetString(1);
                    Rol             = reader.IsDBNull(2) ? "user" : reader.GetString(2);
                    Departman       = reader.IsDBNull(3) ? null   : reader.GetString(3);
                    OnAuthStateChanged?.Invoke();
                    return (true, "Giris basarili");
                }

                return (false, "Kullanici adi veya sifre hatali");
            }
            catch (Exception ex)
            {
                return (false, $"Baglanti hatasi: {ex.Message}");
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            CurrentUser     = null;
            CurrentUserId   = null;
            FullName        = null;
            Rol             = "user";
            Departman       = null;
            OnAuthStateChanged?.Invoke();
        }

        public async Task<(bool success, string message)> ChangePasswordAsync(
            string currentPassword, string newPassword)
        {
            if (!IsAuthenticated || !CurrentUserId.HasValue)
                return (false, "Oturum acik degil");

            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                using var verifyCmd = new NpgsqlCommand(
                    """SELECT COUNT(*) FROM "YLUsers" WHERE "Id"=@id AND "Password"=@pass""", conn);
                verifyCmd.Parameters.AddWithValue("id",   CurrentUserId.Value);
                verifyCmd.Parameters.AddWithValue("pass", currentPassword);
                var count = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());

                if (count == 0)
                    return (false, "Mevcut sifre hatali");

                using var updateCmd = new NpgsqlCommand(
                    """UPDATE "YLUsers" SET "Password"=@np WHERE "Id"=@id""", conn);
                updateCmd.Parameters.AddWithValue("id", CurrentUserId.Value);
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
