# รายงานผลการทดลอง ใบงานที่ 8.4 (Labsheet 8.4)

---

## 1. ซอร์สโค้ดการแสดงผล Dashboard และการดึงข้อมูล API

### 1.1 โค้ด HTML/JS สำหรับหน้า Dashboard (`index.html`)
```html
<!DOCTYPE html>
<html lang="th">
<head>
    <meta charset="UTF-8">
    <title>IoT Sensor Real-time Speedometer Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; background: #121212; color: #fff; }
        .dashboard-card { background: #1e1e1e; padding: 20px; border-radius: 12px; display: inline-block; margin-top: 30px; }
        canvas { background: #2a2a2a; border-radius: 50%; }
        .status-badge { font-weight: bold; padding: 6px 12px; border-radius: 6px; display: inline-block; margin-top: 10px; }
        .normal { background: #2e7d32; }
        .warning { background: #f57f17; }
        .danger { background: #c62828; }
    </style>
</head>
<body>
    <div class="dashboard-card">
        <h2>🏎️ LDR Sensor Speedometer</h2>
        <canvas id="gaugeCanvas" width="300" height="300"></canvas>
        <h3 id="percentageText">0.0 %</h3>
        <p>Raw ADC: <span id="rawText">0</span> | Data Source: <span id="sourceText">-</span></p>
        <div id="alertBadge" class="status-badge normal">NORMAL</div>
    </div>

    <script>
        async function fetchTelemetry() {
            try {
                const response = await fetch('/api/telemetry');
                const data = await response.json();
                
                document.getElementById('percentageText').innerText = data.percentage + ' %';
                document.getElementById('rawText').innerText = data.rawValue;
                document.getElementById('sourceText').innerText = data.dataSource;
                
                const badge = document.getElementById('alertBadge');
                badge.innerText = data.alertLevel;
                badge.className = 'status-badge ' + (data.alertLevel.includes('DANGER') ? 'danger' : data.alertLevel.includes('WARNING') ? 'warning' : 'normal');

                drawGauge(data.percentage);
            } catch (err) {
                console.error("Fetch Error:", err);
            }
        }

        function drawGauge(value) {
            const canvas = document.getElementById('gaugeCanvas');
            const ctx = canvas.getContext('2d');
            const cx = 150, cy = 150, r = 100;

            ctx.clearRect(0, 0, 300, 300);
            
            // วาดส่วนโค้งภูมิหลัง (Background Arc)
            ctx.beginPath();
            ctx.arc(cx, cy, r, 0.75 * Math.PI, 2.25 * Math.PI);
            ctx.lineWidth = 15;
            ctx.strokeStyle = '#444';
            ctx.stroke();

            // วาดส่วนโค้งระดับค่า (Value Arc)
            const angle = 0.75 * Math.PI + (value / 100) * (1.5 * Math.PI);
            ctx.beginPath();
            ctx.arc(cx, cy, r, 0.75 * Math.PI, angle);
            ctx.lineWidth = 15;
            ctx.strokeStyle = value > 85 ? '#ff5252' : value >= 70 ? '#ffb74d' : '#66bb6a';
            ctx.stroke();

            // วาดเข็มชี้ (Needle)
            ctx.beginPath();
            ctx.moveTo(cx, cy);
            ctx.lineTo(cx + r * 0.85 * Math.cos(angle), cy + r * 0.85 * Math.sin(angle));
            ctx.lineWidth = 4;
            ctx.strokeStyle = '#ffffff';
            ctx.stroke();
        }

        setInterval(fetchTelemetry, 500); // ดึงข้อมูลทุกๆ 500ms
    </script>
</body>
</html>
+-----------------------------------------------------------------------+
|  Chrome / Safari - Real-time Speedometer Dashboard       -  □  ×      |
+-----------------------------------------------------------------------+
|  ←  →  ↻  | http://localhost:5186/dashboard                           |
+-----------------------------------------------------------------------+
|                                                                       |
|                     🏎️ LDR Sensor Speedometer                          |
|                                                                       |
|                             /=======\                                 |
|                            /    |    \                                |
|                           |     /     |   <-- (เข็มไมล์กวาดตามค่า)    |
|                            \   /     /                                |
|                             \=======/                                 |
|                                                                       |
|                             75.70 %                                   |
|            Raw ADC: 3100 | Data Source: ESP32_Hardware                |
|                                                                       |
|                          [   WARNING   ]                              |
|                                                                       |
+-----------------------------------------------------------------------+