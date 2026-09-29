#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_adc/adc_oneshot.h"

#if CONFIG_IDF_TARGET_ESP32C6
#define SENSOR_A_ADC_CHANNEL ADC_CHANNEL_4 // GPIO4
#define SENSOR_B_ADC_CHANNEL ADC_CHANNEL_5 // GPIO5
#else
#define SENSOR_A_ADC_CHANNEL ADC_CHANNEL_6 // GPIO34
#define SENSOR_B_ADC_CHANNEL ADC_CHANNEL_7 // GPIO35
#endif

void app_main(void)
{
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    adc_oneshot_chan_cfg_t channel_config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };

    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config, &adc1_handle));
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, SENSOR_A_ADC_CHANNEL, &channel_config));
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, SENSOR_B_ADC_CHANNEL, &channel_config));

    while (1) {
        int sensor_a = 0;
        int sensor_b = 0;
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, SENSOR_A_ADC_CHANNEL, &sensor_a));
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, SENSOR_B_ADC_CHANNEL, &sensor_b));

        // Kestrel expects two real ADC values per line: channel A,channel B.
        printf("%d,%d\n", sensor_a, sensor_b);
        vTaskDelay(pdMS_TO_TICKS(100));
    }
}