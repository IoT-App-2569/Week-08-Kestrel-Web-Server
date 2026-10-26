# รายงานผลการทดลองที่ 8.4: แดชบอร์ดมาตรวัดความเร็ว SVG และสนามทดลองสร้างสรรค์

**ชื่อ-นามสกุล:** นาย พรพรม สุขใจมิตร

**รหัสนักศึกษา:** 67030323

---

## กิจกรรมที่ 3: ทดสอบหมุนหน้าปัด Speedometer SVG
*(แนบภาพหน้าจอเบราว์เซอร์ที่แสดงผลหน้าปัด Speedometer SVG ขณะทำงาน)*

![alt text](/Report/img/image-9.png)

---

## สนามทดลองสร้างสรรค์ (Creative Playground)
**รูปแบบที่เลือก:** [ ] A. VU Meter   [ / ] B. 7-Segment   [ ] C. Liquid Tank

*(แนบภาพหน้าจอเบราว์เซอร์ที่แสดงผลหน้าปัดในรูปแบบที่เลือก)*

![alt text](/Report/img/image-10.png)

---

## หลักฐานซอร์สโค้ด (Source Code)
*(แนบโค้ด `index.html` ในส่วนของการวาด SVG และ JavaScript ที่นำมาปรับแต่ง)*

```
<div class="container">
    <h1>📟 Retro 7-Segment Display</h1>
    <div class="subtitle">Creative Playground</div>
    
    <svg width="230" height="130" viewBox="0 0 230 130" id="seven-seg-board">
        <rect x="5" y="5" width="220" height="120" rx="12" fill="#0b0f19" stroke="#1e293b" stroke-width="2"/>
        
        <!-- หลักสิบ (Tens Digit) -->
        <g id="seg-digit1" transform="translate(25, 15)">
            <polygon class="seg a" points="14,12 19,7 41,7 46,12 41,17 19,17"/>
            <polygon class="seg b" points="48,14 53,19 53,41 48,46 43,41 43,19"/>
            <polygon class="seg c" points="48,54 53,59 53,81 48,86 43,81 43,59"/>
            <polygon class="seg d" points="14,88 19,83 41,83 46,88 41,93 19,93"/>
            <polygon class="seg e" points="12,54 17,59 17,81 12,86 7,81 7,59"/>
            <polygon class="seg f" points="12,14 17,19 17,41 12,46 7,41 7,19"/>
            <polygon class="seg g" points="14,50 19,45 41,45 46,50 41,55 19,55"/>
        </g>

        <!-- หลักหน่วย (Ones Digit) -->
        <g id="seg-digit2" transform="translate(100, 15)">
            <polygon class="seg a" points="14,12 19,7 41,7 46,12 41,17 19,17"/>
            <polygon class="seg b" points="48,14 53,19 53,41 48,46 43,41 43,19"/>
            <polygon class="seg c" points="48,54 53,59 53,81 48,86 43,81 43,59"/>
            <polygon class="seg d" points="14,88 19,83 41,83 46,88 41,93 19,93"/>
            <polygon class="seg e" points="12,54 17,59 17,81 12,86 7,81 7,59"/>
            <polygon class="seg f" points="12,14 17,19 17,41 12,46 7,41 7,19"/>
            <polygon class="seg g" points="14,50 19,45 41,45 46,50 41,55 19,55"/>
        </g>
        <text x="175" y="90" fill="#00ffcc" font-family="'Courier New', monospace" font-size="28" font-weight="bold" opacity="0.85">%</text>
    </svg>
</div>

<script>
    // การตั้งค่าความจริง (Truth Table) สำหรับ 7-Segment (0-9)
    const SEGMENT_MAP = {
        0: [1, 1, 1, 1, 1, 1, 0], 1: [0, 1, 1, 0, 0, 0, 0], 2: [1, 1, 0, 1, 1, 0, 1],
        3: [1, 1, 1, 1, 0, 0, 1], 4: [0, 1, 1, 0, 0, 1, 1], 5: [1, 0, 1, 1, 0, 1, 1],
        6: [1, 0, 1, 1, 1, 1, 1], 7: [1, 1, 1, 0, 0, 0, 0], 8: [1, 1, 1, 1, 1, 1, 1], 9: [1, 1, 1, 1, 0, 1, 1]
    };
    const SEG_NAMES = ['a', 'b', 'c', 'd', 'e', 'f', 'g'];

    function setDigit(digitGroupId, num) {
        const group = document.getElementById(digitGroupId);
        if (!group) return;
        const pattern = SEGMENT_MAP[num] || [0, 0, 0, 0, 0, 0, 0];
        SEG_NAMES.forEach((segName, index) => {
            const seg = group.querySelector(`.${segName}`);
            if (seg) pattern[index] ? seg.classList.add('active') : seg.classList.remove('active');
        });
    }

    function update7Segment(percentage) {
        const val = Math.min(99, Math.max(0, Math.round(percentage)));
        setDigit('seg-digit1', Math.floor(val / 10)); // หลักสิบ
        setDigit('seg-digit2', val % 10);             // หลักหน่วย
    }
```

---