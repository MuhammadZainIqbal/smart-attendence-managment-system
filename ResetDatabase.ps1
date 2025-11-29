# ========================================
# Database Reset Script for AMS
# ========================================
# This script completely resets the database to a fresh state
# Run this from the project root directory

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Database Reset Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Stop LocalDB instance
Write-Host "Step 1: Stopping LocalDB instance..." -ForegroundColor Yellow
sqllocaldb stop mssqllocaldb
Start-Sleep -Seconds 2
Write-Host "LocalDB stopped." -ForegroundColor Green
Write-Host ""

# Delete LocalDB instance
Write-Host "Step 2: Deleting LocalDB instance..." -ForegroundColor Yellow
sqllocaldb delete mssqllocaldb
Start-Sleep -Seconds 1
Write-Host "LocalDB instance deleted." -ForegroundColor Green
Write-Host ""

# Create fresh LocalDB instance
Write-Host "Step 3: Creating fresh LocalDB instance..." -ForegroundColor Yellow
sqllocaldb create mssqllocaldb
Start-Sleep -Seconds 1
Write-Host "Fresh LocalDB instance created." -ForegroundColor Green
Write-Host ""

# Start LocalDB instance
Write-Host "Step 4: Starting LocalDB instance..." -ForegroundColor Yellow
sqllocaldb start mssqllocaldb
Start-Sleep -Seconds 2
Write-Host "LocalDB started." -ForegroundColor Green
Write-Host ""

# Delete any orphaned database files
Write-Host "Step 5: Cleaning up orphaned database files..." -ForegroundColor Yellow
$userProfile = $env:USERPROFILE
$possiblePaths = @(
    "$userProfile\AttendenceManagementSystem.mdf",
    "$userProfile\AttendenceManagementSystem_log.ldf",
    "$userProfile\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\mssqllocaldb\AttendenceManagementSystem.mdf",
    "$userProfile\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\mssqllocaldb\AttendenceManagementSystem_log.ldf"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "  Deleted: $path" -ForegroundColor Gray
    }
}
Write-Host "Database files cleaned." -ForegroundColor Green
Write-Host ""

# Delete migration files
Write-Host "Step 6: Deleting migration files..." -ForegroundColor Yellow
$migrationsPath = ".\Migrations\*.cs"
if (Test-Path ".\Migrations") {
    Remove-Item $migrationsPath -Force -ErrorAction SilentlyContinue
    Write-Host "Migration files deleted." -ForegroundColor Green
} else {
    Write-Host "No migration files found." -ForegroundColor Gray
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Database Reset Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps (Run in Package Manager Console):" -ForegroundColor Yellow
Write-Host "  1. Add-Migration FreshStart" -ForegroundColor White
Write-Host "  2. Update-Database" -ForegroundColor White
Write-Host ""
Write-Host "Then start your application and test signup." -ForegroundColor Yellow
Write-Host ""
