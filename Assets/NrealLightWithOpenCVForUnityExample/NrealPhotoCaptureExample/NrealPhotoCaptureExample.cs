using NRKernal;
using NRKernal.NRExamples;
using NRKernal.Record;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Linq;
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


        protected void Start()
        {
            isBlendModeBlendToggle.isOn = isBlendModeBlend;
            saveTextureToGalleryToggle.isOn = saveTextureToGallery;

            m_Canvas = GameObject.Find("PhotoCaptureCanvas");
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
            m_CanvasRenderer.enabled = false;
        }

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
                Camera mainCamera = NRSessionManager.Instance.NRHMDPoseTracker.centerCamera;
                cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;

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

                faces = new MatOfRect();

                cascade = new CascadeClassifier();
                cascade.load(Utils.getFilePath("OpenCVForUnity/objdetect/haarcascade_frontalface_alt.xml"));
            }

            // Copy the raw image data into our target texture
            photoCaptureFrame.UploadImageDataToTexture(m_Texture);


            Utils.texture2DToMat(m_Texture, rgbMat);

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

            Utils.matToTexture2D(rgbMat, m_Texture);


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