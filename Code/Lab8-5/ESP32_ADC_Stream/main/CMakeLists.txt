# ลงทะเบียนคอมโพเนนต์ main
# ตั้งแต่ ESP-IDF v5.0 เป็นต้นมา ไดรเวอร์ ADC ถูกแยกออกมาเป็นคอมโพเนนต์ esp_adc
# จึงต้องประกาศไว้ใน REQUIRES ไม่เช่นนั้นจะคอมไพล์ไม่ผ่าน (หา esp_adc/adc_oneshot.h ไม่เจอ)
idf_component_register(SRCS "main.c"
                       INCLUDE_DIRS "."
                       REQUIRES esp_adc)
