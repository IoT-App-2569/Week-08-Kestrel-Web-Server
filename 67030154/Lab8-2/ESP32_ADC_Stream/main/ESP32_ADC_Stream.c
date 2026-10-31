#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_adc/adc_oneshot.h"
#include "esp_log.h"
#define TAG "ADC_STREAM"
void app_main(void){ adc_oneshot_unit_handle_t adc; adc_oneshot_unit_init_cfg_t unit={.unit_id=ADC_UNIT_1}; adc_oneshot_new_unit(&unit,&adc); adc_oneshot_chan_cfg_t cfg={.atten=ADC_ATTEN_DB_11,.bitwidth=ADC_BITWIDTH_DEFAULT}; adc_oneshot_config_channel(adc,ADC_CHANNEL_6,&cfg); while(1){int raw=0; adc_oneshot_read(adc,ADC_CHANNEL_6,&raw); printf("%d\n",raw); ESP_LOGI(TAG,"raw=%d",raw); vTaskDelay(pdMS_TO_TICKS(100));}}