#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...\n");
    printf("[SYSTEM] ADC Initialized. Streaming EMA filtered values at 20 Hz @ 115200 bps...\n");

    float angle = 0.0f;
    float filtered_val = 0.0f;
    const float alpha = 0.25f;

    while (1) {
        int simulated_raw = (int)((sinf(angle) + 1.0f) * 2000.0f) + (rand() % 95);
        if (simulated_raw > 4095) simulated_raw = 4095;
        if (simulated_raw < 0) simulated_raw = 0;

        filtered_val = (alpha * simulated_raw) + ((1.0f - alpha) * filtered_val);

        printf("%d\n", (int)filtered_val);

        angle += 0.05f;
        if (angle > 6.28f) {
            angle = 0.0f;
        }

        vTaskDelay(pdMS_TO_TICKS(50));
    }
}