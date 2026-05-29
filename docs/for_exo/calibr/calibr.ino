
#include "HX711.h"

// Определяем пины для подключения датчика
const uint8_t DATA_PIN = A0;
const uint8_t CLOCK_PIN = A2;

// Создаем объект my_scale для работы с датчиком
HX711 my_scale;                      

void setup() 
{
    // Инициализируем последовательное соединение на скорости 9600 бод
    Serial.begin(9600);

    // Инициализируем датчик, указывая пины для подключения
    my_scale.begin(DATA_PIN, CLOCK_PIN);
    my_scale.tare(20);

    Serial.println("Калибровка датчика HX711...");
    Serial.println("Убедитесь, что на весах ничего не лежит и нажпите Enter");

    // Ждем подтверждения
    while (Serial.available()) Serial.read();
    while (Serial.available() == 0);

    Serial.println("Определение смещения...");

    // Получаем смещение датчика
    uint32_t offset = my_scale.get_offset();

    Serial.print("Смещение: ");
    Serial.println(offset);
    Serial.println();

    Serial.print("Положите эталонный образец на весы и введите его вес в граммах (округлите вес до целого числа): ");
    while (Serial.available()) Serial.read();

    // Инициализируем переменную для хранения веса
    size_t weight = 0;

    // Читаем вес из ввода пользователя
    while (Serial.peek() != '\n')
    {
        if (Serial.available())
        {
            char user_input = Serial.read();

            //  Переводим введенный вес в число
            if (isdigit(user_input))
            {
                weight *= 10;
                weight = weight + (user_input - '0');
            }
        }
    }

    Serial.print("Вес эталона: ");
    Serial.println(weight);

    // Калибруем датчик
    my_scale.calibrate_scale(weight, 20);

    // Получаем коэффициент масштабирования
    float scale = my_scale.get_scale();

    Serial.print("Коэффициент масштабирования:  ");
    Serial.println(scale, 6);

    Serial.print("\nИспользуйте scale.set_offset(");
    Serial.print(offset);
    Serial.print("); и scale.set_scale(");
    Serial.print(scale, 6);
    Serial.print("); ");
    Serial.print("в настройках вышего проекта.");
}

void loop() 
{
    // В этом примере loop() пуст, так как калибровка выполняется только один раз при инициализации
}