# NrealLight With OpenCVForUnity Example


## Demo Video
[![](http://img.youtube.com/vi/8e_IjCBkpwQ/0.jpg)](https://youtu.be/8e_IjCBkpwQ)


## Demo Hololens App
* [NrealLightWithOpenCVForUnityExample.zip](https://github.com/EnoxSoftware/NrealLightWithOpenCVForUnityExample/releases)


## Environment
* Android (Galaxy S10+ SC-04L)
* Nreal Light
* Unity 2020.3.38f1+ (NRSDK supports the development environment of Unity 2018.4.X and above.)
* [NRSDK](https://developer.nreal.ai/download)  Unity SDK 1.9.5 
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR) 2.4.8+ 


## Setup
1. Download the latest release unitypackage. [NrealLightWithOpenCVForUnityExample.unitypackage](https://github.com/EnoxSoftware/NrealLightWithOpenCVForUnityExample/releases)
1. Create a new project. (NrealLightWithOpenCVForUnityExample)
    * Change the platform to Android in the "Build Settings" window.
1. Import the OpenCVForUnity.
    * Setup the OpenCVForUnity. (Tools > OpenCV for Unity > Set Plugin Import Settings)
    * Move the "OpenCVForUnity/StreamingAssets/objdetect" and "OpenCVForUnity/StreamingAssets/dnn" folders to the "Assets/StreamingAssets/" folder.
    * Run the “download_dnn_models.py” in “Assets/StreamingAssets/dnn/” folder. ('python download_dnn_models.py YoloObjectDetectionExample' and 'python download_dnn_models.py LibFaceDetectionV2Example')
1. Import the NRSDK.
    * Download the latest release NRSDK unitypackage. [NRSDKForUnity_Release_1.9.x.unitypackage](https://developer.nreal.ai/download)
    * Setup the NRSDK. (See [Getting Started with NRSDK](https://nreal.gitbook.io/nrsdk/nrsdk-fundamentals/quickstart-for-android))
1. Import the NrealLightWithOpenCVForUnityExample.unitypackage.
1. Add the "Assets/NrealLightWithOpenCVForUnityExample/*.unity" files to the "Scenes In Build" list in the "Build Settings" window.
1. Build and Deploy to Android device. (See [8. Deploy to Nreal Device](https://nreal.gitbook.io/nrsdk/nrsdk-fundamentals/quickstart-for-android#8.-deploy-to-nreal-device))
    *  (Print the AR marker "CanonicalMarker-d10-i1-sp500-bb1.pdf" and "ChArUcoBoard-mx5-my7-d10-os1000-bb1.pdf" on an A4 size paper)


|Project Assets|Build Settings|
|---|---|
|![ProjectAssets.jpg](ProjectAssets.jpg)|![BuildSettings.jpg](BuildSettings.jpg)|


## ScreenShot
![screenshot01.jpg](screenshot01.jpg)

![screenshot02.jpg](screenshot02.jpg)

![screenshot03.jpg](screenshot03.jpg)

![screenshot04.jpg](screenshot04.jpg)

![screenshot05.jpg](screenshot05.jpg)

![screenshot05.jpg](screenshot06.jpg)

