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
    /// NRCamTexture to mat helper.
    /// v 1.0.0
    /// Depends on NRSDK v 1.9.5 (https://nreal.gitbook.io/nrsdk/nrsdk-fundamentals/core-features).
    /// Depends on OpenCVForUnity version 2.4.1 (WebCamTextureToMatHelper v 1.1.2) or later.
    /// 
    /// By setting outputColorFormat to RGB, processing that does not include extra color conversion is performed.
    /// 
    /// </summary>
    public class NRCamTextureToMatHelper : WebCamTextureToMatHelper
    {

        protected NRRGBCamTexture nrRGBCamTexture = default;

        /// <summary>
        /// Returns the NRRGBCamTexture.
        /// </summary>
        /// <returns>The NRRGBCamTexture.</returns>
        public virtual NRRGBCamTexture GetNRRGBCamTexture()
        {
            return nrRGBCamTexture;
        }

        /// <summary>
        /// Pauses the CurrentFrame timeStamp.
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
        /// Pauses the CurrentFrame gain.
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
        /// Pauses the CurrentFrame exposureTime.
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
        /// Pauses the FrameCount.
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

        new protected ColorFormat baseColorFormat = ColorFormat.RGB;

        protected Matrix4x4 invertZM = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0), new Vector3(1, 1, -1));

        protected Matrix4x4 centerEyePoseFromHeadMatrix = Matrix4x4.identity;

        protected Matrix4x4 projectionMatrix = Matrix4x4.identity;


        // Update is called once per frame
        protected override void Update() { }

        /// <summary>
        /// Initializes this instance by coroutine.
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


#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // Checks camera permission state.
            IEnumerator coroutine = hasUserAuthorizedCameraPermission();
            yield return coroutine;

            if (!(bool)coroutine.Current)
            {
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(ErrorCode.CAMERA_PERMISSION_DENIED);

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
                    Debug.Log("NRCamTextureToMatHelper:: " + "DeviceName:" + "RGB_CAMERA" + " width:" + nrRGBCamTexture.Width + " height:" + nrRGBCamTexture.Height + " frameCount:" + nrRGBCamTexture.FrameCount);

                    baseMat = new Mat(nrRGBCamTexture.Height, nrRGBCamTexture.Width, CvType.CV_8UC3);

                    if (baseColorFormat == outputColorFormat)
                    {
                        frameMat = baseMat;
                    }
                    else
                    {
                        frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
                    }

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat(frameMat.cols(), frameMat.rows(), CvType.CV_8UC(Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;


                    // Get centerEyePose from Head Matrix
                    //
                    // Get physical RGBCamera position (offset position from Head). For some reason, when this value is used in the calculation, the position is shifted.
                    //Pose camPos = NRFrame.GetDevicePoseFromHead(NativeDevice.RGB_CAMERA);// Get Pose RGBCamera From Head
                    //centerEyePoseFromHeadMatrix = Matrix4x4.TRS(camPos.position, camPos.rotation, Vector3.one);
                    //
                    // or
                    //
                    var eyeposeFromHead = NRFrame.EyePoseFromHead;
                    Vector3 localPosition = (eyeposeFromHead.LEyePose.position + eyeposeFromHead.REyePose.position) * 0.5f;
                    Quaternion localRotation = Quaternion.Lerp(eyeposeFromHead.LEyePose.rotation, eyeposeFromHead.REyePose.rotation, 0.5f);
                    centerEyePoseFromHeadMatrix = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
                    //

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
                    onErrorOccurred.Invoke(ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public override void Play()
        {
            if (hasInitDone)
                nrRGBCamTexture.Play();
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public override void Pause()
        {
            if (hasInitDone)
                nrRGBCamTexture.Pause();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public override void Stop()
        {
            if (hasInitDone)
                nrRGBCamTexture.Stop();
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying()
        {
            return hasInitDone ? nrRGBCamTexture.IsPlaying : false;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing()
        {
            return false;
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public override string GetDeviceName()
        {
            return "RGB_CAMERA";
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public override float GetFPS()
        {
            return -1f;
        }

        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public override WebCamTexture GetWebCamTexture()
        {
            return null;
        }

        /// <summary>
        /// Returns the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public override Matrix4x4 GetCameraToWorldMatrix()
        {
            //
            //Pose headPose = NRFrame.HeadPose; // Get Head pose
            //Matrix4x4 HeadPoseM = Matrix4x4.TRS(headPose.position, headPose.rotation, Vector3.one);
            //Matrix4x4 localToWorldMatrix = HeadPoseM * centerEyePoseFromHeadMatrix;
            //
            // or
            // The values obtained by this method generally match reality, but I think they are slightly off to the left.
            Pose centerEyePose = NRFrame.CenterEyePose;
            Matrix4x4 localToWorldMatrix = Matrix4x4.TRS(centerEyePose.position, centerEyePose.rotation, Vector3.one);
            //

            // Transform localToWorldMatrix to cameraToWorldMatrix.
            return localToWorldMatrix * invertZM;
        }

        /// <summary>
        /// Returns the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public override Matrix4x4 GetProjectionMatrix()
        {
            return projectionMatrix;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public override bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return nrRGBCamTexture.DidUpdateThisFrame;
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// Please do not dispose of the returned mat as it will be reused.
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public override Mat GetMat()
        {
            if (!hasInitDone || !nrRGBCamTexture.IsPlaying)
            {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            if (baseColorFormat == outputColorFormat)
            {
                Utils.fastTexture2DToMat(nrRGBCamTexture.GetTexture(), frameMat, false);
            }
            else
            {
                Utils.fastTexture2DToMat(nrRGBCamTexture.GetTexture(), baseMat, false);
                Imgproc.cvtColor(baseMat, frameMat, ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            FlipMat(frameMat, flipVertical, flipHorizontal);
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
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected override void FlipMat(Mat mat, bool flipVertical, bool flipHorizontal)
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
        /// Gets the buffer colors.
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
        /// Releases all resource used by the <see cref="WebCamTextureToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="WebCamTextureToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="WebCamTextureToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="WebCamTextureToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="WebCamTextureToMatHelper"/> was occupying.</remarks>
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