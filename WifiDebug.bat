@rem https://docs.microsoft.com/cs-cz/xamarin/android/get-started/installation/set-up-device-for-development
@rem adb kill-server
adb devices
adb tcpip 5555
adb connect 192.168.1.164:5555
@rem adb usb
@rem adb shell screencap -p /sdcard/screen.png