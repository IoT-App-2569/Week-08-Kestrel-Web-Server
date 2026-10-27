#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/adc.h"

void app_main(void) {
    // ????????? ADC1 Channel 6 (GPIO 34) ?????????????????? 12-bit (0-4095)
    adc1_config_width(ADC_WIDTH_BIT_12);
    adc1_config_channel_atten(ADC1_CHANNEL_6, ADC_ATTEN_DB_11);

    while (1) {
        int raw_value = adc1_get_raw(ADC1_CHANNEL_6);
        printf("%d\n", raw_value);
        vTaskDelay(pdMS_TO_TICKS(100));
    }
}
