from picamera import PiCamera
from time import sleep

camera = PiCamera()
camera.start_previwe()
sleep(30)
camera.stop_previwe()


