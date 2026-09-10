// ============================================================================
// ใบงานที่ 8.2: ESP32 Potentiometer Serial Stream (Software Simulation)
// ============================================================================
#include <stdio.h>
#include <math.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

void app_main(void)
{
    printf("\n[SYSTEM] ESP32 Potentiometer Stream (Simulated Mode) Starting...\n");
    printf("[SYSTEM] Streaming simulated values (0-4095) @ 115200 bps...\n");

    float angle = 0.0f;
    const float PI = 3.14159265f;

    while (1) {
        // 1. คำนวณจำลองค่าแอนะล็อก (0 - 4095) ด้วยคลื่นรูปไซน์ (Sine Wave)
        // sin(angle) มีค่าระหว่าง -1.0 ถึง 1.0
        // ปรับสเกลให้อยู่ในช่วง 0.0 ถึง 1.0 แล้วคูณด้วย 4095
        float sin_val = (sinf(angle) + 1.0f) / 2.0f;
        int simulated_adc = (int)(sin_val * 4095.0f);

        // 2. สตรีมตัวเลขออกทาง UART (stdout) ปิดท้ายด้วย \n
        printf("%d\n", simulated_adc);

        // 3. เพิ่มมุมเพื่อเปลี่ยนค่าในรอบถัดไป (ปรับ 0.05f เพื่อเพิ่ม/ลดความเร็วการเปลี่ยนแปลง)
        angle += 0.05f;
        if (angle >= 2.0f * PI) {
            angle -= 2.0f * PI;
        }

        // หน่วงเวลา 50ms (สตรีม 20 ครั้งต่อวินาที)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}