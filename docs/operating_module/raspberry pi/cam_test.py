from picamera2 import Picamera2, Preview
from time import sleep

camera = Picamera2()
camera_config = camera.create_preview_configuration()
camera.configure(camera_config)

camera.start_preview(Preview.QTGL)
camera.start()
sleep(30000)
camera.close()
print("end")

