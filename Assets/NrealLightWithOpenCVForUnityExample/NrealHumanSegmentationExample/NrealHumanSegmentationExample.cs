#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using NrealLightWithOpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using NRKernal;
using System.Threading;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// Nreal Human Segmentation Example
    /// An example of using OpenCV dnn module and Human Segmentation model on Nreal Light.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/human_segmentation_pphumanseg
    /// </summary>
    [RequireComponent(typeof(NRCamTexture2MatHelper))]
    public class NrealHumanSegmentationExample : MonoBehaviour
    {

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        NRCamTexture2MatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The mask mat.
        /// </summary>
        Mat maskMat;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        /// <summary>
        /// MODEL_FILENAME
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExample/dnn/human_segmentation_pphumanseg_2023mar.onnx";

        /// <summary>
        /// The model filepath.
        /// </summary>
        string model_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        [HeaderAttribute("UI")]

        /// <summary>
        /// Determines if frame skip.
        /// </summary>
        public bool enableFrameSkip;

        /// <summary>
        /// The enable frame skip toggle.
        /// </summary>
        public Toggle enableFrameSkipToggle;

        /// <summary>
        /// Determines if displays camera image.
        /// </summary>
        public bool displayCameraImage = false;

        /// <summary>
        /// The display camera image toggle.
        /// </summary>
        public Toggle displayCameraImageToggle;

        /// <summary>
        /// the main camera.
        /// </summary>
        Camera mainCamera;

        /// <summary>
        /// The quad renderer.
        /// </summary>
        Renderer quad_renderer;

        /*
        // Use this for initialization
        protected virtual void Start()
        {
            enableFrameSkipToggle.isOn = enableFrameSkip;
            displayCameraImageToggle.isOn = displayCameraImage;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();


#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            model_filepath = OpenCVEnv.GetFilePath(MODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        protected virtual IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(MODEL_FILENAME, (result) =>
            {
                model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        protected virtual void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                net = Dnn.readNet(model_filepath);
            }

            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB;
            webCamTextureToMatHelper.Initialize();
        }
        */


        ////
        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            enableFrameSkipToggle.isOn = enableFrameSkip;
            displayCameraImageToggle.isOn = displayCameraImage;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();


            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            Debug.Log("Preparing file access...");

            model_filepath = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: cts.Token);

            Debug.Log("Preparing file access complete!");

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnityExample/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                net = Dnn.readNet(model_filepath);
            }

            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB;
            webCamTextureToMatHelper.Initialize();
        }
        ////

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public virtual void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat rgbMat = webCamTextureToMatHelper.GetMat();


            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));
            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);

#if !UNITY_EDITOR
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", webCamTextureToMatHelper.GetProjectionMatrix());
#else
            mainCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", mainCamera.projectionMatrix);
#endif

            maskMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public virtual void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (maskMat != null)
                maskMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public virtual void OnWebCamTextureToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                if (enableFrameSkip && imageOptimizationHelper.IsCurrentFrameSkipped())
                    return;


                Mat rgbMat = webCamTextureToMatHelper.GetMat();

                if (net == null)
                {
                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Mat blob = Dnn.blobFromImage(rgbMat, 1.0 / 255.0, new Size(192, 192), new Scalar(0.5, 0.5, 0.5), false, false, CvType.CV_32F);
                    // Divide blob by std.
                    Core.divide(blob, new Scalar(0.5, 0.5, 0.5), blob);

                    net.setInput(blob);

                    Mat prob = net.forward("save_infer_model/scale_0.tmp_1");

                    Mat result = new Mat();
                    Core.reduceArgMax(prob, result, 1);

                    result.convertTo(result, CvType.CV_8U);

                    Mat mask192x192 = new Mat(192, 192, CvType.CV_8UC1, (IntPtr)result.dataAddr());
                    Imgproc.resize(mask192x192, maskMat, rgbMat.size(), Imgproc.INTER_NEAREST);

                    if (!displayCameraImage)
                    {
                        // fill all black.
                        Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rgbMat.width(), rgbMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    rgbMat.setTo(new Scalar(255, 255, 255, 255), maskMat);

                    mask192x192.Dispose();
                    result.Dispose();

                    prob.Dispose();
                    blob.Dispose();
                }

                OpenCVMatUtils.MatToTexture2D(rgbMat, texture);
            }

            if (webCamTextureToMatHelper.IsPlaying())
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Matrix4x4 cameraToWorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();
#else
                Matrix4x4 cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;
#endif

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                /*
                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 1.5f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
                */

                //
                // Adjusting the position and scale of the display screen
                // to counteract the phenomenon of texture margins (transparent areas in MR space) being displayed as black when recording video using NRVideoCapture.
                //
                // Position the canvas object slightly in front
                // of the real world web camera.
                float overlayDistance = 1.5f;
                Vector3 ccCameraSpacePos = UnProjectVector(webCamTextureToMatHelper.GetProjectionMatrix(), new Vector3(0.0f, 0.0f, overlayDistance));
                Vector3 tlCameraSpacePos = UnProjectVector(webCamTextureToMatHelper.GetProjectionMatrix(), new Vector3(-overlayDistance, overlayDistance, overlayDistance));

                //position
                Vector3 position = cameraToWorldMatrix.MultiplyPoint3x4(ccCameraSpacePos);
                gameObject.transform.position = position;

                //scale
                Vector3 scale = new Vector3(Mathf.Abs(tlCameraSpacePos.x - ccCameraSpacePos.x) * 2, Mathf.Abs(tlCameraSpacePos.y - ccCameraSpacePos.y) * 2, 1);
                gameObject.transform.localScale = scale;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
                gameObject.transform.rotation = rotation;
                //
            }
        }

        //
        private Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
        {
            Vector3 from = new Vector3(0, 0, 0);
            var axsX = proj.GetRow(0);
            var axsY = proj.GetRow(1);
            var axsZ = proj.GetRow(2);
            from.z = to.z / axsZ.z;
            from.y = (to.y - (from.z * axsY.z)) / axsY.y;
            from.x = (to.x - (from.z * axsX.z)) / axsX.x;
            return from;
        }
        //

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
            imageOptimizationHelper.Dispose();

            if (net != null)
                net.Dispose();

            OpenCVDebug.SetDebugMode(false);

            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public virtual void OnBackButtonClick()
        {
            SceneManager.LoadScene("NrealLightWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public virtual void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public virtual void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public virtual void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public virtual void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.RequestedIsFrontFacing = !webCamTextureToMatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the enable frame skip toggle value changed event.
        /// </summary>
        public void OnEnableFrameSkipToggleValueChanged()
        {
            enableFrameSkip = enableFrameSkipToggle.isOn;
        }

        /// <summary>
        /// Raises the display camera image toggle value changed event.
        /// </summary>
        public void OnDisplayCameraImageToggleValueChanged()
        {
            displayCameraImage = displayCameraImageToggle.isOn;
        }
    }
}
#endif

#endif