# ============================================================================
#  สคริปต์ Flash + Monitor เฟิร์มแวร์ลงบอร์ด ESP32 (ESP-IDF v6.1 แบบ Native)
#  ----------------------------------------------------------------------------
#  วิธีใช้ :
#      .\flash.ps1                      # ใช้พอร์ตค่าเริ่มต้น COM3
#      .\flash.ps1 -Monitor             # flash แล้วเปิดหน้าจอ monitor ต่อทันที
#      .\flash.ps1 -Port COM5           # ระบุพอร์ตอื่น
#      .\flash.ps1 -Auto -Monitor       # ให้ค้นหาพอร์ตอัตโนมัติ
#
#  ก่อนไปทำใบงานที่ 8.3 ต้องกด Ctrl + ] ออกจาก monitor ก่อนเสมอ
#  เพื่อปลดล็อกพอร์ต COM ให้ฝั่ง C# เปิดใช้งานได้
#
#  หมายเหตุ : ไฟล์นี้ต้องบันทึกเป็น UTF-8 with BOM เท่านั้น
# ============================================================================
param(
    [string]$Port = 'COM3',
    [switch]$Auto,
    [switch]$Monitor
)

$ErrorActionPreference = 'Stop'

$IdfProfile = 'C:\Espressif\tools\Microsoft.v6.1.PowerShell_profile.ps1'

if (-not (Test-Path $IdfProfile)) {
    Write-Host 'ไม่พบสคริปต์เปิดใช้งาน ESP-IDF ที่ :' -ForegroundColor Red
    Write-Host "    $IdfProfile" -ForegroundColor Red
    exit 1
}

# ---- ค้นหาพอร์ตอัตโนมัติเมื่อสั่ง -Auto ----
if ($Auto) {
    $ports = @([System.IO.Ports.SerialPort]::GetPortNames() | Where-Object { $_ -ne 'COM1' })

    if ($ports.Count -eq 0) {
        Write-Host 'ไม่พบพอร์ต COM ใดๆ ในระบบ' -ForegroundColor Red
        Write-Host '  1. ตรวจสอบว่าเสียบสาย USB ของบอร์ด ESP32 แล้ว'
        Write-Host '  2. ต้องเป็นสาย USB ที่มีสายข้อมูล ไม่ใช่สายชาร์จอย่างเดียว'
        Write-Host '  3. เปิด Device Manager ดูหัวข้อ Ports (COM & LPT) ว่ามีไดรเวอร์ครบหรือไม่'
        Write-Host '     (ชิปที่พบบ่อยคือ CP210x หรือ CH340)'
        exit 1
    }

    $Port = $ports[0]
    Write-Host "ตรวจพบพอร์ต : $($ports -join ', ')  ->  เลือกใช้ $Port" -ForegroundColor Yellow
}

Write-Host '[1/2] เปิดใช้งานสภาพแวดล้อม ESP-IDF v6.1 ...' -ForegroundColor Cyan
. $IdfProfile *> $null

Set-Location $PSScriptRoot

Write-Host "[2/2] กำลัง flash ลงบอร์ดผ่าน $Port ..." -ForegroundColor Cyan

if ($Monitor) {
    # flash แล้วเปิด monitor ต่อทันที (ออกด้วย Ctrl + ])
    idf.py -p $Port flash monitor
}
else {
    idf.py -p $Port flash

    if ($LASTEXITCODE -eq 0) {
        Write-Host ''
        Write-Host 'Flash สำเร็จ! ดูค่าที่สตรีมออกมาได้ด้วย :' -ForegroundColor Green
        Write-Host "    .\flash.ps1 -Port $Port -Monitor" -ForegroundColor Green
        Write-Host 'อย่าลืมกด Ctrl + ] ออกจาก monitor ก่อนรันโปรแกรม C# ในใบงาน 8.3' -ForegroundColor Yellow
    }
}
