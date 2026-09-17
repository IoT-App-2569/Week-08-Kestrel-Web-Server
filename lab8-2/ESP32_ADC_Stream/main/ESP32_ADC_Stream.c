#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM
#endif

void app_main(void)
{
    // 1. กำหนดค่าเริ่มต้น ADC Unit 1
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. ตั้งค่า Channel 12-bit (0-4095) และ 12dB Attenuation
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));

    // ตัวแปรสำหรับ EMA Filter
    int raw_val = 0;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง (0.0 - 1.0)

    // อ่านค่ารอบแรกเพื่อตั้งต้น filtered_val ป้องกันค่าเริ่มจาก 0
    ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));
    float filtered_val = (float)raw_val;

    while (1) {
        // อ่านค่า ADC ดิบ
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));

        // คำนวณ Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // แปลงเป็น int แบบบวก 0.5f เพื่อปัดเศษตามทศนิยม (Rounding)
        printf("%d\n", (int)(filtered_val + 0.5f));
        fflush(stdout);

        // 20 ครั้งต่อวินาที (รอบละ 50ms)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}