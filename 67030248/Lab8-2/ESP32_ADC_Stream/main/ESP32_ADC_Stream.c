// ============================================================================
// ใบงานที่ 8.5 (Bonus): ESP32 Dual-Channel Stream (POT GPIO34 + LDR GPIO35)
// ============================================================================
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

#include "driver/gpio.h"
#include "driver/rtc_io.h"

#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 (Potentiometer)
#define LDR_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 32 (LDR Light Sensor)

void app_main(void)
{
    printf("\n[SYSTEM] ESP32 Dual-Channel Pot+LDR Starting...\n");

    // 1. สร้างและตั้งค่า ADC Unit 1
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. กำหนดค่า 12-bit, Attenuation 12dB สำหรับ 0-3.3V ทั้งสองช่อง
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, LDR_ADC_CHANNEL, &config));

    // เปิดตัวต้านทานภายใน ESP32 (Internal Pull-down ~45k) ให้เป็นตัวแบ่งแรงดันในตัว
    // ทำให้สามารถต่อ LDR ตรงๆ เข้า 3.3V กับ GPIO 32 ได้เลยโดยไม่ต้องมีตัวต้านทานภายนอก!
    gpio_set_pull_mode(GPIO_NUM_32, GPIO_PULLDOWN_ONLY);
    rtc_gpio_pulldown_en(GPIO_NUM_32);
    rtc_gpio_pullup_dis(GPIO_NUM_32);

    printf("[SYSTEM] Dual ADC with Internal Pull-Down Initialized @ 115200 bps...\n");

    int raw_pot = 0, raw_ldr = 0;
    float filt_pot = 0.0f, filt_ldr = 0.0f;
    const float alpha = 0.25f;

    while (1) {
        // อ่านค่าทั้ง 2 ช่อง
        adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_pot);
        adc_oneshot_read(adc1_handle, LDR_ADC_CHANNEL, &raw_ldr);

        // EMA Filtering
        filt_pot = (alpha * raw_pot) + ((1.0f - alpha) * filt_pot);
        filt_ldr = (alpha * raw_ldr) + ((1.0f - alpha) * filt_ldr);

        // Calibrate Potentiometer (48-4094 -> 0-4095)
        int cal_pot = (int)(((filt_pot - 48.0f) / (4094.0f - 48.0f)) * 4095.0f);
        if (cal_pot < 0) cal_pot = 0;
        if (cal_pot > 4095) cal_pot = 4095;

        // LDR
        int cal_ldr = (int)filt_ldr;
        if (cal_ldr < 0) cal_ldr = 0;
        if (cal_ldr > 4095) cal_ldr = 4095;

        // สตรีมออกเป็นคู่คั่นด้วยจุลภาค: valA,valB
        printf("%d,%d\n", cal_pot, cal_ldr);

        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
