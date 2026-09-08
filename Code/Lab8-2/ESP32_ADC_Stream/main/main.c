// ============================================================================
//  ใบงานที่ 8.2 : ESP32 Potentiometer Serial Stream (ESP-IDF v6.x)
//  รหัสนักศึกษา : 67030098
//  ----------------------------------------------------------------------------
//  หน้าที่ : อ่านค่าแรงดันแอนะล็อกจาก Potentiometer ผ่าน ADC 12 บิต
//            เฉลี่ยหลายครั้ง กรองสัญญาณรบกวนด้วย EMA
//            แล้วสตรีมออกทาง UART (USB) ที่ 115200 bps
//
//  รูปแบบข้อมูลที่ส่งออก : เลขจำนวนเต็มหนึ่งค่าต่อบรรทัด ปิดท้ายด้วย \n
//  เพื่อให้ฝั่ง C# ใช้ SerialPort.ReadLine() + int.TryParse() อ่านได้ง่ายที่สุด
//
//  การต่อวงจร (ESP32 WROOM) — ใช้ Potentiometer B50K :
//      3.3V   -----> Potentiometer ขา 1 (VCC)
//      GND    -----> Potentiometer ขา 3 (GND)
//      GPIO32 -----> Potentiometer ขา 2 (Wiper)  << รับแรงดัน 0 - 3.3V
//
//  ⚠️ ห้ามต่อขา VCC ของ Potentiometer เข้ากับ 5V (VIN) เด็ดขาด
//     ขา GPIO ของ ESP32 ทนแรงดันได้สูงสุดเพียง 3.3V เท่านั้น
// ============================================================================
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

// ----------------------------------------------------------------------------
//  การกำหนดขา ADC ตามประเภทชิป
//  ----------------------------------------------------------------------------
//  ESP32 มีวงจร ADC 2 กลุ่ม :
//    - ADC1 (GPIO 32-39) ใช้งานได้เสมอ  <-- เลือกใช้กลุ่มนี้
//    - ADC2 (GPIO 0,2,4,12-15,25-27) จะถูกระบบ Wi-Fi ยึดครอง อ่านค่าไม่ได้เมื่อเปิด Wi-Fi
//
//  ตารางเทียบขา GPIO กับหมายเลขช่องของ ADC1 บน ESP32 Classic (WROOM / NodeMCU-32S)
//    GPIO 36 -> ADC_CHANNEL_0      GPIO 32 -> ADC_CHANNEL_4   << ที่ใช้อยู่ตอนนี้
//    GPIO 37 -> ADC_CHANNEL_1      GPIO 33 -> ADC_CHANNEL_5
//    GPIO 38 -> ADC_CHANNEL_2      GPIO 34 -> ADC_CHANNEL_6
//    GPIO 39 -> ADC_CHANNEL_3      GPIO 35 -> ADC_CHANNEL_7
//
//  หากย้ายสายไปขาอื่น ให้แก้ค่า POT_ADC_CHANNEL ด้านล่างตามตารางนี้
// ----------------------------------------------------------------------------
#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4  บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 32 บน ESP32 WROOM
#endif

// ----------------------------------------------------------------------------
//  จำนวนครั้งที่อ่านซ้ำแล้วเฉลี่ยในหนึ่งรอบ (Oversampling)
//  ----------------------------------------------------------------------------
//  Potentiometer B50K มีความต้านทานสูงถึง 50 kΩ ทำให้ที่ตำแหน่งกึ่งกลาง
//  ความต้านทานที่ ADC มองเห็น (Source Impedance) สูงประมาณ 12.5 kΩ
//  ซึ่งเกินค่าที่ ADC ของ ESP32 ชอบ (แนะนำต่ำกว่า 10 kΩ)
//  ตัวเก็บประจุภายในวงจร Sample-and-Hold จึงชาร์จไม่ทัน ทำให้ค่าแกว่ง
//
//  การอ่านซ้ำแล้วเฉลี่ยช่วยลดอาการนี้ได้มาก โดยไม่ต้องแก้วงจร
//  (ถ้ายังแกว่งอยู่ ให้เพิ่มตัวเก็บประจุ 0.1 uF จากขา Wiper ลง GND)
// ----------------------------------------------------------------------------
#define ADC_SAMPLE_COUNT   16

// ค่าสัมประสิทธิ์ตัวกรอง Exponential Moving Average (0.0 - 1.0)
//   ค่าน้อย  = นิ่งมาก แต่ตอบสนองช้า
//   ค่ามาก   = ไวมาก  แต่สัญญาณสั่น
#define EMA_ALPHA          0.25f

// คาบการสตรีมข้อมูล (มิลลิวินาที) — 50 ms = 20 ครั้งต่อวินาที (20 Hz)
#define STREAM_PERIOD_MS   50

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...\n");

    // ------------------------------------------------------------------------
    //  1. สร้างและตั้งค่า ADC Unit 1
    // ------------------------------------------------------------------------
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id  = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // ------------------------------------------------------------------------
    //  2. กำหนดความละเอียดและอัตราขยายของช่องสัญญาณ
    //     ADC_BITWIDTH_12 -> 2^12 = 4096 ระดับ นับจาก 0 ถึง 4095
    //     ADC_ATTEN_DB_12 -> ครอบคลุมช่วงแรงดันประมาณ 0 - 3.3V
    //     ความละเอียดต่อ 1 ระดับ = 3.3V / 4095 ~= 0.806 mV
    // ------------------------------------------------------------------------
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten    = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));

    printf("[SYSTEM] ADC1 channel %d ready (GPIO 32). Streaming @ 115200 bps...\n",
           (int)POT_ADC_CHANNEL);

    // ------------------------------------------------------------------------
    //  3. ลูปหลัก : อ่านเฉลี่ย -> กรอง EMA -> สตรีม
    // ------------------------------------------------------------------------
    int   raw_val      = 0;      // ค่าดิบที่อ่านได้จาก ADC (0 - 4095)
    float filtered_val = 0.0f;   // ค่าที่ผ่านการกรอง EMA แล้ว
    bool  first_sample = true;   // รอบแรกให้เซ็ตค่าตรงๆ ไม่ต้องไล่จาก 0

    while (1) {
        // ---- อ่านซ้ำหลายครั้งแล้วหาค่าเฉลี่ย (ลดผลจากความต้านทานสูงของ B50K) ----
        int32_t sum = 0;
        for (int i = 0; i < ADC_SAMPLE_COUNT; i++) {
            // adc_oneshot_read() คืนค่าเป็น esp_err_t (สถานะสำเร็จ/ผิดพลาด)
            // ส่วนค่าที่อ่านได้จริงจะถูกเขียนกลับเข้าตัวแปรผ่านพารามิเตอร์ int *out_raw
            ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));
            sum += raw_val;
        }
        int avg_val = (int)(sum / ADC_SAMPLE_COUNT);

        // ---- การกรองแบบ Exponential Moving Average (EMA) ----
        //   new = (alpha * ค่าปัจจุบัน) + ((1 - alpha) * ค่าเดิมที่กรองไว้)
        // ช่วยลดอาการสั่นของสัญญาณโดยไม่ต้องเก็บประวัติค่าย้อนหลังเป็นอาร์เรย์
        if (first_sample) {
            // ถ้าไม่เซ็ตค่าเริ่มต้น ตัวเลขรอบแรกๆ จะค่อยๆ ไต่จาก 0 ขึ้นมา
            // ทำให้หน้าเว็บเห็นเข็มกวาดขึ้นเองตอนเปิดเครื่องทั้งที่ไม่ได้หมุน
            filtered_val = (float)avg_val;
            first_sample = false;
        } else {
            filtered_val = (EMA_ALPHA * avg_val) + ((1.0f - EMA_ALPHA) * filtered_val);
        }

        // ---- สตรีมตัวเลขเดี่ยวออกทาง UART (stdout) ปิดท้ายด้วย \n ----
        printf("%d\n", (int)filtered_val);

        // หน่วงเวลาด้วย FreeRTOS Task Delay เพื่อคืน CPU ให้ task อื่น
        vTaskDelay(pdMS_TO_TICKS(STREAM_PERIOD_MS));
    }
}
