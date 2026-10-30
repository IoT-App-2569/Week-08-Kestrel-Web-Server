#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM (ADC1)
#endif

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...\n");

    // 1. ตั้งค่า ADC Unit 1
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. กำหนดความละเอียด 12 บิต (0-4095) และ Attenuation 12dB (รองรับช่วง 0 - 3.3V)
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));

    printf("[SYSTEM] ADC Initialized. Streaming EMA filtered values @ 115200 bps...\n");

    int raw_val = 0;
    float filtered_val = 0.0f;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง EMA

    while (1) {
        // อ่านค่า ADC แบบ Oneshot
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));

        // การกรองสัญญาณรบกวน Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // สตรีมค่าตัวเลขจำนวนเต็มออกทาง Serial Monitor
        printf("%d\n", (int)filtered_val);

        // หน่วงเวลา 50ms ส่งข้อมูลความถี่ 20 ครั้งต่อวินาที (20 Hz)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
