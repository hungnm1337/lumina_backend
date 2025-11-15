# DATABASE MIGRATION COMMANDS

## 🔧 Option 1: Automatic Migration (Recommended)

### Windows (PowerShell/CMD)

```powershell
# Navigate to DataLayer project
cd D:\Lumina_Toeic\lumina_backend\DataLayer

# Apply migration
dotnet ef database update --startup-project ..\lumina\lumina.csproj

# Verify migration
dotnet ef migrations list --startup-project ..\lumina\lumina.csproj
```

### Expected Output

```
Applying migration '20251115000000_AddQuotaTracking'.
Done.
```

---

## 🔧 Option 2: Manual SQL Script

If automatic migration fails, run this SQL manually:

```sql
-- Add quota tracking columns to Users table
ALTER TABLE Users ADD
    MonthlyReadingAttempts INT NOT NULL DEFAULT 0,
    MonthlyListeningAttempts INT NOT NULL DEFAULT 0,
    LastQuotaReset DATETIME2 NOT NULL DEFAULT GETDATE();

-- Add index for performance
CREATE NONCLUSTERED INDEX IX_Subscriptions_UserId_Status
ON Subscriptions(UserId, Status)
WHERE Status = 'Active';

-- Verify columns were added
SELECT TOP 1
    UserId,
    MonthlyReadingAttempts,
    MonthlyListeningAttempts,
    LastQuotaReset
FROM Users;
```

---

## 📊 Post-Migration: Create Premium Package

```sql
-- Insert Premium package
INSERT INTO Packages (PackageName, Price, DurationInDays, IsActive)
VALUES ('Premium Monthly', 299000, 30, 1);

-- Get the PackageId (you'll need this for frontend)
SELECT PackageId, PackageName, Price
FROM Packages
WHERE PackageName = 'Premium Monthly';
```

**Example Output:**

```
PackageId | PackageName       | Price
----------|-------------------|-------
1         | Premium Monthly   | 299000
```

⚠️ **Important:** Save this `PackageId` and update it in:

- `lumina_frontend/lumina/src/app/Views/User/upgrade-modal/upgrade-modal.component.ts`

---

## 🧪 Verify Migration Success

### 1. Check Users table structure

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
  AND COLUMN_NAME IN ('MonthlyReadingAttempts', 'MonthlyListeningAttempts', 'LastQuotaReset');
```

**Expected Result:**

```
COLUMN_NAME                 | DATA_TYPE | IS_NULLABLE
----------------------------|-----------|------------
MonthlyReadingAttempts      | int       | NO
MonthlyListeningAttempts    | int       | NO
LastQuotaReset              | datetime2 | NO
```

### 2. Check index was created

```sql
SELECT name, type_desc
FROM sys.indexes
WHERE object_id = OBJECT_ID('Subscriptions')
  AND name = 'IX_Subscriptions_UserId_Status';
```

**Expected Result:**

```
name                              | type_desc
----------------------------------|------------
IX_Subscriptions_UserId_Status    | NONCLUSTERED
```

### 3. Test with sample data

```sql
-- Insert test user (if not exists)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'test@lumina.com')
BEGIN
    INSERT INTO Users (Email, FullName, RoleId, IsActive)
    VALUES ('test@lumina.com', 'Test User', 4, 1);
END

-- Verify quota columns have default values
SELECT UserId, Email, MonthlyReadingAttempts, MonthlyListeningAttempts, LastQuotaReset
FROM Users
WHERE Email = 'test@lumina.com';
```

**Expected Result:**

```
UserId | Email              | MonthlyReadingAttempts | MonthlyListeningAttempts | LastQuotaReset
-------|--------------------|-----------------------|--------------------------|------------------
1      | test@lumina.com    | 0                     | 0                        | 2025-11-15 10:30:00
```

---

## 🐛 Troubleshooting Migration

### Error: "Cannot insert NULL into column"

**Solution:** The migration already includes DEFAULT values. If this occurs:

```sql
ALTER TABLE Users ALTER COLUMN MonthlyReadingAttempts INT NOT NULL;
UPDATE Users SET MonthlyReadingAttempts = 0 WHERE MonthlyReadingAttempts IS NULL;
```

### Error: "Index already exists"

**Solution:** Drop and recreate:

```sql
DROP INDEX IX_Subscriptions_UserId_Status ON Subscriptions;
GO
CREATE NONCLUSTERED INDEX IX_Subscriptions_UserId_Status
ON Subscriptions(UserId, Status)
WHERE Status = 'Active';
```

### Error: "EF Core tools not found"

**Solution:** Install tools:

```powershell
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

---

## 🔄 Rollback Migration (If Needed)

```powershell
# Rollback to previous migration
cd D:\Lumina_Toeic\lumina_backend\DataLayer
dotnet ef database update 20251111110549_intial --startup-project ..\lumina\lumina.csproj

# Or rollback all migrations
dotnet ef database update 0 --startup-project ..\lumina\lumina.csproj
```

**Manual Rollback SQL:**

```sql
-- Remove index
DROP INDEX IX_Subscriptions_UserId_Status ON Subscriptions;

-- Remove columns
ALTER TABLE Users DROP COLUMN MonthlyReadingAttempts;
ALTER TABLE Users DROP COLUMN MonthlyListeningAttempts;
ALTER TABLE Users DROP COLUMN LastQuotaReset;
```

---

## ✅ Migration Checklist

- [ ] Backup database before migration
- [ ] Run migration command
- [ ] Verify columns were added
- [ ] Verify index was created
- [ ] Create Premium package
- [ ] Test with sample user
- [ ] Update frontend with PackageId

---

## 📝 Notes

- **Migration is idempotent:** Safe to run multiple times
- **Default values:** All existing users get 0 quota and current date
- **Performance:** Index on Subscriptions improves query speed
- **Rollback:** Safe to rollback if needed (see section above)

---

**Migration completed?** ✅ Move to next step: PayOS configuration
