# ============================================================
#  پشتیبان‌گیری از پایگاه داده و عکس‌های کالا
#  برنامهٔ WarehouseApp
#
#  اجرای دستی:
#     powershell -ExecutionPolicy Bypass -File scripts\backup-database.ps1
#
#  اجرای خودکار هر شب ساعت ۲۳ (یک بار در خط فرمان با دسترسی مدیر):
#     schtasks /Create /TN "WarehouseApp Backup" /TR "powershell -ExecutionPolicy Bypass -File <مسیر کامل این فایل>" /SC DAILY /ST 23:00 /RL HIGHEST
# ============================================================

param(
    [string]$Server = 'localhost',
    [string]$Database = 'WarehouseAppDb',
    [string]$Folder = '',
    [string]$Uploads = '',
    [int]$KeepDays = 30
)

$ErrorActionPreference = 'Stop'

# اگر پوشه داده نشده باشد، کنار همین پروژه ساخته می‌شود
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Folder)) { $Folder = Join-Path $projectRoot 'backups' }
if ([string]::IsNullOrWhiteSpace($Uploads)) { $Uploads = Join-Path $projectRoot 'wwwroot\uploads' }

if (-not (Test-Path $Folder)) { New-Item -ItemType Directory -Path $Folder -Force | Out-Null }

$stamp = Get-Date -Format 'yyyyMMdd_HHmm'
$file = Join-Path $Folder "WarehouseAppDb_$stamp.bak"

$script:lastError = ''

function Invoke-Backup([string]$path) {
    $result = sqlcmd -S $Server -E -b -Q "BACKUP DATABASE [$Database] TO DISK = N'$path' WITH INIT, COMPRESSION" 2>&1
    $script:lastError = ($result | Out-String).Trim()
    return (($LASTEXITCODE -eq 0) -and [System.IO.File]::Exists($path))
}

Write-Host "== پشتیبان‌گیری از $Database =="
Write-Host "پوشهٔ مقصد: $Folder"

$done = $false

if (Invoke-Backup $file) {
    $done = $true
}
else {
    # ===== تلاش دوم: دادن دسترسی نوشتن به حساب سرویس پایگاه داده =====
    Write-Host 'سرویس پایگاه داده اجازهٔ نوشتن در این پوشه را ندارد — دسترسی داده می‌شود...'

    $account = ''
    try {
        $account = (sqlcmd -S $Server -E -h -1 -W -Q "SET NOCOUNT ON; SELECT TOP 1 service_account FROM sys.dm_server_services WHERE servicename LIKE 'SQL Server (%';" 2>$null |
            Where-Object { $_ -and $_.Trim() -ne '' } | Select-Object -First 1)
        if ($account) { $account = $account.Trim() }
    }
    catch {
        $account = ''
    }

    if (-not $account) { $account = 'NT SERVICE\MSSQLSERVER' }

    Write-Host "حساب سرویس: $account"
    icacls $Folder /grant "${account}:(OI)(CI)M" | Out-Null

    if (Invoke-Backup $file) {
        $done = $true
    }
}

if (-not $done) {
    Write-Error "پشتیبان‌گیری ناموفق بود`n$script:lastError`nبا دسترسی مدیر این دستور را اجرا کن:`nicacls `"$Folder`" /grant `"NT SERVICE\MSSQLSERVER:(OI)(CI)M`""
    exit 1
}

$size = [math]::Round((Get-Item $file).Length / 1MB, 1)
Write-Host "پایگاه داده پشتیبان گرفته شد ($size مگابایت)"

# ===== عکس‌های کالا =====
if (Test-Path $Uploads) {
    $target = Join-Path $Folder "uploads_$stamp"
    Copy-Item -Path $Uploads -Destination $target -Recurse -Force
    $count = (Get-ChildItem -Path $target -Recurse -File | Measure-Object).Count
    Write-Host "عکس‌ها کپی شدند ($count فایل)"
}
else {
    Write-Host 'پوشهٔ عکس‌ها خالی است یا وجود ندارد — رد شد'
}

# ===== پاک‌کردن پشتیبان‌های قدیمی =====
if ($KeepDays -gt 0) {
    $limit = (Get-Date).AddDays(-$KeepDays)

    Get-ChildItem -Path $Folder -Filter 'WarehouseAppDb_*.bak' -File |
        Where-Object { $_.CreationTime -lt $limit } |
        ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "پاک شد: $($_.Name)" }

    Get-ChildItem -Path $Folder -Filter 'uploads_*' -Directory |
        Where-Object { $_.CreationTime -lt $limit } |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force; Write-Host "پاک شد: $($_.Name)" }
}

Write-Host '== پشتیبان‌گیری با موفقیت تمام شد =='
