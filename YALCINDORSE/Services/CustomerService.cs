using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class CustomerListItemModel
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string? TaxNumber { get; set; }
        public string Country { get; set; } = "TR";
        public decimal CurrentBalance { get; set; }
        public string? SalesRepName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CustomerContactModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string ContactName { get; set; } = "";
        public string? ContactTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Department { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class CustomerModel
    {
        public int Id { get; set; }
        public string CustomerCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string Country { get; set; } = "TR";
        public string? Address { get; set; }
        public decimal RiskLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public int? SalesRepId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public List<CustomerContactModel> Contacts { get; set; } = new();
    }

    public class SalesRepOption
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
    }

    public class CustomerService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);
        private bool _schemaEnsured;

        private const string SchemaSql = """
            CREATE SEQUENCE IF NOT EXISTS "YLCustomerCodeSeq"
                START WITH 1
                INCREMENT BY 1;

            CREATE OR REPLACE FUNCTION "YLGenerateCustomerCode"()
            RETURNS TRIGGER
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NEW."CustomerCode" IS NULL OR BTRIM(NEW."CustomerCode") = '' THEN
                    NEW."CustomerCode" := 'CUST-' || LPAD(nextval('"YLCustomerCodeSeq"')::text, 3, '0');
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TABLE IF NOT EXISTS "YLCustomers"
            (
                "Id" SERIAL PRIMARY KEY,
                "CustomerCode" VARCHAR(20) NOT NULL,
                "Title" VARCHAR(200) NOT NULL,
                "TaxNumber" VARCHAR(50),
                "TaxOffice" VARCHAR(100),
                "Country" VARCHAR(2) NOT NULL DEFAULT 'TR',
                "Address" VARCHAR(500),
                "RiskLimit" NUMERIC(18,2) NOT NULL DEFAULT 0,
                "CurrentBalance" NUMERIC(18,2) NOT NULL DEFAULT 0,
                "SalesRepId" INT NULL,
                "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
                "Notes" VARCHAR(1000),
                "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "CreatedBy" VARCHAR(100) NOT NULL DEFAULT 'system',
                "ModifiedDate" TIMESTAMP NULL,
                "ModifiedBy" VARCHAR(100) NULL,
                CONSTRAINT "FK_YLCustomers_SalesRep"
                    FOREIGN KEY ("SalesRepId")
                    REFERENCES "YLUsers" ("Id")
                    ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS "YLCustomerContacts"
            (
                "Id" SERIAL PRIMARY KEY,
                "CustomerId" INT NOT NULL,
                "ContactName" VARCHAR(100) NOT NULL,
                "ContactTitle" VARCHAR(100),
                "Email" VARCHAR(100),
                "Phone" VARCHAR(30),
                "Mobile" VARCHAR(30),
                "Department" VARCHAR(100),
                "IsPrimary" BOOLEAN NOT NULL DEFAULT FALSE,
                "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
                "Notes" VARCHAR(500),
                "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "CreatedBy" VARCHAR(100) NOT NULL DEFAULT 'system',
                "ModifiedDate" TIMESTAMP NULL,
                "ModifiedBy" VARCHAR(100) NULL,
                CONSTRAINT "FK_YLCustomerContacts_Customer"
                    FOREIGN KEY ("CustomerId")
                    REFERENCES "YLCustomers" ("Id")
                    ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_YLCustomers_Code"
                ON "YLCustomers" ("CustomerCode");

            CREATE INDEX IF NOT EXISTS "IX_YLCustomers_Country"
                ON "YLCustomers" ("Country");

            CREATE INDEX IF NOT EXISTS "IX_YLCustomers_SalesRep"
                ON "YLCustomers" ("SalesRepId");

            CREATE INDEX IF NOT EXISTS "IX_YLCustomerContacts_CustomerId"
                ON "YLCustomerContacts" ("CustomerId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_YLCustomerContacts_Primary"
                ON "YLCustomerContacts" ("CustomerId")
                WHERE "IsPrimary" = TRUE AND "IsActive" = TRUE;

            DROP TRIGGER IF EXISTS "TRG_YLCustomers_Code" ON "YLCustomers";

            CREATE TRIGGER "TRG_YLCustomers_Code"
            BEFORE INSERT ON "YLCustomers"
            FOR EACH ROW
            EXECUTE FUNCTION "YLGenerateCustomerCode"();
            """;

        public CustomerService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        public async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured)
                return;

            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured)
                    return;

                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                try
                {
                    using var cmd = new NpgsqlCommand(SchemaSql, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception)
                {
                    // Tablolarin sahibi olmadigimizda (must be owner hatasi) veya yetki kisitlamalarinda
                    // sorma/olusturma hatalarini gormezden gelip devam ediyoruz.
                    // Tablolari sistem admin'inin (postgres vb.) olusturdugu varsayilmaktadir.
                }

                _schemaEnsured = true;
            }
            finally
            {
                _schemaLock.Release();
            }
        }

        public async Task<List<CustomerListItemModel>> GetCustomersAsync()
        {
            await EnsureSchemaAsync();

            var items = new List<CustomerListItemModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT c."Id",
                       c."CustomerCode",
                       c."Title",
                       c."TaxNumber",
                       c."Country",
                       c."CurrentBalance",
                       c."IsActive",
                       u."FullName"
                FROM "YLCustomers" c
                LEFT JOIN "YLUsers" u ON u."Id" = c."SalesRepId"
                ORDER BY c."Title";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new CustomerListItemModel
                {
                    Id = reader.GetInt32(0),
                    CustomerCode = reader.GetString(1),
                    Title = reader.GetString(2),
                    TaxNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Country = reader.IsDBNull(4) ? "TR" : reader.GetString(4),
                    CurrentBalance = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                    IsActive = reader.GetBoolean(6),
                    SalesRepName = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return items;
        }

        public async Task<CustomerModel?> GetCustomerByIdAsync(int id)
        {
            await EnsureSchemaAsync();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string customerSql = """
                SELECT "Id",
                       "CustomerCode",
                       "Title",
                       "TaxNumber",
                       "TaxOffice",
                       "Country",
                       "Address",
                       "RiskLimit",
                       "CurrentBalance",
                       "SalesRepId",
                       "IsActive",
                       "Notes",
                       "CreatedDate",
                       "CreatedBy",
                       "ModifiedDate",
                       "ModifiedBy"
                FROM "YLCustomers"
                WHERE "Id" = @id;
                """;

            using var customerCmd = new NpgsqlCommand(customerSql, conn);
            customerCmd.Parameters.AddWithValue("id", id);

            CustomerModel? customer = null;
            using (var reader = await customerCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    customer = new CustomerModel
                    {
                        Id = reader.GetInt32(0),
                        CustomerCode = reader.GetString(1),
                        Title = reader.GetString(2),
                        TaxNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                        TaxOffice = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Country = reader.IsDBNull(5) ? "TR" : reader.GetString(5),
                        Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                        RiskLimit = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        CurrentBalance = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8),
                        SalesRepId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                        IsActive = reader.GetBoolean(10),
                        Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
                        CreatedDate = reader.IsDBNull(12) ? DateTime.Now : reader.GetDateTime(12),
                        CreatedBy = reader.IsDBNull(13) ? "system" : reader.GetString(13),
                        ModifiedDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                        ModifiedBy = reader.IsDBNull(15) ? null : reader.GetString(15)
                    };
                }
            }

            if (customer == null)
                return null;

            const string contactSql = """
                SELECT "Id",
                       "CustomerId",
                       "ContactName",
                       "ContactTitle",
                       "Email",
                       "Phone",
                       "Mobile",
                       "Department",
                       "IsPrimary",
                       "IsActive",
                       "Notes"
                FROM "YLCustomerContacts"
                WHERE "CustomerId" = @customerId
                  AND "IsActive" = TRUE
                ORDER BY "IsPrimary" DESC, "ContactName";
                """;

            using var contactCmd = new NpgsqlCommand(contactSql, conn);
            contactCmd.Parameters.AddWithValue("customerId", id);
            using var contactReader = await contactCmd.ExecuteReaderAsync();
            while (await contactReader.ReadAsync())
            {
                customer.Contacts.Add(new CustomerContactModel
                {
                    Id = contactReader.GetInt32(0),
                    CustomerId = contactReader.GetInt32(1),
                    ContactName = contactReader.GetString(2),
                    ContactTitle = contactReader.IsDBNull(3) ? null : contactReader.GetString(3),
                    Email = contactReader.IsDBNull(4) ? null : contactReader.GetString(4),
                    Phone = contactReader.IsDBNull(5) ? null : contactReader.GetString(5),
                    Mobile = contactReader.IsDBNull(6) ? null : contactReader.GetString(6),
                    Department = contactReader.IsDBNull(7) ? null : contactReader.GetString(7),
                    IsPrimary = contactReader.GetBoolean(8),
                    IsActive = contactReader.GetBoolean(9),
                    Notes = contactReader.IsDBNull(10) ? null : contactReader.GetString(10)
                });
            }

            return customer;
        }

        public async Task<List<SalesRepOption>> GetSalesRepsAsync()
        {
            await EnsureSchemaAsync();

            var reps = new List<SalesRepOption>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT "Id", "FullName"
                FROM "YLUsers"
                WHERE "IsActive" = TRUE
                ORDER BY "FullName";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reps.Add(new SalesRepOption
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1)
                });
            }

            return reps;
        }

        public CustomerModel CreateDraft()
        {
            return new CustomerModel
            {
                Country = "TR",
                IsActive = true,
                Contacts = new List<CustomerContactModel>()
            };
        }

        public async Task<(bool success, string message, int customerId)> SaveCustomerAsync(CustomerModel customer)
        {
            await EnsureSchemaAsync();

            NormalizeCustomer(customer);
            var validationError = ValidateCustomer(customer);
            if (validationError != null)
                return (false, validationError, customer.Id);

            var actor = string.IsNullOrWhiteSpace(_auth.CurrentUser) ? "system" : _auth.CurrentUser!;

            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();

                if (customer.Id == 0)
                {
                    const string insertSql = """
                        INSERT INTO "YLCustomers"
                        (
                            "Title",
                            "TaxNumber",
                            "TaxOffice",
                            "Country",
                            "Address",
                            "RiskLimit",
                            "CurrentBalance",
                            "SalesRepId",
                            "IsActive",
                            "Notes",
                            "CreatedBy"
                        )
                        VALUES
                        (
                            @title,
                            @taxNumber,
                            @taxOffice,
                            @country,
                            @address,
                            @riskLimit,
                            @currentBalance,
                            @salesRepId,
                            @isActive,
                            @notes,
                            @createdBy
                        )
                        RETURNING "Id", "CustomerCode";
                        """;

                    using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
                    AddCustomerParameters(insertCmd, customer, actor, false);

                    using var reader = await insertCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        customer.Id = reader.GetInt32(0);
                        customer.CustomerCode = reader.GetString(1);
                    }
                }
                else
                {
                    const string updateSql = """
                        UPDATE "YLCustomers"
                        SET "Title" = @title,
                            "TaxNumber" = @taxNumber,
                            "TaxOffice" = @taxOffice,
                            "Country" = @country,
                            "Address" = @address,
                            "RiskLimit" = @riskLimit,
                            "CurrentBalance" = @currentBalance,
                            "SalesRepId" = @salesRepId,
                            "IsActive" = @isActive,
                            "Notes" = @notes,
                            "ModifiedDate" = CURRENT_TIMESTAMP,
                            "ModifiedBy" = @modifiedBy
                        WHERE "Id" = @id;
                        """;

                    using var updateCmd = new NpgsqlCommand(updateSql, conn, tx);
                    AddCustomerParameters(updateCmd, customer, actor, true);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                await UpsertContactsAsync(conn, tx, customer, actor);
                await tx.CommitAsync();

                return (true, "Cari kart basariyla kaydedildi", customer.Id);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return (false, "Ayni musteri kodu veya primary ilgili kisi kaydi zaten mevcut", customer.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Kayit sirasinda hata: {ex.Message}", customer.Id);
            }
        }

        public async Task<(bool success, string message)> DeleteCustomerAsync(int id)
        {
            await EnsureSchemaAsync();

            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("""DELETE FROM "YLCustomers" WHERE "Id" = @id;""", conn);
                cmd.Parameters.AddWithValue("id", id);
                await cmd.ExecuteNonQueryAsync();

                return (true, "Cari kart silindi");
            }
            catch (Exception ex)
            {
                return (false, $"Silme sirasinda hata: {ex.Message}");
            }
        }

        private static void NormalizeCustomer(CustomerModel customer)
        {
            customer.Title = customer.Title.Trim();
            customer.CustomerCode = customer.CustomerCode.Trim();
            customer.Country = string.IsNullOrWhiteSpace(customer.Country)
                ? "TR"
                : customer.Country.Trim().ToUpperInvariant();
            customer.TaxNumber = NullIfWhiteSpace(customer.TaxNumber);
            customer.TaxOffice = NullIfWhiteSpace(customer.TaxOffice);
            customer.Address = NullIfWhiteSpace(customer.Address);
            customer.Notes = NullIfWhiteSpace(customer.Notes);

            var normalizedContacts = new List<CustomerContactModel>();
            foreach (var contact in customer.Contacts)
            {
                contact.ContactName = contact.ContactName.Trim();
                contact.ContactTitle = NullIfWhiteSpace(contact.ContactTitle);
                contact.Email = NullIfWhiteSpace(contact.Email);
                contact.Phone = NullIfWhiteSpace(contact.Phone);
                contact.Mobile = NullIfWhiteSpace(contact.Mobile);
                contact.Department = NullIfWhiteSpace(contact.Department);
                contact.Notes = NullIfWhiteSpace(contact.Notes);

                var hasAnyValue =
                    !string.IsNullOrWhiteSpace(contact.ContactName) ||
                    !string.IsNullOrWhiteSpace(contact.ContactTitle) ||
                    !string.IsNullOrWhiteSpace(contact.Email) ||
                    !string.IsNullOrWhiteSpace(contact.Phone) ||
                    !string.IsNullOrWhiteSpace(contact.Mobile) ||
                    !string.IsNullOrWhiteSpace(contact.Department) ||
                    !string.IsNullOrWhiteSpace(contact.Notes);

                if (hasAnyValue)
                    normalizedContacts.Add(contact);
            }

            customer.Contacts = normalizedContacts;
            EnsureSinglePrimaryContact(customer.Contacts);
        }

        private static string? ValidateCustomer(CustomerModel customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Title))
                return "Musteri unvani zorunludur";

            if (customer.Country.Length != 2)
                return "Ulke kodu 2 karakter olmali";

            if (customer.RiskLimit < 0)
                return "Risk limiti negatif olamaz";

            foreach (var contact in customer.Contacts)
            {
                var hasAnyValue =
                    !string.IsNullOrWhiteSpace(contact.ContactName) ||
                    !string.IsNullOrWhiteSpace(contact.ContactTitle) ||
                    !string.IsNullOrWhiteSpace(contact.Email) ||
                    !string.IsNullOrWhiteSpace(contact.Phone) ||
                    !string.IsNullOrWhiteSpace(contact.Mobile) ||
                    !string.IsNullOrWhiteSpace(contact.Department) ||
                    !string.IsNullOrWhiteSpace(contact.Notes);

                if (hasAnyValue && string.IsNullOrWhiteSpace(contact.ContactName))
                    return "Ilgili kisi satirlarinda ad zorunludur";
            }

            return null;
        }

        private static void EnsureSinglePrimaryContact(List<CustomerContactModel> contacts)
        {
            var primaryIndex = contacts.FindIndex(x => x.IsPrimary);
            if (primaryIndex < 0)
                return;

            for (var i = 0; i < contacts.Count; i++)
            {
                if (i != primaryIndex)
                    contacts[i].IsPrimary = false;
            }
        }

        private async Task UpsertContactsAsync(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            CustomerModel customer,
            string actor)
        {
            var currentIds = customer.Contacts
                .Where(x => x.Id > 0)
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            if (currentIds.Length == 0)
            {
                const string deactivateAllSql = """
                    UPDATE "YLCustomerContacts"
                    SET "IsActive" = FALSE,
                        "ModifiedDate" = CURRENT_TIMESTAMP,
                        "ModifiedBy" = @modifiedBy
                    WHERE "CustomerId" = @customerId
                      AND "IsActive" = TRUE;
                    """;

                using var deactivateAllCmd = new NpgsqlCommand(deactivateAllSql, conn, tx);
                deactivateAllCmd.Parameters.AddWithValue("customerId", customer.Id);
                deactivateAllCmd.Parameters.AddWithValue("modifiedBy", actor);
                await deactivateAllCmd.ExecuteNonQueryAsync();
            }
            else
            {
                const string deactivateMissingSql = """
                    UPDATE "YLCustomerContacts"
                    SET "IsActive" = FALSE,
                        "IsPrimary" = FALSE,
                        "ModifiedDate" = CURRENT_TIMESTAMP,
                        "ModifiedBy" = @modifiedBy
                    WHERE "CustomerId" = @customerId
                      AND "Id" <> ALL(@ids)
                      AND "IsActive" = TRUE;
                    """;

                using var deactivateMissingCmd = new NpgsqlCommand(deactivateMissingSql, conn, tx);
                deactivateMissingCmd.Parameters.AddWithValue("customerId", customer.Id);
                deactivateMissingCmd.Parameters.AddWithValue("modifiedBy", actor);
                deactivateMissingCmd.Parameters.AddWithValue("ids", currentIds);
                await deactivateMissingCmd.ExecuteNonQueryAsync();
            }

            foreach (var contact in customer.Contacts)
            {
                if (contact.Id == 0)
                {
                    const string insertSql = """
                        INSERT INTO "YLCustomerContacts"
                        (
                            "CustomerId",
                            "ContactName",
                            "ContactTitle",
                            "Email",
                            "Phone",
                            "Mobile",
                            "Department",
                            "IsPrimary",
                            "IsActive",
                            "Notes",
                            "CreatedBy"
                        )
                        VALUES
                        (
                            @customerId,
                            @contactName,
                            @contactTitle,
                            @email,
                            @phone,
                            @mobile,
                            @department,
                            @isPrimary,
                            @isActive,
                            @notes,
                            @createdBy
                        );
                        """;

                    using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
                    AddContactParameters(insertCmd, customer.Id, contact, actor, false);
                    await insertCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    const string updateSql = """
                        UPDATE "YLCustomerContacts"
                        SET "ContactName" = @contactName,
                            "ContactTitle" = @contactTitle,
                            "Email" = @email,
                            "Phone" = @phone,
                            "Mobile" = @mobile,
                            "Department" = @department,
                            "IsPrimary" = @isPrimary,
                            "IsActive" = @isActive,
                            "Notes" = @notes,
                            "ModifiedDate" = CURRENT_TIMESTAMP,
                            "ModifiedBy" = @modifiedBy
                        WHERE "Id" = @id
                          AND "CustomerId" = @customerId;
                        """;

                    using var updateCmd = new NpgsqlCommand(updateSql, conn, tx);
                    AddContactParameters(updateCmd, customer.Id, contact, actor, true);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static void AddCustomerParameters(NpgsqlCommand cmd, CustomerModel customer, string actor, bool isUpdate)
        {
            if (isUpdate)
            {
                cmd.Parameters.AddWithValue("id", customer.Id);
                cmd.Parameters.AddWithValue("modifiedBy", actor);
            }
            else
            {
                cmd.Parameters.AddWithValue("createdBy", actor);
            }

            cmd.Parameters.AddWithValue("title", customer.Title);
            cmd.Parameters.AddWithValue("taxNumber", (object?)customer.TaxNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("taxOffice", (object?)customer.TaxOffice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("country", customer.Country);
            cmd.Parameters.AddWithValue("address", (object?)customer.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("riskLimit", customer.RiskLimit);
            cmd.Parameters.AddWithValue("currentBalance", customer.CurrentBalance);
            cmd.Parameters.AddWithValue("salesRepId", (object?)customer.SalesRepId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isActive", customer.IsActive);
            cmd.Parameters.AddWithValue("notes", (object?)customer.Notes ?? DBNull.Value);
        }

        private static void AddContactParameters(
            NpgsqlCommand cmd,
            int customerId,
            CustomerContactModel contact,
            string actor,
            bool isUpdate)
        {
            if (isUpdate)
            {
                cmd.Parameters.AddWithValue("id", contact.Id);
                cmd.Parameters.AddWithValue("modifiedBy", actor);
            }
            else
            {
                cmd.Parameters.AddWithValue("createdBy", actor);
            }

            cmd.Parameters.AddWithValue("customerId", customerId);
            cmd.Parameters.AddWithValue("contactName", contact.ContactName);
            cmd.Parameters.AddWithValue("contactTitle", (object?)contact.ContactTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("email", (object?)contact.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("phone", (object?)contact.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mobile", (object?)contact.Mobile ?? DBNull.Value);
            cmd.Parameters.AddWithValue("department", (object?)contact.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isPrimary", contact.IsPrimary);
            cmd.Parameters.AddWithValue("isActive", contact.IsActive);
            cmd.Parameters.AddWithValue("notes", (object?)contact.Notes ?? DBNull.Value);
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
