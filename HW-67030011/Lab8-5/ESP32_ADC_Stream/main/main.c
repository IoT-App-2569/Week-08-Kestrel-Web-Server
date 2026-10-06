// ============================================================================
// ใบงานที่ 8.5: ESP32 Dual-Channel Serial Stream (ESP-IDF v6.x)
// นักศึกษา: 67030011 กฤตนัย บุญน้อย
//
// - Channel A: Potentiometer ที่ GPIO 34 (ADC1_CH6)
// - Channel B: ค่าเริ่มต้นให้ฝั่ง Kestrel จำลองสัญญาณเอง (ทางเลือกที่ 1)
//   ถ้าต่อเซนเซอร์ตัวที่ 2 (LDR / Potentiometer) ที่ GPIO 35 (ADC1_CH7) แล้ว
//   ให้ตั้ง DUAL_CHANNEL เป็น 1 จะส่งเป็นคู่ "A,B" (ทางเลือกที่ 2 - Bonus)
// ============================================================================
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#define CH_B_ADC_CHANNEL   ADC_CHANNEL_5   // GPIO 5 บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM
#define CH_B_ADC_CHANNEL   ADC_CHANNEL_7   // GPIO 35 บน ESP32 WROOM
#endif

// 0 = ส่ง Channel A อย่างเดียว (Kestrel จำลอง Channel B)
// 1 = ส่ง "A,B" จากเซนเซอร์จริง 2 ตัว (ต้องต่อเซนเซอร์ที่ GPIO 35 ก่อน ไม่อย่างนั้นขาลอยจะได้ค่ามั่ว)
#define DUAL_CHANNEL       0

static const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง EMA

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Dual-Channel Stream Starting (DUAL_CHANNEL=%d)...\n", DUAL_CHANNEL);

    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));
#if DUAL_CHANNEL
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, CH_B_ADC_CHANNEL, &config));
#endif

    printf("[SYSTEM] ADC Initialized. Streaming @ 115200 bps, 20 Hz...\n");

    int raw_a = 0;
    ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_a));
    float filt_a = (float)raw_a;
#if DUAL_CHANNEL
    int raw_b = 0;
    ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, CH_B_ADC_CHANNEL, &raw_b));
    float filt_b = (float)raw_b;
#endif

    while (1) {
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_a));
        filt_a = (alpha * raw_a) + ((1.0f - alpha) * filt_a);

#if DUAL_CHANNEL
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, CH_B_ADC_CHANNEL, &raw_b));
        filt_b = (alpha * raw_b) + ((1.0f - alpha) * filt_b);
        printf("%d,%d\n", (int)filt_a, (int)filt_b);
#else
        printf("%d\n", (int)filt_a);
#endif

        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
