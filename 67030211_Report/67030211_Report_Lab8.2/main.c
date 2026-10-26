#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_adc/adc_oneshot.h"

void app_main(void) {
    adc_oneshot_unit_handle_t adc;
    adc_oneshot_unit_init_cfg_t init_cfg = { .unit_id = ADC_UNIT_1 };
    adc_oneshot_new_unit(&init_cfg, &adc);
    adc_oneshot_chan_cfg_t ch_cfg = { .bitwidth = ADC_BITWIDTH_12, .atten = ADC_ATTEN_DB_12 };
    adc_oneshot_config_channel(adc, ADC_CHANNEL_6, &ch_cfg);

    while (1) {
        int raw = 0;
        adc_oneshot_read(adc, ADC_CHANNEL_6, &raw);
        float v = (raw / 4095.0f) * 3.3f;
        printf("{\"voltage\":%.2f,\"raw\":%d}\n", v, raw);
        vTaskDelay(pdMS_TO_TICKS(500));
    }
}
