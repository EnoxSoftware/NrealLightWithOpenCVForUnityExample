using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using NRKernal;
using OpenCVForUnity.UnityIntegration;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// NrealLightWithOpenCVForUnity Example
    /// </summary>
    public class NrealLightWithOpenCVForUnityExample : MonoBehaviour
    {
        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        // Use this for initialization
        protected void Start()
        {
            exampleTitle.text = "NrealLightWithOpenCVForUnity Example " + Application.version;

            versionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + OpenCVEnv.GetVersion() + " (" + Core.VERSION + ")";
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
#if UNITY_ANDROID && !UNITY_EDITOR
            versionInfo.text += " / NRSDK " + NRVersionInfo.GetVersion();
#endif
            versionInfo.text += " / ";

#if UNITY_EDITOR
            versionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
#elif UNITY_ANDROID
            versionInfo.text += "Android";
#elif UNITY_IOS
            versionInfo.text += "iOS";
#elif UNITY_WSA
            versionInfo.text += "WSA";
#elif UNITY_WEBGL
            versionInfo.text += "WebGL";
#endif
            versionInfo.text += " ";
#if ENABLE_MONO
            versionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            versionInfo.text += ".NET";
#endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnScrollRectValueChanged()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }


        public void OnShowLicenseButtonClick()
        {
            SceneManager.LoadScene("ShowLicense");
        }

        public void OnNrealPhotoCaptureExampleButtonClick()
        {
            SceneManager.LoadScene("NrealPhotoCaptureExample");
        }

        public void OnNRCamTexture2MatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("NRCamTexture2MatHelperExample");
        }

        public void OnNrealFaceDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("NrealFaceDetectionExample");
        }

        public void OnNrealArUcoExampleButtonClick()
        {
            SceneManager.LoadScene("NrealArUcoExample");
        }
        public void OnNrealObjectDetectionYOLOv4ExampleButtonClick()
        {
            SceneManager.LoadScene("NrealObjectDetectionYOLOv4Example");
        }

        public void OnNrealFaceDetectorYNExampleButtonClick()
        {
            SceneManager.LoadScene("NrealFaceDetectorYNExample");
        }

        public void OnNrealHumanSegmentationExampleButtonClick()
        {
            SceneManager.LoadScene("NrealHumanSegmentationExample");
        }
    }
}