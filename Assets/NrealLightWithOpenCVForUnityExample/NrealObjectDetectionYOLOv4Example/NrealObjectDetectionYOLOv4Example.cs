#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
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
using System.Text;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using System.Threading;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// Nreal ObjectDetection YOLOv4 Example
    /// An example of detecting objects in Nreal Light RGB camera using OpenCV dnn module.
    /// Referring to https://github.com/AlexeyAB/darknet.
    /// https://gist.github.com/YashasSamaga/48bdb167303e10f4d07b754888ddbdcf
    /// 
    /// [Tested Models]
    /// yolov4-tiny https://github.com/AlexeyAB/darknet/releases/download/yolov4/yolov4-tiny.weights, https://raw.githubusercontent.com/AlexeyAB/darknet/0faed3e60e52f742bbef43b83f6be51dd30f373e/cfg/yolov4-tiny.cfg
    /// yolov4 https://github.com/AlexeyAB/darknet/releases/download/yolov4/yolov4.weights, https://raw.githubusercontent.com/AlexeyAB/darknet/0faed3e60e52f742bbef43b83f6be51dd30f373e/cfg/yolov4.cfg
    /// </summary>
    [RequireComponent(typeof(NRCamTexture2MatHelper))]
    public class NrealObjectDetectionYOLOv4Example : MonoBehaviour
    {
        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model = "yolov4-tiny.weights";

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config = "yolov4-tiny.cfg";

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes = "coco.names";

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.25f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold = 0.45f;

        //[TooltipAttribute("Maximum detections per image.")]
        //public int topK = 1000;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth = 416;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight = 416;


        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;


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
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The YOLOv4 ObjectDetector.
        /// </summary>
        YOLOv4ObjectDetector objectDetector;


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


            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = OpenCVEnv.GetFilePath("OpenCVForUnityExample/dnn/" + classes);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = OpenCVEnv.GetFilePath("OpenCVForUnityExample/dnn/" + config);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = OpenCVEnv.GetFilePath("OpenCVForUnityExample/dnn/" + model);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }

            Run();
        }


        // Use this for initialization
        protected virtual void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded.");
            }
            else
            {
                objectDetector = new YOLOv4ObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold/*, topK);
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

            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = await OpenCVEnv.GetFilePathTaskAsync("OpenCVForUnityExample/dnn/" + classes, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = await OpenCVEnv.GetFilePathTaskAsync("OpenCVForUnityExample/dnn/" + config, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = await OpenCVEnv.GetFilePathTaskAsync("OpenCVForUnityExample/dnn/" + model, cancellationToken: cts.Token);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnityExample/dnn”.");
            }

            Debug.Log("Preparing file access complete!");

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded.");
            }
            else
            {
                objectDetector = new YOLOv4ObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold/*, topK*/);
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
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public virtual void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose();

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

                if (objectDetector == null)
                {
                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbMat, bgrMat, Imgproc.COLOR_RGB2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat results = objectDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("YOLOv4ObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    if (!displayCameraImage)
                    {
                        // fill all black.
                        Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rgbMat.width(), rgbMat.height()), new Scalar(0, 0, 0, 0), -1);
                    }

                    objectDetector.visualize(rgbMat, results, false, true);
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

            if (objectDetector != null)
                objectDetector.dispose();

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


    public class YOLOv4ObjectDetector
    {
        Size input_size;
        float conf_threshold;
        float nms_threshold;
        int topK;
        int backend;
        int target;

        int num_classes = 80;

        DetectionModel detection_model;

        List<string> classNames;

        List<Scalar> palette;

        Mat maxSizeImg;

        MatOfInt classIds;
        MatOfFloat confidences;
        MatOfRect boxes;

        public YOLOv4ObjectDetector(string modelFilepath, string configFilepath, string classesFilepath, Size inputSize, float confThreshold = 0.25f, float nmsThreshold = 0.45f, int topK = 1000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                detection_model = new DetectionModel(modelFilepath, configFilepath);
                detection_model.setInputParams(1.0 / 255.0, inputSize, new Scalar(0, 0, 0), true, false);
                detection_model.setNmsAcrossClasses(false);// Perform classwise NMS.
                detection_model.setPreferableBackend(this.backend);
                detection_model.setPreferableTarget(this.target);
            }

            if (!string.IsNullOrEmpty(classesFilepath))
            {
                classNames = readClassNames(classesFilepath);
                num_classes = classNames.Count;
            }

            input_size = new Size(inputSize.width > 0 ? inputSize.width : 640, inputSize.height > 0 ? inputSize.height : 640);
            conf_threshold = Mathf.Clamp01(confThreshold);
            nms_threshold = Mathf.Clamp01(nmsThreshold);
            this.topK = topK;
            this.backend = backend;
            this.target = target;

            classIds = new MatOfInt();
            confidences = new MatOfFloat();
            boxes = new MatOfRect();

            palette = new List<Scalar>();
            palette.Add(new Scalar(255, 56, 56, 255));
            palette.Add(new Scalar(255, 157, 151, 255));
            palette.Add(new Scalar(255, 112, 31, 255));
            palette.Add(new Scalar(255, 178, 29, 255));
            palette.Add(new Scalar(207, 210, 49, 255));
            palette.Add(new Scalar(72, 249, 10, 255));
            palette.Add(new Scalar(146, 204, 23, 255));
            palette.Add(new Scalar(61, 219, 134, 255));
            palette.Add(new Scalar(26, 147, 52, 255));
            palette.Add(new Scalar(0, 212, 187, 255));
            palette.Add(new Scalar(44, 153, 168, 255));
            palette.Add(new Scalar(0, 194, 255, 255));
            palette.Add(new Scalar(52, 69, 147, 255));
            palette.Add(new Scalar(100, 115, 255, 255));
            palette.Add(new Scalar(0, 24, 236, 255));
            palette.Add(new Scalar(132, 56, 255, 255));
            palette.Add(new Scalar(82, 0, 133, 255));
            palette.Add(new Scalar(203, 56, 255, 255));
            palette.Add(new Scalar(255, 149, 200, 255));
            palette.Add(new Scalar(255, 55, 199, 255));
        }

        protected virtual Mat preprocess(Mat image)
        {
            // Add padding to make it square.
            int max = Mathf.Max(image.cols(), image.rows());

            if (maxSizeImg == null)
                maxSizeImg = new Mat(max, max, image.type());
            if (maxSizeImg.width() != max || maxSizeImg.height() != max)
                maxSizeImg.create(max, max, image.type());

            Imgproc.rectangle(maxSizeImg, new OpenCVRect(0, 0, maxSizeImg.width(), maxSizeImg.height()), Scalar.all(114), -1);

            Mat _maxSizeImg_roi = new Mat(maxSizeImg, new OpenCVRect((max - image.cols()) / 2, (max - image.rows()) / 2, image.cols(), image.rows()));
            image.copyTo(_maxSizeImg_roi);

            return maxSizeImg;// [max, max, 3]
        }

        public virtual Mat infer(Mat image)
        {
            // cheack
            if (image.channels() != 3)
            {
                Debug.Log("The input image must be in BGR format.");
                return new Mat();
            }

            // Preprocess
            Mat input_blob = preprocess(image);

            // Forward
            detection_model.detect(input_blob, classIds, confidences, boxes, conf_threshold, nms_threshold);

            // Postprocess
            int num = classIds.rows();
            Mat results = new Mat(num, 6, CvType.CV_32FC1);

            float maxSize = Mathf.Max((float)image.size().width, (float)image.size().height);
            float x_shift = (maxSize - (float)image.size().width) / 2f;
            float y_shift = (maxSize - (float)image.size().height) / 2f;

            for (int i = 0; i < num; ++i)
            {
                int[] classId_arr = new int[1];
                classIds.get(i, 0, classId_arr);
                int id = classId_arr[0];

                float[] confidence_arr = new float[1];
                confidences.get(i, 0, confidence_arr);
                float confidence = confidence_arr[0];

                int[] box_arr = new int[4];
                boxes.get(i, 0, box_arr);
                int x = box_arr[0] - (int)x_shift;
                int y = box_arr[1] - (int)y_shift;
                int w = box_arr[2];
                int h = box_arr[3];

                results.put(i, 0, new float[] { x, y, x + w, y + h, confidence, id });
            }

            return results;
        }

        protected virtual Mat postprocess(Mat output_blob, Size original_shape)
        {
            return output_blob;
        }

        public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.empty() || results.cols() < 6)
                return;

            for (int i = results.rows() - 1; i >= 0; --i)
            {
                float[] box = new float[4];
                results.get(i, 0, box);
                float[] conf = new float[1];
                results.get(i, 4, conf);
                float[] cls = new float[1];
                results.get(i, 5, cls);

                float left = box[0];
                float top = box[1];
                float right = box[2];
                float bottom = box[3];
                int classId = (int)cls[0];

                Scalar c = palette[classId % palette.Count];
                Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), color, 2);

                string label = String.Format("{0:0.00}", conf[0]);
                if (classNames != null && classNames.Count != 0)
                {
                    if (classId < (int)classNames.Count)
                    {
                        label = classNames[classId] + " " + label;
                    }
                }

                int[] baseLine = new int[1];
                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                top = Mathf.Max((float)top, (float)labelSize.height);
                Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                    new Point(left + labelSize.width, top + baseLine[0]), color, Core.FILLED);
                Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);
            }

            // Print results
            if (print_results)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] box = new float[4];
                    results.get(i, 0, box);
                    float[] conf = new float[1];
                    results.get(i, 4, conf);
                    float[] cls = new float[1];
                    results.get(i, 5, cls);

                    int classId = (int)cls[0];
                    string label = String.Format("{0:0}", cls[0]);
                    if (classNames != null && classNames.Count != 0)
                    {
                        if (classId < (int)classNames.Count)
                        {
                            label = classNames[classId] + " " + label;
                        }
                    }

                    sb.AppendLine(String.Format("-----------object {0}-----------", i + 1));
                    sb.AppendLine(String.Format("conf: {0:0.0000}", conf[0]));
                    sb.AppendLine(String.Format("cls: {0:0}", label));
                    sb.AppendLine(String.Format("box: {0:0} {1:0} {2:0} {3:0}", box[0], box[1], box[2], box[3]));
                }

                Debug.Log(sb);
            }
        }

        public virtual void dispose()
        {
            if (detection_model != null)
                detection_model.Dispose();

            if (maxSizeImg != null)
                maxSizeImg.Dispose();

            maxSizeImg = null;

            if (classIds != null)
                classIds.Dispose();
            if (confidences != null)
                confidences.Dispose();
            if (boxes != null)
                boxes.Dispose();

            classIds = null;
            confidences = null;
            boxes = null;
        }

        protected virtual List<string> readClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }
    }
}
#endif

#endif