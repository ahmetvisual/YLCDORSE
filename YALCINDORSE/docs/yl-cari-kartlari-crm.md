# YL Cari Kartlari CRM Semasi

Bu dokuman, `Cari Kartlar` modulu icin PostgreSQL tarafinda kullanilacak tablo, indeks ve kod uretim yapisini tanimlar.

ERP tarafinda yaygin uygulama su sekildedir:

- Musteri ana karti ayri bir tabloda tutulur.
- Ilgili kisiler cocuk tabloda tutulur.
- Kod alani manuel degil, sistem tarafindan uretilir.
- Satis temsilcisi bir kullanici kaydina baglanir.
- Audit alanlari (`CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy`) standart olarak tutulur.
- Her musteride yalnizca bir aktif `primary` ilgili kisi bulunabilir.

Bu implementasyonda, ekran ihtiyaci nedeniyle musteride opsiyonel `Notes` alani da eklenmistir.

## SQL

```sql
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
```

## Notlar

- `CurrentBalance` alaninin finans hareketlerinden beslenmesi tavsiye edilir. Bu ilk surumde ekran tarafinda read-only gosterilecektir.
- `SalesRepId`, mevcut `YLUsers` tablosuna baglidir.
- `IsActive` alanlari soft delete / pasiflestirme mantigi icin korunmustur.
