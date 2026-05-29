#include "HX711.h"
#include <TroykaThermometer.h>
#include <microLED.h>

#define DATA_PIN_EXO A0
#define CLOCK_PIN_EXO A1
#define PHOTOREZ_FRAME A2
#define THERMOMETR_FRAME A3
#define STRIP_PIN 2
#define NUMLEDS 20     


/*HX711 my_scale;
TroykaThermometer thermometer(THERMOMETR_FRAME);

float calibrated_offset= 153486;
uint32_t calibrated_scale = 654.767700;

microLED<NUMLEDS, STRIP_PIN, MLED_NO_CLOCK, LED_WS2812, ORDER_GRB> strip;*/
 
void setup()
{
    Serial.begin(9600);
    /*
    //Тензодатчик
    my_scale.begin(DATA_PIN_EXO, CLOCK_PIN_EXO);
    my_scale.set_scale(calibrated_scale); 
    my_scale.set_offset(calibrated_offset);
    my_scale.tare();

    //Фоторезистор
    pinMode(PHOTOREZ_FRAME, INPUT);

    //Датчик температуры
    thermometer.read();
    */
    /*
    Serial.println("13123");
    strip.setBrightness(150);
    strip.clear();
    for(int i=0; i<NUMLEDS; i++)
        strip.set(i, mRGB(255, 255, 255));
    Serial.println("1");
    //strip.show();*/
}
 
void loop()
{
    Serial.println("13123");
    /*
    int weight_measurement = my_scale.get_units(20);
    int light_measurement = analogRead(PHOTOREZ_FRAME);
    int temperature_measurement = thermometer.getTemperatureC();
    Serial.print("Вес: ");
    Serial.println(weight_measurement);
    Serial.print("Свет: ");
    Serial.println(light_measurement);
    Serial.print("Температура: ");
    Serial.println(temperature_measurement);
    Serial.println("---------------------------------------");
    Serial.println();
    delay(100);*/
}