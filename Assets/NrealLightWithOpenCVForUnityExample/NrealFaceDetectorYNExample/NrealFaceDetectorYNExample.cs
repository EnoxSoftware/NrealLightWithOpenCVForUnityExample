#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using NrealLightWithOpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using NRKernal;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// Nreal FaceDetectorYN Example
    /// An example of detecting human face in Nreal Light RGB camera using the FaceDetectorYN class.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    /// </summary>
    [RequireComponent(typeof(NRCamTexture2MatHelper))]
    public class NrealFaceDetectorYNExample : MonoBehaviour
    {
        /// <summary>
        /// The FaceDetectorYN.
        /// </summary>
        FaceDetectorYN faceDetector;

        /// <summary>
        /// The size for the network input.
        /// </summary>
        int inputSizeW = 320;
        int inputSizeH = 320;

        /// <summary>
        /// Filter out faces of score < score_threshold.
        /// </summary>
        float scoreThreshold = 0.5f; //0.9f;

        /// <summary>
        /// Suppress bounding boxes of iou >= nms_threshold
        /// </summary>
        float nmsThreshold = 0.3f;

        /// <summary>
        /// Keep top_k bounding boxes before NMS.
        /// </summary>
        int topK = 5000;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The input mat.
        /// </summary>
        Mat inputMat;

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
        /// MODEL_FILENAME
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnity/objdetect/face_detection_yunet_2023mar.onnx";

        protected Scalar bBoxColor = new Scalar(255, 255, 0, 255);

        protected Scalar[] keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };


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
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            enableFrameSkipToggle.isOn = enableFrameSkip;
            displayCameraImageToggle.isOn = displayCameraImage;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();


            string fd_modelPath = OpenCVEnv.GetFilePath(MODEL_FILENAME);
            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnityExample/objdetect/setup_objdetect_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
            }

            webCamTextureToMatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB;
            webCamTextureToMatHelper.Initialize();
        }
        */

        ////
        string fd_modelPath;

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

            fd_modelPath = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: cts.Token);

            Debug.Log("Preparing file access complete!");

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnityExample/objdetect/setup_objdetect_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
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

            bgrMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC3);
            inputMat = new Mat(new Size(inputSizeW, inputSizeH), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public virtual void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (bgrMat != null)
                bgrMat.Dispose();

            if (inputMat != null)
                inputMat.Dispose();
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

                /*
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

                Utils.matToTexture2D(rgbMat, texture);
                */


                Mat rgbMat = webCamTextureToMatHelper.GetMat();

                if (faceDetector == null)
                {
                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbMat, texture);
                    return;
                }

                Imgproc.cvtColor(rgbMat, bgrMat, Imgproc.COLOR_RGB2BGR);

                Detection[] detections = Detect(bgrMat);

                if (!displayCameraImage)
                {
                    // fill all black.
                    Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rgbMat.width(), rgbMat.height()), new Scalar(0, 0, 0, 0), -1);
                }

                foreach (var d in detections)
                {
                    DrawDetection(d, rgbMat);
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

            if (faceDetector != null)
                faceDetector.Dispose();

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

        protected virtual Detection[] Detect(Mat image)
        {
            Imgproc.resize(image, inputMat, inputMat.size());

            float scaleRatioX = (float)image.width() / inputMat.width();
            float scaleRatioY = (float)image.height() / inputMat.height();

            Detection[] detections;

            using (Mat faces = new Mat())
            {
                // The detection output faces is a two - dimension array of type CV_32F, whose rows are the detected face instances, columns are the location of a face and 5 facial landmarks.
                // The format of each row is as follows:
                // x1, y1, w, h, x_re, y_re, x_le, y_le, x_nt, y_nt, x_rcm, y_rcm, x_lcm, y_lcm
                // ,  where x1, y1, w, h are the top - left coordinates, width and height of the face bounding box, { x, y}_{ re, le, nt, rcm, lcm}
                // stands for the coordinates of right eye, left eye, nose tip, the right corner and left corner of the mouth respectively.
                faceDetector.detect(inputMat, faces);

                detections = new Detection[faces.rows()];

                for (int i = 0; i < faces.rows(); i++)
                {
                    float[] buf = new float[Detection.Size];
                    faces.get(i, 0, buf);

                    for (int x = 0; x < 14; x++)
                    {
                        if (x % 2 == 0)
                        {
                            buf[x] *= scaleRatioX;
                        }
                        else
                        {
                            buf[x] *= scaleRatioY;
                        }
                    }

                    GCHandle gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
                    detections[i] = (Detection)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(Detection));
                    gch.Free();
                }
            }

            return detections;
        }

        protected virtual void DrawDetection(Detection d, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(d.xy.x, d.xy.y), new Point(d.xy.x + d.wh.x, d.xy.y + d.wh.y), bBoxColor, 2);
            Imgproc.circle(frame, new Point(d.rightEye.x, d.rightEye.y), 2, keyPointsColors[0], 2);
            Imgproc.circle(frame, new Point(d.leftEye.x, d.leftEye.y), 2, keyPointsColors[1], 2);
            Imgproc.circle(frame, new Point(d.nose.x, d.nose.y), 2, keyPointsColors[2], 2);
            Imgproc.circle(frame, new Point(d.rightMouth.x, d.rightMouth.y), 2, keyPointsColors[3], 2);
            Imgproc.circle(frame, new Point(d.leftMouth.x, d.leftMouth.y), 2, keyPointsColors[4], 2);

            string label = d.score.ToString();
            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            float top = Mathf.Max(d.xy.y, (float)labelSize.height);
            float left = d.xy.x;
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Detection
        {
            // Bounding box
            public readonly Vector2 xy;
            public readonly Vector2 wh;

            // Key points
            public readonly Vector2 rightEye;
            public readonly Vector2 leftEye;
            public readonly Vector2 nose;
            public readonly Vector2 rightMouth;
            public readonly Vector2 leftMouth;

            // Confidence score [0, 1]
            public readonly float score;

            // sizeof(Detection)
            public const int Size = 15 * sizeof(float);
        };
    }
}
#endif

#endif