using NRKernal;
using NRKernal.NRExamples;
using NRKernal.Record;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NrealLightWithOpenCVForUnityExample
{

#if UNITY_ANDROID && !UNITY_EDITOR
    using GalleryDataProvider = NativeGalleryDataProvider;
#else
    using GalleryDataProvider = MockGalleryDataProvider;
#endif

    /// <summary>
    /// Nreal PhotoCapture Example
    /// An example of holographic photo blending using the NRPhotocCapture class on Nreal Light.
    /// </summary>
    public class NrealPhotoCaptureExample : MonoBehaviour
    {
        /// <summary> The photo capture object. </summary>
        private NRPhotoCapture m_PhotoCaptureObject;
        /// <summary> The camera resolution. </summary>
        private Resolution m_CameraResolution;
        private bool isOnPhotoProcess = false;
        GalleryDataProvider galleryDataTool;


        GameObject m_Canvas = null;
        Renderer m_CanvasRenderer = null;
        Texture2D m_Texture = null;

        /// <summary>
        /// Determines if enable BlendMode Blend.
        /// </summary>
        public bool isBlendModeBlend;

        /// <summary>
        /// The is BlendMode Blend toggle.
        /// </summary>
        public Toggle isBlendModeBlendToggle;

        /// <summary>
        /// Determines if save texture to gallery.
        /// </summary>
        public bool saveTextureToGallery;

        /// <summary>
        /// The save texture to gallery toggle.
        /// </summary>
        public Toggle saveTextureToGalleryToggle;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The faces.
        /// </summary>
        MatOfRect faces;


        public GameObject XREAL_Head;
        public GameObject XREAL_RGBCamera;
        public GameObject XREAL_LEye;
        public GameObject XREAL_REye;
        public GameObject XREAL_CenterEye;
        public GameObject XREAL_LeftGrayscaleCamera;
        public GameObject XREAL_RightGrayscaleCamera;

        public RaycastLaser raycastLaser;

        /*
        protected void Start()
        {
            isBlendModeBlendToggle.isOn = isBlendModeBlend;
            saveTextureToGalleryToggle.isOn = saveTextureToGallery;

            m_Canvas = GameObject.Find("PhotoCaptureCanvas");
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.enabled = false;
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
            faces = new MatOfRect();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            Debug.Log("Preparing file access...");

            string cascade_filepath = await OpenCVEnv.GetFilePathTaskAsync("OpenCVForUnityExample/objdetect/haarcascade_frontalface_alt.xml", cancellationToken: cts.Token);

            Debug.Log("Preparing file access complete!");

            cascade = new CascadeClassifier();
            cascade.load(cascade_filepath);


            isBlendModeBlendToggle.isOn = isBlendModeBlend;
            saveTextureToGalleryToggle.isOn = saveTextureToGallery;

            m_Canvas = GameObject.Find("PhotoCaptureCanvas");
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.enabled = false;
        }
        //////

        /// <summary> Use this for initialization. </summary>
        void Create(Action<NRPhotoCapture> onCreated)
        {
            if (m_PhotoCaptureObject != null)
            {
                NRDebugger.Info("The NRPhotoCapture has already been created.");
                return;
            }

            // Create a PhotoCapture object
            NRPhotoCapture.CreateAsync(false, delegate (NRPhotoCapture captureObject)
            {
                m_CameraResolution = NRPhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

                if (captureObject == null)
                {
                    NRDebugger.Error("Can not get a captureObject.");
                    return;
                }

                m_PhotoCaptureObject = captureObject;

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.cameraResolutionWidth = m_CameraResolution.width;
                cameraParameters.cameraResolutionHeight = m_CameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                cameraParameters.frameRate = NativeConstants.RECORD_FPS_DEFAULT;
                cameraParameters.blendMode = (isBlendModeBlend) ? BlendMode.Blend : BlendMode.RGBOnly;

                // Activate the camera
                m_PhotoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (NRPhotoCapture.PhotoCaptureResult result)
                {
                    NRDebugger.Info("Start PhotoMode Async");
                    if (result.success)
                    {
                        onCreated?.Invoke(m_PhotoCaptureObject);
                    }
                    else
                    {
                        isOnPhotoProcess = false;
                        this.Close();
                        NRDebugger.Error("Start PhotoMode faild." + result.resultType);
                    }
                }, true);
            });
        }

        /// <summary> Take a photo. </summary>
        void TakeAPhoto()
        {
            if (isOnPhotoProcess)
            {
                NRDebugger.Warning("Currently in the process of taking pictures, Can not take photo .");
                return;
            }

            isOnPhotoProcess = true;
            if (m_PhotoCaptureObject == null)
            {
                this.Create((capture) =>
                {
                    capture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
            }
            else
            {
                m_PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
        }

        /// <summary> Executes the 'captured photo memory' action. </summary>
        /// <param name="result">            The result.</param>
        /// <param name="photoCaptureFrame"> The photo capture frame.</param>
        void OnCapturedPhotoToMemory(NRPhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Debug.Log("photoCaptureFrame.pixelFormat " + photoCaptureFrame.pixelFormat);
            Debug.Log("photoCaptureFrame.hasLocationData " + photoCaptureFrame.hasLocationData);
            Debug.Log("photoCaptureFrame.dataLength " + photoCaptureFrame.dataLength);

            Matrix4x4 cameraToWorldMatrix;
            Matrix4x4 projectionMatrix;

            if (photoCaptureFrame.hasLocationData)
            {
                photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
                photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);
            }
            else
            {
                // Get cameraToWorldMatrix
                // https://community.xreal.com/t/screen-to-world-point-from-centre-cam/1740/6
                Pose headPose = NRFrame.HeadPose;
                Matrix4x4 HeadPoseMatrix = Matrix4x4.TRS(headPose.position, headPose.rotation, Vector3.one);
                var eyeposeFromHead = NRFrame.EyePoseFromHead;
                Matrix4x4 rgbCameraPoseFromHeadMatrix = Matrix4x4.TRS(eyeposeFromHead.RGBEyePose.position, eyeposeFromHead.RGBEyePose.rotation, Vector3.one);
                Matrix4x4 localToWorldMatrix = HeadPoseMatrix * rgbCameraPoseFromHeadMatrix;
                // Transform localToWorldMatrix to cameraToWorldMatrix.
                cameraToWorldMatrix = localToWorldMatrix * Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), new Vector3(1, 1, -1));
                Debug.Log("localToWorldMatrix:\n" + localToWorldMatrix.ToString());

                // Get projectionMatrix
                bool _result;
                EyeProjectMatrixData pm = NRFrame.GetEyeProjectMatrix(out _result, 0.3f, 1000f);
                projectionMatrix = pm.RGBEyeMatrix;
            }

            Debug.Log("cameraToWorldMatrix:\n" + cameraToWorldMatrix.ToString());
            Debug.Log("projectionMatrix:\n" + projectionMatrix.ToString());

            Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;


            if (m_Texture == null)
            {
                m_Texture = new Texture2D(m_CameraResolution.width, m_CameraResolution.height, TextureFormat.RGB24, false);
                rgbMat = new Mat(m_Texture.height, m_Texture.width, CvType.CV_8UC3);
                grayMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);

                //faces = new MatOfRect();

                //cascade = new CascadeClassifier();
                //cascade.load(Utils.getFilePath("OpenCVForUnity/objdetect/haarcascade_frontalface_alt.xml"));
            }

            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(m_Texture);


            OpenCVMatUtils.Texture2DToMat(m_Texture, rgbMat);

            Imgproc.cvtColor(rgbMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);

            // fill all black.
            //Imgproc.rectangle (rgbMat, new Point (0, 0), new Point (rgbMat.width (), rgbMat.height ()), new Scalar (0, 0, 0, 0), -1);
            // draw an edge lines.
            Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rgbMat.width(), rgbMat.height()), new Scalar(255, 0, 0, 255), 2);
            // draw a diagonal line.
            //Imgproc.line (rgbMat, new Point (0, 0), new Point (rgbMat.cols (), rgbMat.rows ()), new Scalar (255, 0, 0, 255));

            if (cascade != null)
                cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size(grayMat.cols() * 0.05, grayMat.rows() * 0.05), new Size());

            OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
            for (int i = 0; i < rects.Length; i++)
            {
                //Debug.Log ("detect faces " + rects [i]);
                Imgproc.rectangle(rgbMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
            }

            Imgproc.putText(rgbMat, "W:" + rgbMat.width() + " H:" + rgbMat.height() + " SO:" + Screen.orientation, new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, new Scalar(0, 255, 0, 255), 2, Imgproc.LINE_AA, false);

            OpenCVMatUtils.MatToTexture2D(rgbMat, m_Texture);


            m_Texture.wrapMode = TextureWrapMode.Clamp;

            m_CanvasRenderer.enabled = true;
            m_CanvasRenderer.sharedMaterial.SetTexture("_MainTex", m_Texture);
            m_CanvasRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
            m_CanvasRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
            m_CanvasRenderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));
            m_CanvasRenderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);


            /*
            // Position the canvas object slightly in front
            // of the real world web camera.
            Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
            position *= 1.5f;

            // Rotate the canvas object so that it faces the user.
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

            m_Canvas.transform.position = position;
            m_Canvas.transform.rotation = rotation;
            */

            // Adjusting the position and scale of the display screen
            // to counteract the phenomenon of texture margins (transparent areas in MR space) being displayed as black when recording video using NRVideoCapture.
            //
            // Position the canvas object slightly in front
            // of the real world web camera.
            float overlayDistance = 1.5f;
            Vector3 ccCameraSpacePos = UnProjectVector(projectionMatrix, new Vector3(0.0f, 0.0f, overlayDistance));
            Vector3 tlCameraSpacePos = UnProjectVector(projectionMatrix, new Vector3(-overlayDistance, overlayDistance, overlayDistance));

            //position
            Vector3 position = cameraToWorldMatrix.MultiplyPoint3x4(ccCameraSpacePos);
            m_Canvas.transform.position = position;

            //scale
            Vector3 scale = new Vector3(Mathf.Abs(tlCameraSpacePos.x - ccCameraSpacePos.x) * 2, Mathf.Abs(tlCameraSpacePos.y - ccCameraSpacePos.y) * 2, 1);
            m_Canvas.transform.localScale = scale;

            // Rotate the canvas object so that it faces the user.
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
            m_Canvas.transform.rotation = rotation;
            //


            // Place a marker objects at the coordinates of XREAL Glass Components.
            PlaceMarkerObjects();
            // Draw the View Frustum of the RGB Camera.
            DrawViewFrustum(cameraToWorldMatrix, projectionMatrix);


            Debug.Log("Took picture!");

            if (saveTextureToGallery)
                SaveTextureToGallery(m_Texture);

            // Release camera resource after capture the photo.
            this.Close();
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

        private void PlaceMarkerObjects()
        {
            // The XREAL glasses consists of the following key components
            // 2 x Grayscale Cameras
            // 2 x Display Cameras
            // Head / IMU
            // RGB Camera
            // https://xreal.gitbook.io/nrsdk/v/v1.10.2/advanced/nrsdk-coordinate-systems#converting-extrinsics-from-unity-to-opencv

            // Head/IMU
            Pose headPose = NRFrame.HeadPose;
            Matrix4x4 headPoseM = Matrix4x4.TRS(headPose.position, headPose.rotation, Vector3.one);
            OpenCVARUtils.SetTransformFromMatrix(XREAL_Head.transform, ref headPoseM);

            // RGB Camera
            var eyeposeFromHead = NRFrame.EyePoseFromHead;
            Pose rgbCameraPose = eyeposeFromHead.RGBEyePose;
            Matrix4x4 rgbCameraPoseFromHeadM = Matrix4x4.TRS(rgbCameraPose.position, rgbCameraPose.rotation, Vector3.one);
            Matrix4x4 rgbCameraPoseM = headPoseM * rgbCameraPoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_RGBCamera.transform, ref rgbCameraPoseM);

            // 2 x Grayscale Cameras
            Pose leftGrayscaleCameraPose = NRFrame.GetDevicePoseFromHead(NativeDevice.LEFT_GRAYSCALE_CAMERA);
            Matrix4x4 leftGrayscaleCameraPoseFromHeadM = Matrix4x4.TRS(leftGrayscaleCameraPose.position, leftGrayscaleCameraPose.rotation, Vector3.one);
            Matrix4x4 leftGrayscaleCameraPoseM = headPoseM * leftGrayscaleCameraPoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_LeftGrayscaleCamera.transform, ref leftGrayscaleCameraPoseM);
            Pose rightGrayscaleCameraPose = NRFrame.GetDevicePoseFromHead(NativeDevice.RIGHT_GRAYSCALE_CAMERA);
            Matrix4x4 rightGrayscaleCameraPoseFromHeadM = Matrix4x4.TRS(rightGrayscaleCameraPose.position, rightGrayscaleCameraPose.rotation, Vector3.one);
            Matrix4x4 rightGrayscaleCameraPoseM = headPoseM * rightGrayscaleCameraPoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_RightGrayscaleCamera.transform, ref rightGrayscaleCameraPoseM);

            // 2 x Display Cameras (LEye and REye)
            Pose lEyePose = eyeposeFromHead.LEyePose;
            Matrix4x4 lEyePoseFromHeadM = Matrix4x4.TRS(lEyePose.position, lEyePose.rotation, Vector3.one);
            Matrix4x4 lEyePoseM = headPoseM * lEyePoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_LEye.transform, ref lEyePoseM);
            Pose rEyePose = eyeposeFromHead.REyePose;
            Matrix4x4 rEyePoseFromHeadM = Matrix4x4.TRS(rEyePose.position, rEyePose.rotation, Vector3.one);
            Matrix4x4 rEyePoseM = headPoseM * rEyePoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_REye.transform, ref rEyePoseM);

            // Center Eye (the pose of between left eye and right eye)
            Vector3 centerEye_pos = (eyeposeFromHead.LEyePose.position + eyeposeFromHead.REyePose.position) * 0.5f;
            Quaternion centerEye_rot = Quaternion.Lerp(eyeposeFromHead.LEyePose.rotation, eyeposeFromHead.REyePose.rotation, 0.5f);
            Pose centerEyePose = new Pose(centerEye_pos, centerEye_rot);
            Matrix4x4 centerEyePoseFromHeadM = Matrix4x4.TRS(centerEyePose.position, centerEyePose.rotation, Vector3.one);
            Matrix4x4 centerEyePoseM = headPoseM * centerEyePoseFromHeadM;
            OpenCVARUtils.SetTransformFromMatrix(XREAL_CenterEye.transform, ref centerEyePoseM);

            Debug.Log("XREAL_Head_localToWorldMatrix: \n" + XREAL_Head.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_RGBCamera_localToWorldMatrix: \n" + XREAL_RGBCamera.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_LeftGrayscaleCamera_localToWorldMatrix: \n" + XREAL_LeftGrayscaleCamera.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_RightGrayscaleCamera_localToWorldMatrix: \n" + XREAL_RightGrayscaleCamera.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_LEye_localToWorldMatrix: \n" + XREAL_LEye.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_REye_localToWorldMatrix: \n" + XREAL_REye.transform.localToWorldMatrix.ToString());
            Debug.Log("XREAL_CenterEye_localToWorldMatrix: \n" + XREAL_CenterEye.transform.localToWorldMatrix.ToString());


            //
            Debug.Log("rgbCameraPoseFromHeadM: \n" + rgbCameraPoseFromHeadM.ToString()); // == NRFrame.GetDevicePoseFromHead(NativeDevice.RGB_CAMERA)
            Debug.Log("leftGrayscaleCameraPoseFromHeadM: \n" + leftGrayscaleCameraPoseFromHeadM.ToString()); // Return identity matrix.
            Debug.Log("rightGrayscaleCameraPoseFromHeadM: \n" + rightGrayscaleCameraPoseFromHeadM.ToString()); // Return identity matrix.
            Debug.Log("lEyePoseFromHeadM: \n" + lEyePoseFromHeadM.ToString()); // == NRFrame.GetDevicePoseFromHead(NativeDevice.LEFT_DISPLAY)
            Debug.Log("rEyePoseFromHeadM: \n" + rEyePoseFromHeadM.ToString()); // == NRFrame.GetDevicePoseFromHead(NativeDevice.RIGHT_DISPLAY)
            Debug.Log("centerEyePoseFromHeadM: \n" + centerEyePoseFromHeadM.ToString()); // == NRFrame.CenterEyePose

            //Pose magenticePose = NRFrame.GetDevicePoseFromHead(NativeDevice.MAGENTICE);
            //Matrix4x4 magenticePoseFromHeadM = Matrix4x4.TRS(magenticePose.position, magenticePose.rotation, Vector3.one);
            //Debug.Log("magenticePoseFromHeadM: \n" + magenticePoseFromHeadM.ToString());

            //Pose leftDisplayPose = NRFrame.GetDevicePoseFromHead(NativeDevice.LEFT_DISPLAY);
            //Matrix4x4 leftDisplayPoseFromHeadM = Matrix4x4.TRS(leftDisplayPose.position, leftDisplayPose.rotation, Vector3.one);
            //Debug.Log("leftDisplayPoseFromHeadM: \n" + leftDisplayPoseFromHeadM.ToString());

            //Pose rightDisplayPose = NRFrame.GetDevicePoseFromHead(NativeDevice.RIGHT_DISPLAY);
            //Matrix4x4 rightDisplayPoseFromHeadM = Matrix4x4.TRS(rightDisplayPose.position, rightDisplayPose.rotation, Vector3.one);
            //Debug.Log("rightDisplayPoseFromHeadM: \n" + rightDisplayPoseFromHeadM.ToString());
            //
        }

        private void DrawViewFrustum(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
        {
            for (int i = raycastLaser.transform.childCount - 1; i >= 0; --i)
            {
                GameObject.DestroyImmediate(raycastLaser.transform.GetChild(i).gameObject);
            }

            // Get the ray directions
            Vector3 imageCenterDirection = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, new Vector2(0, 0));
            Vector3 imageTopLeftDirection = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, new Vector2(-1, -1));
            Vector3 imageTopRightDirection = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, new Vector2(1, -1));
            Vector3 imageBotLeftDirection = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, new Vector2(-1, 1));
            Vector3 imageBotRightDirection = PixelCoordToWorldCoord(cameraToWorldMatrix, projectionMatrix, new Vector2(1, 1));

            // Paint the rays on the 3d world
            raycastLaser.shootLaserFrom(cameraToWorldMatrix.GetColumn(3), imageCenterDirection, 3f);
            raycastLaser.shootLaserFrom(cameraToWorldMatrix.GetColumn(3), imageTopLeftDirection, 3f);
            raycastLaser.shootLaserFrom(cameraToWorldMatrix.GetColumn(3), imageTopRightDirection, 3f);
            raycastLaser.shootLaserFrom(cameraToWorldMatrix.GetColumn(3), imageBotLeftDirection, 3f);
            raycastLaser.shootLaserFrom(cameraToWorldMatrix.GetColumn(3), imageBotRightDirection, 3f);
        }

        private static Vector3 PixelCoordToWorldCoord(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix, Vector2 pixelCoordinates)
        {
            //  pixelCoordinates is -1 to 1 coords

            float focalLengthX = projectionMatrix.GetColumn(0).x;
            float focalLengthY = projectionMatrix.GetColumn(1).y;
            float centerX = projectionMatrix.GetColumn(2).x;
            float centerY = projectionMatrix.GetColumn(2).y;

            float normFactor = projectionMatrix.GetColumn(2).z;
            centerX = centerX / normFactor;
            centerY = centerY / normFactor;

            Vector3 dirRay = new Vector3((pixelCoordinates.x - centerX) / focalLengthX, (pixelCoordinates.y - centerY) / focalLengthY, 1.0f / normFactor); //Direction is in camera space
            Vector3 direction = new Vector3(Vector3.Dot(cameraToWorldMatrix.GetRow(0), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(1), dirRay), Vector3.Dot(cameraToWorldMatrix.GetRow(2), dirRay));

            return direction;
        }

        /// <summary> Closes this object. </summary>
        void Close()
        {
            if (m_PhotoCaptureObject == null)
            {
                NRDebugger.Error("The NRPhotoCapture has not been created.");
                return;
            }
            // Deactivate our camera
            m_PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        /// <summary> Executes the 'stopped photo mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedPhotoMode(NRPhotoCapture.PhotoCaptureResult result)
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;
            isOnPhotoProcess = false;
        }

        /// <summary> Executes the 'destroy' action. </summary>
        void OnDestroy()
        {
            // Shutdown our photo capture resource
            m_PhotoCaptureObject?.Dispose();
            m_PhotoCaptureObject = null;


            if (m_Texture != null)
            {
                Texture2D.Destroy(m_Texture);
                m_Texture = null;
            }

            if (rgbMat != null)
                rgbMat.Dispose();

            if (grayMat != null)
                grayMat.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (cts != null)
                cts.Dispose();
        }


        public void SaveTextureToGallery(Texture2D _texture)
        {
            try
            {
                string filename = string.Format("Nreal_Shot_{0}.png", NRTools.GetTimeStamp().ToString());
                byte[] _bytes = _texture.EncodeToPNG();
                NRDebugger.Info(_bytes.Length / 1024 + "Kb was saved as: " + filename);
                if (galleryDataTool == null)
                {
                    galleryDataTool = new GalleryDataProvider();
                }

                galleryDataTool.InsertImage(_bytes, filename, "Screenshots");
            }
            catch (Exception e)
            {
                NRDebugger.Error("[TakePicture] Save picture faild!");
                throw e;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NrealLightWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the take photo button click event.
        /// </summary>
        public void OnTakePhotoButtonClick()
        {
            TakeAPhoto();
        }

        /// <summary>
        /// Raises the is BlendMode Blend toggle value changed event.
        /// </summary>
        public void OnIsBlendModeBlendToggleValueChanged()
        {
            isBlendModeBlend = isBlendModeBlendToggle.isOn;

            if (m_PhotoCaptureObject != null)
                this.Close();
        }

        /// <summary>
        /// Raises the save texture to gallery toggle value changed event.
        /// </summary>
        public void OnSaveTextureToGalleryToggleValueChanged()
        {
            saveTextureToGallery = saveTextureToGalleryToggle.isOn;
        }
    }
}