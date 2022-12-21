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
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using NrealLightWithOpenCVForUnity.UnityUtils.Helper;
using NRKernal;

namespace NrealLightWithOpenCVForUnityExample
{
    /// <summary>
    /// Nreal Dnn ObjectDetection Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/object_detection.cpp.
    /// </summary>
    [RequireComponent(typeof(NRCamTextureToMatHelper))]
    public class NrealDnnObjectDetectionExample : MonoBehaviour
    {

        [HeaderAttribute("DNN")]

        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model;

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config;

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes;

        [TooltipAttribute("Optional list of classes to label detected objects.")]
        public List<string> classesList;

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.5f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold = 0.4f;

        [TooltipAttribute("Preprocess input image by multiplying on a scale factor.")]
        public float scale = 1.0f;

        [TooltipAttribute("Preprocess input image by subtracting mean values. Mean values should be in BGR order and delimited by spaces.")]
        public Scalar mean = new Scalar(0, 0, 0, 0);

        [TooltipAttribute("Indicate that model works with RGB input images instead BGR ones.")]
        public bool swapRB = false;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth = 320;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight = 320;


        /// <summary>
        /// The texture.
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        protected NRCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        protected Mat bgrMat;

        /// <summary>
        /// The net.
        /// </summary>
        protected Net net;

        protected List<string> classNames;
        protected List<string> outBlobNames;
        protected List<string> outBlobTypes;

        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;

#if UNITY_WEBGL
        protected IEnumerator getFilePath_Coroutine;
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


        // Use this for initialization
        protected virtual void Start()
        {
            enableFrameSkipToggle.isOn = enableFrameSkip;
            displayCameraImageToggle.isOn = displayCameraImage;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<NRCamTextureToMatHelper>();


#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + classes);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + config);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + model);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            Run();
#endif
        }

#if UNITY_WEBGL
        protected virtual IEnumerator GetFilePath()
        {
            if (!string.IsNullOrEmpty(classes))
            {
                var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + classes, (result) =>
                {
                    classes_filepath = result;
                });
                yield return getFilePathAsync_0_Coroutine;

                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            if (!string.IsNullOrEmpty(config))
            {
                var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + config, (result) =>
                {
                    config_filepath = result;
                });
                yield return getFilePathAsync_1_Coroutine;

                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            if (!string.IsNullOrEmpty(model))
            {
                var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + model, (result) =>
                {
                    model_filepath = result;
                });
                yield return getFilePathAsync_2_Coroutine;

                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        protected virtual void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (!string.IsNullOrEmpty(classes))
            {
                classNames = readClassNames(classes_filepath);
                if (classNames == null)
                {
                    Debug.LogError(classes + " is not loaded. Please see “Assets/StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf”. ");
                }
            }
            else if (classesList.Count > 0)
            {
                classNames = classesList;
            }

            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(model + " is not loaded. Please see “Assets/StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf”. ");
            }
            else
            {
                //! [Initialize network]
                net = Dnn.readNet(model_filepath, config_filepath);
                //! [Initialize network]

                outBlobNames = getOutputsNames(net);
                //for (int i = 0; i < outBlobNames.Count; i++)
                //{
                //    Debug.Log("names [" + i + "] " + outBlobNames[i]);
                //}

                outBlobTypes = getOutputsTypes(net);
                //for (int i = 0; i < outBlobTypes.Count; i++)
                //{
                //    Debug.Log("types [" + i + "] " + outBlobTypes[i]);
                //}
            }

            webCamTextureToMatHelper.outputColorFormat = WebCamTextureToMatHelper.ColorFormat.RGB;
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public virtual void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();


            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGB24, false);

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

            bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
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
        public virtual void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
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

                    Imgproc.cvtColor(rgbMat, bgrMat, Imgproc.COLOR_RGB2BGR);

                    // Create a 4D blob from a frame.
                    Size inpSize = new Size(inpWidth > 0 ? inpWidth : bgrMat.cols(),
                                       inpHeight > 0 ? inpHeight : bgrMat.rows());
                    Mat blob = Dnn.blobFromImage(bgrMat, scale, inpSize, mean, swapRB, false);


                    // Run a model.
                    net.setInput(blob);

                    if (net.getLayer(0).outputNameToIndex("im_info") != -1)
                    {  // Faster-RCNN or R-FCN
                        Imgproc.resize(bgrMat, bgrMat, inpSize);
                        Mat imInfo = new Mat(1, 3, CvType.CV_32FC1);
                        imInfo.put(0, 0, new float[] {
                            (float)inpSize.height,
                            (float)inpSize.width,
                            1.6f
                        });
                        net.setInput(imInfo, "im_info");
                    }

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    List<Mat> outs = new List<Mat>();
                    net.forward(outs, outBlobNames);

                    //tm.stop();
                    //Debug.Log("Inference time, ms: " + tm.getTimeMilli());

                    postprocess(rgbMat, outs, net, Dnn.DNN_BACKEND_OPENCV);

                    for (int i = 0; i < outs.Count; i++)
                    {
                        outs[i].Dispose();
                    }
                    blob.Dispose();
                }

                Utils.matToTexture2D(rgbMat, texture);
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

            Utils.setDebugMode(false);

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
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
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
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

        /// <summary>
        /// Reads the class names.
        /// </summary>
        /// <returns>The class names.</returns>
        /// <param name="filename">Filename.</param>
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

        /// <summary>
        /// Postprocess the specified frame, outs and net.
        /// </summary>
        /// <param name="frame">Frame.</param>
        /// <param name="outs">Outs.</param>
        /// <param name="net">Net.</param>
        /// <param name="backend">Backend.</param>
        protected virtual void postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {
            MatOfInt outLayers = net.getUnconnectedOutLayers();
            string outLayerType = outBlobTypes[0];

            List<int> classIdsList = new List<int>();
            List<float> confidencesList = new List<float>();
            List<Rect2d> boxesList = new List<Rect2d>();

            if (net.getLayer(0).outputNameToIndex("im_info") != -1)
            {
                // Faster-RCNN or R-FCN
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {
                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {
                        outs[0].get(i, 0, data);

                        float confidence = data[2];
                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 1); // Skip 0th background class id.
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "DetectionOutput")
            {
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {
                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];
                    for (int i = 0; i < outs[0].rows(); i++)
                    {
                        outs[0].get(i, 0, data);

                        float confidence = data[2];
                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 1); // Skip 0th background class id.
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "Region")
            {
                for (int i = 0; i < outs.Count; ++i)
                {
                    // Network produces output blob with a shape NxC where N is a number of
                    // detected objects and C is a number of classes + 4 where the first 4
                    // numbers are [center_x, center_y, width, height]

                    //Debug.Log ("outs[i].ToString() "+outs[i].ToString());

                    float[] positionData = new float[5];
                    float[] confidenceData = new float[outs[i].cols() - 5];
                    for (int p = 0; p < outs[i].rows(); p++)
                    {
                        outs[i].get(p, 0, positionData);
                        outs[i].get(p, 5, confidenceData);

                        int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                        float confidence = confidenceData[maxIdx];
                        if (confidence > confThreshold)
                        {
                            float centerX = positionData[0] * frame.cols();
                            float centerY = positionData[1] * frame.rows();
                            float width = positionData[2] * frame.cols();
                            float height = positionData[3] * frame.rows();
                            float left = centerX - width / 2;
                            float top = centerY - height / 2;

                            classIdsList.Add(maxIdx);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Unknown output layer type: " + outLayerType);
            }

            // NMS is used inside Region layer only on DNN_BACKEND_OPENCV for another backends we need NMS in sample
            // or NMS is required if number of outputs > 1
            if (outLayers.total() > 1 || (outLayerType == "Region" && backend != Dnn.DNN_BACKEND_OPENCV))
            {
                Dictionary<int, List<int>> class2indices = new Dictionary<int, List<int>>();
                for (int i = 0; i < classIdsList.Count; i++)
                {
                    if (confidencesList[i] >= confThreshold)
                    {
                        if (!class2indices.ContainsKey(classIdsList[i]))
                            class2indices.Add(classIdsList[i], new List<int>());

                        class2indices[classIdsList[i]].Add(i);
                    }
                }

                List<Rect2d> nmsBoxesList = new List<Rect2d>();
                List<float> nmsConfidencesList = new List<float>();
                List<int> nmsClassIdsList = new List<int>();
                foreach (int key in class2indices.Keys)
                {
                    List<Rect2d> localBoxesList = new List<Rect2d>();
                    List<float> localConfidencesList = new List<float>();
                    List<int> classIndicesList = class2indices[key];
                    for (int i = 0; i < classIndicesList.Count; i++)
                    {
                        localBoxesList.Add(boxesList[classIndicesList[i]]);
                        localConfidencesList.Add(confidencesList[classIndicesList[i]]);
                    }

                    using (MatOfRect2d localBoxes = new MatOfRect2d(localBoxesList.ToArray()))
                    using (MatOfFloat localConfidences = new MatOfFloat(localConfidencesList.ToArray()))
                    using (MatOfInt nmsIndices = new MatOfInt())
                    {
                        Dnn.NMSBoxes(localBoxes, localConfidences, confThreshold, nmsThreshold, nmsIndices);
                        for (int i = 0; i < nmsIndices.total(); i++)
                        {
                            int idx = (int)nmsIndices.get(i, 0)[0];
                            nmsBoxesList.Add(localBoxesList[idx]);
                            nmsConfidencesList.Add(localConfidencesList[idx]);
                            nmsClassIdsList.Add(key);
                        }
                    }
                }

                boxesList = nmsBoxesList;
                classIdsList = nmsClassIdsList;
                confidencesList = nmsConfidencesList;
            }

            if (!displayCameraImage)
            {
                // fill all black.
                Imgproc.rectangle(frame, new Point(0, 0), new Point(frame.width(), frame.height()), new Scalar(0, 0, 0, 0), -1);
            }

            for (int idx = 0; idx < boxesList.Count; ++idx)
            {
                Rect2d box = boxesList[idx];
                drawPred(classIdsList[idx], confidencesList[idx], box.x, box.y,
                    box.x + box.width, box.y + box.height, frame);
            }
        }

        /// <summary>
        /// Draws the pred.
        /// </summary>
        /// <param name="classId">Class identifier.</param>
        /// <param name="conf">Conf.</param>
        /// <param name="left">Left.</param>
        /// <param name="top">Top.</param>
        /// <param name="right">Right.</param>
        /// <param name="bottom">Bottom.</param>
        /// <param name="frame">Frame.</param>
        protected virtual void drawPred(int classId, float conf, double left, double top, double right, double bottom, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

            string label = conf.ToString();
            if (classNames != null && classNames.Count != 0)
            {
                if (classId < (int)classNames.Count)
                {
                    label = classNames[classId] + ": " + label;
                }
            }

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            top = Mathf.Max((float)top, (float)labelSize.height);
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        /// <summary>
        /// Gets the outputs names.
        /// </summary>
        /// <returns>The outputs names.</returns>
        /// <param name="net">Net.</param>
        protected virtual List<string> getOutputsNames(Net net)
        {
            List<string> names = new List<string>();


            MatOfInt outLayers = net.getUnconnectedOutLayers();
            for (int i = 0; i < outLayers.total(); ++i)
            {
                names.Add(net.getLayer((int)outLayers.get(i, 0)[0]).get_name());
            }
            outLayers.Dispose();

            return names;
        }

        /// <summary>
        /// Gets the outputs types.
        /// </summary>
        /// <returns>The outputs types.</returns>
        /// <param name="net">Net.</param>
        protected virtual List<string> getOutputsTypes(Net net)
        {
            List<string> types = new List<string>();


            MatOfInt outLayers = net.getUnconnectedOutLayers();
            for (int i = 0; i < outLayers.total(); ++i)
            {
                types.Add(net.getLayer((int)outLayers.get(i, 0)[0]).get_type());
            }
            outLayers.Dispose();

            return types;
        }
    }
}
#endif

#endif