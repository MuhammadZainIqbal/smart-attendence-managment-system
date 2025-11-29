# Database Reset Scripts

## Purpose
These scripts completely reset your LocalDB database to a fresh state during development.

## What They Do
1. Stop LocalDB instance
2. Delete LocalDB instance completely
3. Create a fresh LocalDB instance
4. Start LocalDB
5. Delete any orphaned database files
6. Delete all migration files

## Usage

### Option 1: PowerShell Script (Recommended)

**In PowerShell (as Administrator):**
```powershell
cd "C:\Users\Zain Iqbal\Desktop\AttendenceManagementSystem\AttendenceManagementSystem"
.\ResetDatabase.ps1
```

If you get an execution policy error, run this first:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\ResetDatabase.ps1
```

### Option 2: Batch File

**Double-click** `ResetDatabase.bat` or run in Command Prompt:
```cmd
ResetDatabase.bat
```

## After Running the Script

**In Visual Studio Package Manager Console, run:**
```powershell
Add-Migration FreshStart
Update-Database
```

## Then Test Your App
1. Press F5 to start the application
2. Go to `/Account/SignUp`
3. Register a new institute/admin

---

## ⚠️ Warning
**Only use during development!** This will delete ALL data in your database.

## Troubleshooting

### "LocalDB instance not found"
- This is normal if it's the first run or already deleted
- The script will create it

### "Cannot delete file - Access Denied"
- Close Visual Studio
- Stop IIS Express
- Run the script again

### Still getting duplicate key errors?
1. Run this script
2. Close Visual Studio completely
3. Reopen Visual Studio
4. Run `Add-Migration` and `Update-Database`
5. Test signup again
