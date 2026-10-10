<#
    پاک‌کردن دادهٔ آزمایشی از پایگاه دادهٔ برنامه.

    بدون سوئیچ:
        فقط دادهٔ کاری پاک می‌شود — سفارش‌ها، اقلام سفارش، حرکات انبار،
        چک و سفته، بدهی و طلب، هزینه‌ها، پیگیری‌ها، فرصت‌های فروش،
        پیامک‌ها و تاریخچهٔ عملیات.
        کالا، دسته، انبار و طرف حساب دست‌نخورده می‌مانند.

    -All:
        کالا، دسته، انبار، طرف حساب و عکس‌های کالا هم پاک می‌شوند.

    -Force:
        بدون پرسش تأیید.

    همیشه می‌مانند: حساب کارمندان، نقش‌ها و دسترسی‌ها، کانال‌های فروش و تنظیمات.

    نمونهٔ اجرا (پنجرهٔ فرمان در پوشهٔ پروژه):

        powershell -ExecutionPolicy Bypass -File scripts\reset-data.ps1

        powershell -ExecutionPolicy Bypass -File scripts\reset-data.ps1 -All

    پیشنهاد: پیش از اجرا، از صفحهٔ «پشتیبان‌گیری» یک نسخهٔ پشتیبان بگیر.
#>

[CmdletBinding()]
param(
    [switch]$All,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$server = 'localhost'
$database = 'WarehouseAppDb'

# ترتیب مهم است: هر جدول پیش از جدولی که به آن وابسته است پاک می‌شود
$work = @(
    'SalesOrderItems',
    'SalesOrders',
    'StockMovements',
    'Cheques',
    'Debts',
    'Expenses',
    'CrmInteractions',
    'CrmOpportunities',
    'SmsMessages',
    'AuditLogs'
)

$master = @(
    'ProductImages',
    'Products',
    'Categories',
    'Warehouses',
    'Parties'
)

function Invoke-Sql([string]$sql) {
    $out = sqlcmd -S $server -d $database -E -I -b -Q $sql 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host $out
        throw "دستور پایگاه داده با خطا برگشت (کد $LASTEXITCODE)"
    }
    return $out
}

function Get-RowCount([string]$table) {
    $out = sqlcmd -S $server -d $database -E -I -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [$table];" 2>&1
    foreach ($line in $out) {
        if ($line -match '^\s*\d+\s*$') { return [int]$line.Trim() }
    }
    return 0
}

$targets = $work
if ($All) { $targets = $work + $master }

Write-Host ''
Write-Host '  پاک‌کردن دادهٔ آزمایشی' -ForegroundColor Cyan
Write-Host "  پایگاه داده: $database روی $server"
Write-Host ''
Write-Host '  این جدول‌ها پاک می‌شوند:'

$before = @{}
foreach ($table in $targets) {
    $count = Get-RowCount $table
    $before[$table] = $count
    Write-Host ("    {0,-20} {1,8} ردیف" -f $table, $count)
}

if (-not $All) {
    Write-Host ''
    Write-Host '  کالا، دسته، انبار و طرف حساب دست‌نخورده می‌مانند.' -ForegroundColor DarkGray
    Write-Host '  اگر می‌خواهی آن‌ها هم پاک شوند، با سوئیچ -All اجرا کن.' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '  حساب کارمندان، نقش‌ها، کانال‌های فروش و تنظیمات همیشه می‌مانند.' -ForegroundColor DarkGray
Write-Host ''

if (-not $Force) {
    $answer = Read-Host '  مطمئنی؟ برای ادامه کلمهٔ بله را بنویس'
    if ($answer -ne 'بله') {
        Write-Host '  لغو شد — هیچ چیزی پاک نشد.' -ForegroundColor Yellow
        return
    }
}

Write-Host ''
Write-Host '  در حال پاک‌کردن...' -ForegroundColor Cyan

$sql = "SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;`n"

foreach ($table in $targets) {
    # زیردسته‌ها پیش از دسته‌های اصلی
    if ($table -eq 'Categories') {
        $sql += "DELETE FROM [Categories] WHERE [ParentId] IS NOT NULL;`n"
    }
    $sql += "DELETE FROM [$table];`n"
}

$sql += "COMMIT TRANSACTION;"

Invoke-Sql $sql | Out-Null

# شمارندهٔ شناسه‌ها به صفر برمی‌گردد تا کالا و سفارش تازه از ۱ شروع شود
foreach ($table in $targets) {
    sqlcmd -S $server -d $database -E -I -Q "DBCC CHECKIDENT('$table', RESEED, 0)" 2>&1 | Out-Null
}

Write-Host ''
Write-Host '  نتیجه:' -ForegroundColor Green

foreach ($table in $targets) {
    $after = Get-RowCount $table
    Write-Host ("    {0,-20} {1,8} → {2,8}" -f $table, $before[$table], $after)
}

Write-Host ''
Write-Host '  تمام شد. پایگاه داده آمادهٔ استفادهٔ واقعی است.' -ForegroundColor Green
Write-Host ''
