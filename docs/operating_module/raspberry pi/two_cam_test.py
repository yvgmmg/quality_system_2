from tkinter import *
import cv2
from picamera2 import Picamera2

"""
root = Tk()
root.title("Menu")
root.geometry("1000x1000");

cameras = [(i+1, type(camera["Model"])) for i,camera in enumerate(Picamera2.global_camera_info())]
cameras_var = Variable(value=cameras)
cameras_listbox = Listbox(listvariable=cameras_var)
cameras_listbox.pack(anchor=NW, fill=X)
root.mainloop()

cameras = Picamera2.global_camera_info()
for i, cam in enumerate(cameras):
    print(i, cam)
"""

cap = cv2.VideoCapture(0)
#module_cap = 

while True:
    ret, frame = cap.read()
    cv2.imshow('video', frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break
cv2.relese()
cv2.destroyAllWindows()
