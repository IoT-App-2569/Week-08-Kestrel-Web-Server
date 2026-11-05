// ============================================================================
// ใบงานที่ 8.2: ESP32 Simulated Sensor Stream + EMA Filter (No Hardware)
// ============================================================================
#include <stdio.h>
#include <stdbool.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF Simulated Sensor Stream Starting...\n");
    printf("[SYSTEM] Streaming mock values with EMA Filter @ 115200 bps...\n");

    int raw_val = 0;
    int step = 200; 
    bool counting_up = true;

    // --- เพิ่มตัวแปรสำหรับ EMA Filter ตามกิจกรรมที่ 3 ---
    float filtered_val = 0.0f;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง (0.0 - 1.0)

    while (1) {
        // 1. ตรรกะจำลองการสร้างค่าดิบ (raw_val) ช่วง 0 - 4095
        if (counting_up) {
            raw_val += step;
            if (raw_val >= 4095) {
                raw_val = 4095;
                counting_up = false; 
            }
        } else {
            raw_val -= step;
            if (raw_val <= 0) {
                raw_val = 0;
                counting_up = true;  
            }
        }

        // 2. การกรองแบบ Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // 3. ส่งค่าที่ผ่านการกรองแล้วออกทาง Serial (แปลงกลับเป็นจำนวนเต็ม)
        printf("%d\n", (int)filtered_val);

        // 4. สตรีมเร็วขึ้นที่ 20 ครั้งต่อวินาที (20 Hz)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}