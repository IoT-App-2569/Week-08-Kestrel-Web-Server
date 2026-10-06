// ============================================================================
// ใบงานที่ 8.4: ESP32 Potentiometer Serial Stream (ต้นทางข้อมูลของ SVG Dashboard) (ESP-IDF v6.x)
// นักศึกษา: 67030011 กฤตนัย บุญน้อย
//
// กิจกรรมที่ 1: อ่านค่า ADC แบบ Oneshot แล้วสตรีมออก UART บรรทัดละ 1 ค่า
// กิจกรรมที่ 3: เพิ่มการกรองสัญญาณแบบ Exponential Moving Average (EMA) @ 20 Hz
// ============================================================================
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

// ----------------------------------------------------------------------------
// การกำหนดขา ADC ตามประเภทชิป:
// - ESP32 Classic (NodeMCU-32S): ขา GPIO 34 คือ ADC_UNIT_1, ADC_CHANNEL_6
// - ESP32-C6: ขา GPIO 4 คือ ADC_UNIT_1, ADC_CHANNEL_4
// ----------------------------------------------------------------------------
#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM
#endif

// 1 = ใช้ EMA Filter (กิจกรรมที่ 3), 0 = ส่งค่าดิบ 10 Hz (กิจกรรมที่ 1)
#define USE_EMA_FILTER     1

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...\n");

    // 1. สร้างและตั้งค่า ADC Unit 1
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. ความละเอียด 12-bit (0-4095), Attenuation 12dB สำหรับช่วง 0-3.3V
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));

    printf("[SYSTEM] ADC Initialized. Streaming raw values @ 115200 bps...\n");

    int raw_val = 0;
#if USE_EMA_FILTER
    float filtered_val = 0.0f;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง (0.0 - 1.0)

    // เริ่มต้น EMA ด้วยค่าที่อ่านได้จริง ไม่ให้ค่าไต่ขึ้นจาก 0 ตอนบูต
    ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));
    filtered_val = (float)raw_val;
#endif

    while (1) {
        // 3. อ่านค่า ADC แบบ Oneshot
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));

#if USE_EMA_FILTER
        // การกรองแบบ Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // ส่งค่าที่ผ่านการกรองแล้วออกทาง Serial บรรทัดละค่า
        printf("%d\n", (int)filtered_val);

        // สตรีม 20 ครั้งต่อวินาที (20 Hz)
        vTaskDelay(pdMS_TO_TICKS(50));
#else
        // 4. สตรีมตัวเลขเดี่ยวออกทาง UART (stdout) ปิดท้ายด้วย \n
        printf("%d\n", raw_val);

        // 10 ครั้งต่อวินาที
        vTaskDelay(pdMS_TO_TICKS(100));
#endif
    }
}
