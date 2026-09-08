// ============================================================================
// ใบงานที่ 8.2: ESP32 Simulated Serial Stream (Virtual ADC)
// ============================================================================
#include <stdio.h>
#include <math.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Simulated ADC Stream Starting...\n");
    printf("[SYSTEM] Streaming simulated ADC values (0-4095) @ 115200 bps...\n");

    float angle = 0.0f;
    float filtered_val = 0.0f;
    const float alpha = 0.25f; // EMA Filter coefficient

    while (1) {
        // จำลองค่า ADC (0-4095) ขึ้น-ลงตามคลื่น Sine ให้เหมือนการหมุนปรับ Volume
        int raw_val = (int)((sin(angle) + 1.0f) * 2047.5f);
        angle += 0.05f;

        // การกรองสัญญาณแบบ Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // สตรีมตัวเลขออกทาง Serial
        printf("%d\n", (int)filtered_val);

        vTaskDelay(pdMS_TO_TICKS(50));
    }
}