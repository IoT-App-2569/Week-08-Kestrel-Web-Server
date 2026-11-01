$Host.UI.RawUI.WindowTitle = "Lab 8.2 Serial Monitor - COM3"
$port = [System.IO.Ports.SerialPort]::new("COM3", 115200)
$port.ReadTimeout = 2000
$port.Open()
Write-Host "ESP-IDF Lab 8.2 | COM3 @ 115200 | ADC GPIO34" -ForegroundColor Cyan
try {
    1..20 | ForEach-Object { Write-Host $port.ReadLine().Trim() }
} finally {
    $port.Close()
}
Write-Host "Hardware capture complete" -ForegroundColor Green
