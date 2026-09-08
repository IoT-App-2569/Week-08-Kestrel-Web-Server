# ============================================================================
#  สคริปต์ Build เฟิร์มแวร์ ESP32 ด้วย ESP-IDF v6.1 แบบ Native (ไม่ใช้ Docker)
#  ----------------------------------------------------------------------------
#  วิธีใช้ :
#      .\build.ps1                  # build อย่างเดียว
#      .\build.ps1 -Target esp32c6  # เปลี่ยนชิปเป้าหมายเป็น ESP32-C6
#      .\build.ps1 -Clean           # ล้าง build เก่าทิ้งก่อน
#
#  หมายเหตุ : ไฟล์นี้ต้องบันทึกเป็น UTF-8 with BOM เท่านั้น
#             เพราะ Windows PowerShell 5.1 จะอ่านไฟล์ที่ไม่มี BOM เป็นรหัส ANSI
#             ทำให้ตัวอักษรไทยเพี้ยนจนสคริปต์ทำงานไม่ได้
# ============================================================================
param(
    [string]$Target = 'esp32',
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

# พาธของสคริปต์เปิดใช้งาน ESP-IDF ที่ติดตั้งผ่าน ESP-IDF Installation Manager (EIM)
$IdfProfile = 'C:\Espressif\tools\Microsoft.v6.1.PowerShell_profile.ps1'

if (-not (Test-Path $IdfProfile)) {
    Write-Host 'ไม่พบสคริปต์เปิดใช้งาน ESP-IDF ที่ :' -ForegroundColor Red
    Write-Host "    $IdfProfile" -ForegroundColor Red
    Write-Host ''
    Write-Host 'ให้เปิดโปรแกรม ESP-IDF Installation Manager (EIM) ตรวจสอบว่าติดตั้ง v6.1 ครบถ้วน'
    Write-Host 'หรือแก้ตัวแปร IdfProfile ในไฟล์นี้ให้ตรงกับเวอร์ชันที่ติดตั้งไว้'
    exit 1
}

Write-Host '[1/3] เปิดใช้งานสภาพแวดล้อม ESP-IDF v6.1 ...' -ForegroundColor Cyan
. $IdfProfile *> $null
Write-Host "      IDF_PATH = $env:IDF_PATH" -ForegroundColor DarkGray

Set-Location $PSScriptRoot

if ($Clean) {
    Write-Host '[*] ล้างไฟล์ build เก่า ...' -ForegroundColor Yellow
    idf.py fullclean
}

Write-Host "[2/3] ตั้งค่าชิปเป้าหมายเป็น $Target ..." -ForegroundColor Cyan
idf.py set-target $Target

Write-Host '[3/3] กำลังคอมไพล์ ...' -ForegroundColor Cyan
idf.py build

if ($LASTEXITCODE -eq 0) {
    Write-Host ''
    Write-Host 'Build สำเร็จ! ขั้นถัดไปสั่ง flash ด้วย :' -ForegroundColor Green
    Write-Host '    .\flash.ps1 -Monitor' -ForegroundColor Green
}
