#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_adc/adc_oneshot.h"

// กำหนด Channel ADC
#define ADC_CHANNEL_A ADC_CHANNEL_6 // ADC1_CH6 คือ GPIO 34 (Potentiometer จริง)
#define ADC_CHANNEL_B ADC_CHANNEL_7 // ADC1_CH7 คือ GPIO 35 (Optional: Sensor 2 / LDR)

// ตั้งค่า 1 หากมีการต่อเซนเซอร์ตัวที่ 2 หรือ 0 หากต้องการส่งเพียงตัวเดียวแล้วให้เซิร์ฟเวอร์ C# จำลองช่อง B
#define DUAL_SENSOR_ENABLED 0

void app_main(void) {
    adc_oneshot_unit_handle_t adc_handle;
    adc_oneshot_unit_init_cfg_t init_config = {
        .unit_id = ADC_UNIT_1,
    };
    adc_oneshot_new_unit(&init_config, &adc_handle);

    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    adc_oneshot_config_channel(adc_handle, ADC_CHANNEL_A, &config);

#if DUAL_SENSOR_ENABLED
    adc_oneshot_config_channel(adc_handle, ADC_CHANNEL_B, &config);
#endif

    int raw_a = 0;
    int raw_b = 0;

    while (1) {
        adc_oneshot_read(adc_handle, ADC_CHANNEL_A, &raw_a);

#if DUAL_SENSOR_ENABLED
        adc_oneshot_read(adc_handle, ADC_CHANNEL_B, &raw_b);
        // ส่งแบบคั่นด้วยจุลภาค: rawA,rawB
        printf("%d,%d\n", raw_a, raw_b);
#else
        // ส่งค่าแชนแนล A ตัวเดียว (Kestrel จะสร้างสัญญาณ Sine จำลองสำหรับแชนแนล B ให้อัตโนมัติ)
        printf("%d\n", raw_a);
#endif

        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
