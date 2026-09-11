// ============================================================================
// ใบงานที่ 8.5: ESP32 Dual-Channel ADC Serial Stream (ESP-IDF v6.x)
// อ่านค่า 2 ช่องสัญญาณพร้อมกัน แล้วส่งออกทาง Serial เป็น CSV "valA,valB"
// ============================================================================
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

// ----------------------------------------------------------------------------
// การกำหนดขา ADC ตามประเภทชิป:
// - ESP32 Classic (NodeMCU-32S):
//     ช่อง A (Analog Speedometer) -> GPIO 34 -> ADC_UNIT_1, ADC_CHANNEL_6
//     ช่อง B (Audio VU Meter)     -> GPIO 35 -> ADC_UNIT_1, ADC_CHANNEL_7
// - ESP32-C6:
//     ช่อง A -> GPIO 4 -> ADC_UNIT_1, ADC_CHANNEL_4
//     ช่อง B -> GPIO 2 -> ADC_UNIT_1, ADC_CHANNEL_2
// ----------------------------------------------------------------------------
#if CONFIG_IDF_TARGET_ESP32C6
#define CHANNEL_A_ADC    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#define CHANNEL_B_ADC    ADC_CHANNEL_2   // GPIO 2 บน ESP32-C6
#else
#define CHANNEL_A_ADC    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM
#define CHANNEL_B_ADC    ADC_CHANNEL_7   // GPIO 35 บน ESP32 WROOM
#endif

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Dual-Channel ADC Stream Starting...\n");

    // 1. สร้างและตั้งค่า ADC Unit 1 (ใช้ Unit เดียวกันสำหรับทั้ง 2 ช่องสัญญาณ)
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. กำหนดค่าความละเอียด 12-bit (0-4095) และอัตราขยายสัญญาณ (Attenuation 12dB สำหรับ 0-3.3V)
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, CHANNEL_A_ADC, &config));
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, CHANNEL_B_ADC, &config));

    printf("[SYSTEM] ADC Initialized. Streaming \"valA,valB\" @ 115200 bps...\n");

    int raw_a = 0, raw_b = 0;
    float filtered_a = 0.0f, filtered_b = 0.0f;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง (0.0 - 1.0)

    while (1) {
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, CHANNEL_A_ADC, &raw_a));
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, CHANNEL_B_ADC, &raw_b));

        // การกรองแบบ Exponential Moving Average (EMA) แยกอิสระต่อช่องสัญญาณ
        filtered_a = (alpha * raw_a) + ((1.0f - alpha) * filtered_a);
        filtered_b = (alpha * raw_b) + ((1.0f - alpha) * filtered_b);

        // ส่งค่าที่ผ่านการกรองแล้วออกทาง Serial เป็น CSV: "valA,valB"
        printf("%d,%d\n", (int)filtered_a, (int)filtered_b);

        // สตรีมเร็วขึ้นที่ 20 ครั้งต่อวินาที (20 Hz)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
