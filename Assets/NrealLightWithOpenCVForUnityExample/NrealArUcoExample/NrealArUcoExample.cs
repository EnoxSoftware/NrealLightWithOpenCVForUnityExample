#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using NrealLightWithOpenCVForUnity.UnityUtils.Helper;
using NRKernal;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.ObjdetectModule;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// Nreal ArUco Example
    /// An example of marker based AR using OpenCVForUnity on Nreal Light.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// </summary>
    [RequireComponent(typeof(NRCamTexture2MatHelper), typeof(ImageOptimizationHelper))]
    public class NrealArUcoExample : MonoBehaviour
    {
        [HeaderAttribute("Preview")]

        /// <summary>
        /// The preview image.
        /// </summary>
        public RawImage previewImage;

        /// <summary>
        /// Determines if displays the camera preview.
        /// </summary>
        public bool displayCameraPreview;

        /// <summary>
        /// The toggle for switching the camera preview display state.
        /// </summary>
        public Toggle displayCameraPreviewToggle;


        [HeaderAttribute("Detection")]

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = true;

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;


        [HeaderAttribute("AR")]

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = Objdetect.DICT_6X6_250;

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.188f;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public ARGameObject arGameObject;


        [Space(10)]

        /// <summary>
        /// Determines if enable lerp filter.
        /// </summary>
        public bool enableLerpFilter;

        /// <summary>
        /// The enable lerp filter toggle.
        /// </summary>
        public Toggle enableLerpFilterToggle;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distCoeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM;

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM;

        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        NRCamTexture2MatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        // for CanonicalMarker.
        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Dictionary dictionary;
        ArucoDetector arucoDetector;

        Mat rvecs;
        Mat tvecs;

        Mat rgbMat4preview;
        Texture2D texture;


        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        Mat downScaleMat;

        bool _isThreadRunning = false;
        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        bool _isDetecting = false;
        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

        bool _hasUpdatedARTransformMatrix = false;
        bool hasUpdatedARTransformMatrix
        {
            get
            {
                lock (sync)
                    return _hasUpdatedARTransformMatrix;
            }
            set
            {
                lock (sync)
                    _hasUpdatedARTransformMatrix = value;
            }
        }

        /// <summary>
        /// the main camera.
        /// </summary>
        Camera mainCamera;


        // Use this for initialization
        protected void Start()
        {
            displayCameraPreviewToggle.isOn = displayCameraPreview;
            enableDownScaleToggle.isOn = enableDownScale;
            enableLerpFilterToggle.isOn = enableLerpFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTexture2MatHelper>();
            webCamTextureToMatHelper.outputColorFormat = Source2MatHelperColorFormat.GRAY;
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat grayMat = webCamTextureToMatHelper.GetMat();

            float width = grayMat.width();
            float height = grayMat.height();

            if (enableDownScale)
            {
                downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
            }
            else
            {
                downScaleMat = grayMat;
            }
            float downScaleMatWidth = downScaleMat.width();
            float downScaleMatHeight = downScaleMat.height();

            texture = new Texture2D((int)downScaleMatWidth, (int)downScaleMatHeight, TextureFormat.RGB24, false);
            previewImage.texture = texture;

            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

#if UNITY_ANDROID && !UNITY_EDITOR

            // Get the rgb camera's intrinsic matrix
            NativeMat3f intrinsicMat = NRFrame.GetDeviceIntrinsicMatrix(NativeDevice.RGB_CAMERA);

            camMatrix = CreateCameraMatrix(intrinsicMat.column0.X, intrinsicMat.column1.Y, intrinsicMat.column2.X, intrinsicMat.column2.Y);
            NRDistortionParams distort = NRFrame.GetDeviceDistortion(NativeDevice.RGB_CAMERA);
            distCoeffs = new MatOfDouble(new double[] { distort.distortParams1, distort.distortParams2, distort.distortParams3, distort.distortParams4, distort.distortParams5 });

            Debug.Log("Created CameraParameters from NRFrame.GetDeviceIntrinsicMatrix on device.");

#else

            mainCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;

            // set camera parameters.
            int max_d = (int)Mathf.Max(width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;

            camMatrix = CreateCameraMatrix(fx, fy, cx, cy);
            distCoeffs = new MatOfDouble(0, 0, 0, 0);

            Debug.Log("Created a dummy CameraParameters.");

            // if WebCamera is frontFaceing, flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice().isFrontFacing)
            {
                webCamTextureToMatHelper.flipHorizontal = true;
            }

#endif

            if (enableDownScale)
            {
                float DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
                double[] data = new double[(int)camMatrix.total()];
                camMatrix.get(0, 0, data);
                data[0] /= DOWNSCALE_RATIO;
                data[2] /= DOWNSCALE_RATIO;
                data[4] /= DOWNSCALE_RATIO;
                data[5] /= DOWNSCALE_RATIO;
                camMatrix.put(0, 0, data);
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());



            //Calibration camera
            Size imageSize = new Size(downScaleMatWidth, downScaleMatHeight);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rvecs = new Mat(1, 10, CvType.CV_64FC3);
            tvecs = new Mat(1, 10, CvType.CV_64FC3);
            dictionary = Objdetect.getPredefinedDictionary(dictionaryId);

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);


            transformationM = new Matrix4x4();

            invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            //Debug.Log("invertYM " + invertYM.ToString());

            invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            //Debug.Log("invertZM " + invertZM.ToString());

            rgbMat4preview = new Mat();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            StopThread();
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Clear();
            }
            isDetecting = false;


            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            hasUpdatedARTransformMatrix = false;


            if (arucoDetector != null)
                arucoDetector.Dispose();

            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();
            if (rvecs != null)
                rvecs.Dispose();
            if (tvecs != null)
                tvecs.Dispose();

            if (rgbMat4preview != null)
                rgbMat4preview.Dispose();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

        // Update is called once per frame
        void Update()
        {
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                if (enableDetection && !isDetecting)
                {
                    isDetecting = true;

                    Mat grayMat = webCamTextureToMatHelper.GetMat();

                    if (enableDownScale)
                    {
                        downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                    }
                    else
                    {
                        downScaleMat = grayMat;
                    }

                    StartThread(ThreadWorker);
                }
            }
        }

        private void StartThread(Action action)
        {
#if WINDOWS_UWP || (!UNITY_WSA_10_0 && (NET_4_6 || NET_STANDARD_2_0))
            System.Threading.Tasks.Task.Run(() => action());
#else
            System.Threading.ThreadPool.QueueUserWorkItem(_ => action());
#endif
        }

        private void StopThread()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning)
            {
                //Wait threading stop
            }
        }

        private void ThreadWorker()
        {
            isThreadRunning = true;

            DetectARUcoMarker();

            lock (ExecuteOnMainThread)
            {
                if (ExecuteOnMainThread.Count == 0)
                {
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        OnDetectionDone();
                    });
                }
            }

            isThreadRunning = false;
        }

        private void DetectARUcoMarker()
        {
            // Detect markers and estimate Pose
            Calib3d.undistort(downScaleMat, downScaleMat, camMatrix, distCoeffs);
            arucoDetector.detectMarkers(downScaleMat, corners, ids, rejectedCorners);

            if (applyEstimationPose && ids.total() > 0)
            {
                if (rvecs.cols() < ids.total())
                    rvecs.create(1, (int)ids.total(), CvType.CV_64FC3);
                if (tvecs.cols() < ids.total())
                    tvecs.create(1, (int)ids.total(), CvType.CV_64FC3);

                using (MatOfPoint3f objPoints = new MatOfPoint3f(
                    new Point3(-markerLength / 2f, markerLength / 2f, 0),
                    new Point3(markerLength / 2f, markerLength / 2f, 0),
                    new Point3(markerLength / 2f, -markerLength / 2f, 0),
                    new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                ))
                {
                    for (int i = 0; i < ids.total(); i++)
                    {
                        using (Mat rvec = new Mat(3, 1, CvType.CV_64FC1))
                        using (Mat tvec = new Mat(3, 1, CvType.CV_64FC1))
                        using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                        using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                        {
                            // Calculate pose for each marker
                            Calib3d.solvePnP(objPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                            rvec.reshape(3, 1).copyTo(new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));
                            tvec.reshape(3, 1).copyTo(new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));

                            // This example can display the ARObject on only first detected marker.
                            if (i == 0)
                            {
                                // Convert to unity pose data.
                                double[] rvecArr = new double[3];
                                rvec.get(0, 0, rvecArr);
                                double[] tvecArr = new double[3];
                                tvec.get(0, 0, tvecArr);
                                PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                                // Create transform matrix.
                                transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);

                                // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
                                // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                                ARM = invertYM * transformationM * invertYM;

                                // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                                ARM = ARM * invertYM * invertZM;

                                hasUpdatedARTransformMatrix = true;
                            }
                        }
                    }
                }
            }
        }

        private void OnDetectionDone()
        {
            if (displayCameraPreview)
            {
                Imgproc.cvtColor(downScaleMat, rgbMat4preview, Imgproc.COLOR_GRAY2RGB);

                if (ids.total() > 0)
                {
                    Objdetect.drawDetectedMarkers(rgbMat4preview, corners, ids, new Scalar(0, 255, 0));

                    if (applyEstimationPose)
                    {
                        for (int i = 0; i < ids.total(); i++)
                        {
                            using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                            using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                            {
                                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                                Calib3d.drawFrameAxes(rgbMat4preview, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);
                            }
                        }
                    }
                }

                Utils.matToTexture2D(rgbMat4preview, texture);
            }

            if (applyEstimationPose)
            {
                if (hasUpdatedARTransformMatrix)
                {
                    hasUpdatedARTransformMatrix = false;


#if UNITY_ANDROID && !UNITY_EDITOR
                    Matrix4x4 cameraToWorldMatrix = webCamTextureToMatHelper.GetCameraToWorldMatrix();
#else
                    Matrix4x4 cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;
#endif

                    // Apply the cameraToWorld matrix with the Z-axis inverted.
                    ARM = cameraToWorldMatrix * invertZM * ARM;

                    if (enableLerpFilter)
                    {
                        arGameObject.SetMatrix4x4(ARM);
                    }
                    else
                    {
                        ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
                    }
                }
            }

            isDetecting = false;
        }

        private Mat CreateCameraMatrix(double fx, double fy, double cx, double cy)
        {
            Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            return camMatrix;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
            imageOptimizationHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NrealLightWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the display camera preview toggle value changed event.
        /// </summary>
        public void OnDisplayCamreaPreviewToggleValueChanged()
        {
            displayCameraPreview = displayCameraPreviewToggle.isOn;
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            enableDownScale = enableDownScaleToggle.isOn;

            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the enable lerp filter toggle value changed event.
        /// </summary>
        public void OnEnableLerpFilterToggleValueChanged()
        {
            enableLerpFilter = enableLerpFilterToggle.isOn;
        }
    }
}

#endif