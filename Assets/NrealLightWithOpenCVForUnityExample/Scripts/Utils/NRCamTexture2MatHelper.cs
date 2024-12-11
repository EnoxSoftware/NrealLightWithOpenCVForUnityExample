#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using NRKernal;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections;
using UnityEngine;

namespace NrealLightWithOpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// A helper component class for obtaining camera frames from Nreal NRRGBCamTexture and converting them to OpenCV <c>Mat</c> format in real-time.
    /// </summary>
    /// <remarks>
    /// The <c>NRCamTexture2MatHelper</c> class captures video frames from a device's camera using Nreal NRRGBCamTexture
    /// and converts each frame to an OpenCV <c>Mat</c> object every frame. 
    /// 
    /// This component is particularly useful for image processing tasks in Unity, such as computer vision applications, 
    /// where real-time camera input in <c>Mat</c> format is required. It enables seamless integration of OpenCV-based 
    /// image processing algorithms with HoloLens camera input.
    /// 
    /// <para><strong>Note:</strong> By setting outputColorFormat to RGB, processing that does not include extra color conversion is performed.</para>
    /// <para><strong>Note:</strong> Depends on <a href="https://nreal.gitbook.io/nrsdk/nrsdk-fundamentals/core-features">NRSDK</a> v 2.1.0 or later.</para>
    /// <para><strong>Note:</strong> Depends on OpenCVForUnity version 2.6.4 or later.</para>
    /// </remarks>
    /// <example>
    /// Attach this component to a GameObject and call <c>GetMat()</c> to retrieve the latest camera frame in <c>Mat</c> format. 
    /// The helper class manages camera start/stop operations and frame updates internally.
    /// </example>
    public class NRCamTexture2MatHelper : WebCamTexture2MatHelper
    {

        protected NRRGBCamTexture nrRGBCamTexture = default;

        /// <summary>
        /// Return the NRRGBCamTexture.
        /// </summary>
        /// <returns>The NRRGBCamTexture.</returns>
        public virtual NRRGBCamTexture GetNRRGBCamTexture()
        {
            return nrRGBCamTexture;
        }

        /// <summary>
        /// Return the CurrentFrame timeStamp.
        /// </summary>
        public virtual ulong GetCurrentFrameTimeStamp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_NRSDK_API
            return hasInitDone ? nrRGBCamTexture.CurrentFrame.timeStamp : ulong.MinValue;
#else
            return ulong.MinValue;
#endif
        }

        /// <summary>
        /// Return the CurrentFrame gain.
        /// </summary>
        public virtual ulong GetCurrentFrameGain()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_NRSDK_API
            return hasInitDone ? nrRGBCamTexture.CurrentFrame.gain : ulong.MinValue;
#else
            return ulong.MinValue;
#endif
        }

        /// <summary>
        /// Return the CurrentFrame exposureTime.
        /// </summary>
        public virtual ulong GetCurrentFrameExposureTime()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_NRSDK_API
            return hasInitDone ? nrRGBCamTexture.CurrentFrame.exposureTime : ulong.MinValue;
#else
            return ulong.MinValue;
#endif
        }

        /// <summary>
        /// Return the FrameCount.
        /// </summary>
        public virtual int GetFrameCount()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_NRSDK_API
            return hasInitDone ? nrRGBCamTexture.FrameCount : -1;
#else
            return -1;
#endif
        }


#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_NRSDK_API

        new protected Source2MatHelperColorFormat baseColorFormat = Source2MatHelperColorFormat.RGB;

        protected Matrix4x4 invertZM = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), new Vector3(1, 1, -1));

        protected Matrix4x4 rgbCameraPoseFromHeadMatrix = Matrix4x4.identity;

        protected Matrix4x4 centerEyePoseFromHeadMatrix = Matrix4x4.identity;

        protected Matrix4x4 projectionMatrix = Matrix4x4.identity;


        // Update is called once per frame
        protected override void Update() { }

        /// <summary>
        /// Initialize this instance by coroutine.
        /// </summary>
        protected override IEnumerator _Initialize()
        {

            if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }

            isInitWaiting = true;

            // Wait one frame before starting initialization process
            yield return null;


#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // Checks camera permission state.
            IEnumerator coroutine = hasUserAuthorizedCameraPermission();
            yield return coroutine;

            if (!(bool)coroutine.Current)
            {
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.CAMERA_PERMISSION_DENIED, string.Empty);

                yield break;
            }
#endif

            // Create an instance of NRRGBCamTexture
            nrRGBCamTexture = new NRRGBCamTexture();
            nrRGBCamTexture.Play();


            int initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {

                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (nrRGBCamTexture.DidUpdateThisFrame)
                {
                    Debug.Log("NRCamTexture2MatHelper:: " + "DeviceName:" + "RGB_CAMERA" + " width:" + nrRGBCamTexture.Width + " height:" + nrRGBCamTexture.Height + " frameCount:" + nrRGBCamTexture.FrameCount);

                    baseMat = new Mat(nrRGBCamTexture.Height, nrRGBCamTexture.Width, CvType.CV_8UC3);

                    if (baseColorFormat == outputColorFormat)
                    {
                        frameMat = baseMat;
                    }
                    else
                    {
                        frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
                    }

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat(frameMat.cols(), frameMat.rows(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;


                    // Get physical RGBCamera position (offset position from Head).
                    Pose camPos = NRFrame.GetDevicePoseFromHead(NativeDevice.RGB_CAMERA);
                    rgbCameraPoseFromHeadMatrix = Matrix4x4.TRS(camPos.position, camPos.rotation, Vector3.one);

                    // Get CenterEyePose (between left eye and right eye) position (offset position from Head).
                    var eyeposeFromHead = NRFrame.EyePoseFromHead;
                    Vector3 localPosition = (eyeposeFromHead.LEyePose.position + eyeposeFromHead.REyePose.position) * 0.5f;
                    Quaternion localRotation = Quaternion.Lerp(eyeposeFromHead.LEyePose.rotation, eyeposeFromHead.REyePose.rotation, 0.5f);
                    centerEyePoseFromHeadMatrix = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);

                    // Get projection Matrix
                    //
                    //NativeMat3f mat = NRFrame.GetDeviceIntrinsicMatrix(NativeDevice.RGB_CAMERA);// Get the rgb camera's intrinsic matrix
                    //projectionMatrix = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues(mat.column0.X, mat.column1.Y,
                    //    mat.column2.X, mat.column2.Y, NRFrame.GetDeviceResolution(NativeDevice.RGB_CAMERA).width, NRFrame.GetDeviceResolution(NativeDevice.RGB_CAMERA).height, 0.3f, 1000f);
                    //
                    // or
                    //
                    bool result;
                    EyeProjectMatrixData pm = NRFrame.GetEyeProjectMatrix(out result, 0.3f, 1000f);
                    while (!result)
                    {
                        yield return new WaitForEndOfFrame();
                        pm = NRFrame.GetEyeProjectMatrix(out result, 0.3f, 1000f);
                    }
                    projectionMatrix = pm.RGBEyeMatrix;
                    //


                    if (onInitialized != null)
                        onInitialized.Invoke();

                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout)
            {
                nrRGBCamTexture.Stop();
                nrRGBCamTexture = null;
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.TIMEOUT, string.Empty);
            }
        }

        /// <summary>
        /// Start the active camera.
        /// </summary>
        public override void Play()
        {
            if (hasInitDone)
                nrRGBCamTexture.Play();
        }

        /// <summary>
        /// Pause the active camera.
        /// </summary>
        public override void Pause()
        {
            if (hasInitDone)
                nrRGBCamTexture.Pause();
        }

        /// <summary>
        /// Stop the active camera.
        /// </summary>
        public override void Stop()
        {
            if (hasInitDone)
                nrRGBCamTexture.Stop();
        }

        /// <summary>
        /// Indicate whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying()
        {
            return hasInitDone ? nrRGBCamTexture.IsPlaying : false;
        }

        /// <summary>
        /// Indicate whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing()
        {
            return false;
        }

        /// <summary>
        /// Return the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public override string GetDeviceName()
        {
            return "RGB_CAMERA";
        }

        /// <summary>
        /// Return the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public override float GetFPS()
        {
            return -1f;
        }

        /// <summary>
        /// Return the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public override WebCamTexture GetWebCamTexture()
        {
            return null;
        }

        /// <summary>
        /// Return the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public override Matrix4x4 GetCameraToWorldMatrix()
        {
            //
            // RGB camera position is used. However, even if this correct value is used in the calculation, the projected AR object will appear slightly offset upward.
            // https://community.xreal.com/t/screen-to-world-point-from-centre-cam/1740/6
            Pose headPose = NRFrame.HeadPose;
            Matrix4x4 HeadPoseM = Matrix4x4.TRS(headPose.position, headPose.rotation, Vector3.one);
            Matrix4x4 localToWorldMatrix = HeadPoseM * rgbCameraPoseFromHeadMatrix;
            //
            // or
            //
            // Center eye position is used. The projected positions obtained with this method are generally consistent with reality, but are slightly off to the left.
            //Pose headPose = NRFrame.HeadPose;
            //Matrix4x4 HeadPoseM = Matrix4x4.TRS(headPose.position, headPose.rotation, Vector3.one);
            //Matrix4x4 localToWorldMatrix = HeadPoseM * centerEyePoseFromHeadMatrix;
            //

            // Transform localToWorldMatrix to cameraToWorldMatrix.
            return localToWorldMatrix * invertZM;
        }

        /// <summary>
        /// Return the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public override Matrix4x4 GetProjectionMatrix()
        {
            return projectionMatrix;
        }

        /// <summary>
        /// Indicate whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public override bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return nrRGBCamTexture.DidUpdateThisFrame;
        }

        /// <summary>
        /// Get the mat of the current frame.
        /// </summary>
        /// <remarks>
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// Please do not dispose of the returned mat as it will be reused.
        /// </remarks>
        /// <returns>The mat of the current frame.</returns>
        public override Mat GetMat()
        {
            if (!hasInitDone || !nrRGBCamTexture.IsPlaying)
            {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            if (baseColorFormat == outputColorFormat)
            {
                Utils.texture2DToMat(nrRGBCamTexture.GetTexture(), frameMat, false);
            }
            else
            {
                Utils.texture2DToMat(nrRGBCamTexture.GetTexture(), baseMat, false);
                Imgproc.cvtColor(baseMat, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            FlipMat(frameMat, flipVertical, flipHorizontal, false, 0);
            if (rotatedFrameMat != null)
            {
                Core.rotate(frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            }
            else
            {
                return frameMat;
            }
        }

        /// <summary>
        /// Flip Mat
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="flipVertical"></param>
        /// <param name="flipHorizontal"></param>
        /// <param name="isFrontFacing"></param>
        /// <param name="videoRotationAngle"></param>
        protected override void FlipMat(Mat mat, bool flipVertical, bool flipHorizontal, bool isFrontFacing, int videoRotationAngle)
        {
            int flipCode = int.MinValue;

            if (flipVertical)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 0;
                }
                else if (flipCode == 0)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == 1)
                {
                    flipCode = -1;
                }
                else if (flipCode == -1)
                {
                    flipCode = 1;
                }
            }

            if (flipHorizontal)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 1;
                }
                else if (flipCode == 0)
                {
                    flipCode = -1;
                }
                else if (flipCode == 1)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == -1)
                {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue)
            {
                Core.flip(mat, mat, flipCode);
            }
        }

        /// <summary>
        /// Get the buffer colors.
        /// </summary>
        /// <returns>The buffer colors.</returns>
        public override Color32[] GetBufferColors()
        {
            if (!hasInitDone)
                return null;

            return nrRGBCamTexture.GetTexture().GetPixels32();
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected override void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (nrRGBCamTexture != null)
            {
                nrRGBCamTexture.Stop();
                nrRGBCamTexture = null;
            }
            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (baseMat != null)
            {
                baseMat.Dispose();
                baseMat = null;
            }
            if (rotatedFrameMat != null)
            {
                rotatedFrameMat.Dispose();
                rotatedFrameMat = null;
            }
        }

        /// <summary>
        /// Releases all resource used by the <see cref="NRCamTexture2MatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="NRCamTexture2MatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="NRCamTexture2MatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="NRCamTexture2MatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="NRCamTexture2MatHelper"/> was occupying.</remarks>
        public override void Dispose()
        {
            if (colors != null)
                colors = null;

            if (isInitWaiting)
            {
                CancelInitCoroutine();
                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }
        }

#endif // UNITY_ANDROID && !DISABLE_NRSDK_API

    }
}

#endif
#endif