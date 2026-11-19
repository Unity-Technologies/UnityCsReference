// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    ///<summary>Provides methods to take screenshots.</summary>
    [NativeHeader("Modules/ScreenCapture/Public/CaptureScreenshot.h")]
    public static class ScreenCapture
    {
        public static void CaptureScreenshot(string filename)
        {
            CaptureScreenshot(filename, 1, StereoScreenCaptureMode.LeftEye);
        }

        ///<summary>Captures a screenshot and saves it as a .png file to a specified path.</summary>
        ///<remarks>
        ///  <para>Captures the current rendered screen output and saves it as an image file. This is a screenshot of the final frame presented to the user, not a capture from a specific Camera. If multiple cameras render to the screen, their combined result is captured. In split-screen or local multiplayer setups, the entire composed screen is saved.
        ///
        ///If the screenshot exists already, <c>ScreenCapture.CaptureScreenshot</c> overwrites it with a new screenshot.
        ///
        ///Add <c>.png</c> to the end of <c>filename</c> to save the screenshot as a .png file.
        ///
        ///
        ///**Mobile platforms (such as Android and iOS)**: <c>filename</c> is appended to the persistent data path. For more information, refer to <see cref="Application.persistentDataPath" />.
        ///
        ///**Windows Editor, macOS Editor, and other non-mobile platforms**: <c>filename</c> is interpreted relative to the project directory (the folder containing the <c>Assets</c> folder), not relative to <see cref="Application.persistentDataPath" />. Therefore, a relative path such as <c>SomeLevel.png</c> is saved in the project folder, not in <c>%userprofile%\AppData\LocalLow\...</c> on Windows.
        ///
        ///To save screenshots to the persistent data path (for example, <c>%userprofile%\AppData\LocalLow\&lt;companyname&gt;\&lt;productname&gt;</c> on Windows Editor), pass a full path: <c>System.IO.Path.Combine(Application.persistentDataPath, "screenshot.png")</c>.
        ///
        ///When the <c>superSize</c> parameter is more than 1, a larger resolution screenshot is
        ///produced. For example, if you pass 4, you create a screenshot 4x4 larger than normal. This is useful to produce screenshots you want to print.</para>
        ///  <para>The <see cref="CaptureScreenshot" /> returns immediately on Android. The screen capture continues in the background.  The resulting screen shot is saved in the file system after a few seconds.</para>
        ///</remarks>
        ///<param name="filename">The path to save the screenshot file to.</param>
        ///<param name="superSize">The factor to increase resolution with.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Generate a screenshot and save it to disk with the name SomeLevel.png.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnMouseDown()
        ///    {
        ///        ScreenCapture.CaptureScreenshot("SomeLevel.png");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void CaptureScreenshot(string filename, int superSize)
        {
            CaptureScreenshot(filename, superSize, StereoScreenCaptureMode.LeftEye);
        }

        ///<summary>Captures a screenshot and saves it as a .png file to a specified path.</summary>
        ///<remarks>
        ///  <para>Captures the current rendered screen output and saves it as an image file. This is a screenshot of the final frame presented to the user, not a capture from a specific Camera. If multiple cameras render to the screen, their combined result is captured. In split-screen or local multiplayer setups, the entire composed screen is saved.
        ///
        ///If the screenshot exists already, <c>ScreenCapture.CaptureScreenshot</c> overwrites it with a new screenshot.
        ///
        ///Add <c>.png</c> to the end of <c>filename</c> to save the screenshot as a .png file.
        ///
        ///
        ///**Mobile platforms (such as Android and iOS)**: <c>filename</c> is appended to the persistent data path. For more information, refer to <see cref="Application.persistentDataPath" />.
        ///
        ///**Windows Editor, macOS Editor, and other non-mobile platforms**: <c>filename</c> is interpreted relative to the project directory (the folder containing the <c>Assets</c> folder), not relative to <see cref="Application.persistentDataPath" />. Therefore, a relative path such as <c>SomeLevel.png</c> is saved in the project folder, not in <c>%userprofile%\AppData\LocalLow\...</c> on Windows.
        ///
        ///To save screenshots to the persistent data path (for example, <c>%userprofile%\AppData\LocalLow\&lt;companyname&gt;\&lt;productname&gt;</c> on Windows Editor), pass a full path: <c>System.IO.Path.Combine(Application.persistentDataPath, "screenshot.png")</c>.
        ///
        ///When the <c>superSize</c> parameter is more than 1, a larger resolution screenshot is
        ///produced. For example, if you pass 4, you create a screenshot 4x4 larger than normal. This is useful to produce screenshots you want to print.</para>
        ///  <para>The <see cref="CaptureScreenshot" /> returns immediately on Android. The screen capture continues in the background.  The resulting screen shot is saved in the file system after a few seconds.</para>
        ///</remarks>
        ///<param name="filename">The path to save the screenshot file to.</param>
        ///<param name="stereoCaptureMode">The eye texture to capture when stereo rendering is enabled.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Generate a screenshot and save it to disk with the name SomeLevel.png.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnMouseDown()
        ///    {
        ///        ScreenCapture.CaptureScreenshot("SomeLevel.png");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void CaptureScreenshot(string filename, StereoScreenCaptureMode stereoCaptureMode)
        {
            CaptureScreenshot(filename, 1, stereoCaptureMode);
        }

        public static Texture2D CaptureScreenshotAsTexture()
        {
            return CaptureScreenshotAsTexture(1, StereoScreenCaptureMode.LeftEye);
        }

        ///<summary>Captures a screenshot of the game view into a Texture2D object.</summary>
        ///<remarks>When <c>superSize</c> parameter is larger than 1, a larger resolution screenshot will be
        ///produced. For example, passing 4 will make the screenshot be 4x4 larger than it normally
        ///would. This is useful to produce screenshots for printing.
        ///
        ///**Important**: To ensure reliable results, always wait until the frame rendering process is complete before calling this method. To guarantee this, you can call it from a coroutine that yields on <see cref="WaitForEndOfFrame" />.</remarks>
        ///<param name="superSize">Factor by which to increase resolution.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ScreenShotter : MonoBehaviour
        ///{
        ///    IEnumerator RecordFrame()
        ///    {
        ///        yield return new WaitForEndOfFrame();
        ///        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        ///        // do something with texture
        ///
        ///        // cleanup
        ///        Object.Destroy(texture);
        ///    }
        ///
        ///    public void LateUpdate()
        ///    {
        ///        StartCoroutine(RecordFrame());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Texture2D CaptureScreenshotAsTexture(int superSize)
        {
            return CaptureScreenshotAsTexture(superSize, StereoScreenCaptureMode.LeftEye);
        }

        ///<summary>Captures a screenshot of the game view into a Texture2D object.</summary>
        ///<remarks>When <c>superSize</c> parameter is larger than 1, a larger resolution screenshot will be
        ///produced. For example, passing 4 will make the screenshot be 4x4 larger than it normally
        ///would. This is useful to produce screenshots for printing.
        ///
        ///**Important**: To ensure reliable results, always wait until the frame rendering process is complete before calling this method. To guarantee this, you can call it from a coroutine that yields on <see cref="WaitForEndOfFrame" />.</remarks>
        ///<param name="stereoCaptureMode">Specifies the eye texture to capture when stereo rendering is enabled.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ScreenShotter : MonoBehaviour
        ///{
        ///    IEnumerator RecordFrame()
        ///    {
        ///        yield return new WaitForEndOfFrame();
        ///        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        ///        // do something with texture
        ///
        ///        // cleanup
        ///        Object.Destroy(texture);
        ///    }
        ///
        ///    public void LateUpdate()
        ///    {
        ///        StartCoroutine(RecordFrame());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Texture2D CaptureScreenshotAsTexture(StereoScreenCaptureMode stereoCaptureMode)
        {
            return CaptureScreenshotAsTexture(1, stereoCaptureMode);
        }

        ///<summary>Captures a screenshot of the game view into a RenderTexture object.</summary>
        ///<remarks>
        ///  <para>This variant of screen capture make it possible to read pixels asynchronously using <see cref="T:UnityEngine.Rendering.AsyncGPUReadback" />, making the process consume less time on the main thread.
        ///
        ///**Important**: To ensure reliable results, always wait until the frame rendering process is complete before calling this method. To guarantee this, you can call it from a coroutine that yields on <see cref="WaitForEndOfFrame" />.</para>
        ///  <para>To capture each display when you use multiple displays, capture each at the end of the camera rendering. The following example demonstrates how to do this:</para>
        ///</remarks>
        ///<param name="renderTexture">RenderTexture that will get filled with the screen content.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///using UnityEngine.Rendering;
        ///
        ///public class ScreenCaptureIntoRenderTexture : MonoBehaviour
        ///{
        ///    private RenderTexture renderTexture;
        ///
        ///    IEnumerator Start()
        ///    {
        ///        yield return new WaitForEndOfFrame();
        ///
        ///        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        ///        ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);
        ///        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, ReadbackCompleted);
        ///    }
        ///
        ///    void ReadbackCompleted(AsyncGPUReadbackRequest request)
        ///    {
        ///        // Render texture no longer needed, it has been read back.
        ///        DestroyImmediate(renderTexture);
        ///
        ///        using (var imageBytes = request.GetData<byte>())
        ///        {
        ///            // do something with the pixel data.
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using System.IO;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///using UnityEngine.Rendering;
        ///using System.Security.Cryptography;
        ///
        ///public class CaptureScreen : MonoBehaviour
        ///{
        ///    private Camera cameraToCapture;
        ///    private RenderTexture renderTexture;
        ///
        ///    public void CaptureCamera(Camera camera)
        ///    {
        ///        cameraToCapture = camera;
        ///        if (cameraToCapture != null)
        ///        {
        ///            if (cameraToCapture.targetDisplay >= Display.displays.Length)
        ///            {
        ///                Debug.LogWarning("Invalid targetDisplay index, make sure you have activated the targetDisplay (i.e.: Display.displays[" + cameraToCapture.targetDisplay + "].Activate()).");
        ///            }
        ///            else
        ///            {
        ///                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogWarning("Unable to capture, cameraToCapture is null");
        ///        }
        ///    }
        ///
        ///    void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        ///    {
        ///        if (cameraToCapture != null && (cam == cameraToCapture))
        ///        {
        ///            if (cameraToCapture.targetDisplay < Display.displays.Length)
        ///            {
        ///                List<DisplayInfo> displayLayout = new List<DisplayInfo>();
        ///                Screen.GetDisplayLayout(displayLayout);
        ///                renderTexture = new RenderTexture(
        ///                    displayLayout[cameraToCapture.targetDisplay].width,
        ///                    displayLayout[cameraToCapture.targetDisplay].height,
        ///                    0);
        ///
        ///                ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);
        ///                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, ReadbackCompleted);
        ///
        ///                Debug.Log("Capturing " + cameraToCapture.targetDisplay + " " +
        ///                    displayLayout[cameraToCapture.targetDisplay].width + "x" +
        ///                    displayLayout[cameraToCapture.targetDisplay].height);
        ///            }
        ///
        ///            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        ///        }
        ///    }
        ///
        ///
        ///    void ReadbackCompleted(AsyncGPUReadbackRequest request)
        ///    {
        ///        DestroyImmediate(renderTexture);
        ///        using (var imageBytes = request.GetData<byte>())
        ///        {
        ///            byte[] bytes = ImageConversion.EncodeArrayToPNG(imageBytes.ToArray(),
        ///                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
        ///                (uint)request.width,
        ///                (uint)request.height, 0);
        ///            File.WriteAllBytes(Application.dataPath + "/../SavedScreen" + cameraToCapture.targetDisplay + ".png", bytes);
        ///            Debug.Log("Capture done, saved to png");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static extern void CaptureScreenshotIntoRenderTexture(RenderTexture renderTexture);

        private static extern void CaptureScreenshot(string filename, [UnityEngine.Internal.DefaultValue("1")] int superSize, [UnityEngine.Internal.DefaultValue("1")]  StereoScreenCaptureMode CaptureMode);
        private static extern Texture2D CaptureScreenshotAsTexture(int superSize, StereoScreenCaptureMode stereoScreenCaptureMode);

        // Composes XR eye render textures into one side-by-side Texture2D (for stereo image tests).
        // Reads each single-pass slice: GLES3 binds the slice and ReadPixels (CopyTexture no-ops on
        // array slices there), other backends CopyTexture the slice into a plain 2D RT (they can't
        // bind the eye texture's memoryless depth). Returns null if no usable eye textures are supplied.
        internal static Texture2D CaptureXREyeTexturesToTexture2D(RenderTexture[] eyeRenderTextures)
        {
            if (eyeRenderTextures == null)
                return null;

            var eyes = new List<Texture2D>();
            bool gles3 = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
            foreach (var eyeRT in eyeRenderTextures)
            {
                if (eyeRT == null)
                    continue;

                int sliceCount = eyeRT.dimension == TextureDimension.Tex2DArray ? eyeRT.volumeDepth : 1;
                var desc = eyeRT.descriptor;
                desc.dimension = TextureDimension.Tex2D;
                desc.volumeDepth = 1;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                for (int s = 0; s < sliceCount; s++)
                {
                    var prevActive = RenderTexture.active;
                    var eye = new Texture2D(eyeRT.width, eyeRT.height, TextureFormat.RGBA32, false, true);
                    if (gles3)
                    {
                        Graphics.SetRenderTarget(eyeRT, 0, CubemapFace.Unknown, s);
                        eye.ReadPixels(new Rect(0, 0, eyeRT.width, eyeRT.height), 0, 0, false);
                    }
                    else
                    {
                        // Binding the eye texture directly trips its memoryless depth buffer, so blit the
                        // slice into a plain 2D RT (this also resolves MSAA) and read that. Blit into a
                        // RenderTexture flips vertically, so pass a (1,-1)/(0,1) scale+offset to flip back and
                        // match the GLES3 ReadPixels orientation. Dynamic-resolution scenes render into a
                        // scaled sub-rect of the eye RT and are not captured cleanly this way, so those tests
                        // are ignored rather than compared.
                        var tmp = RenderTexture.GetTemporary(desc);
                        try
                        {
                            Graphics.Blit(eyeRT, tmp, new Vector2(1f, -1f), new Vector2(0f, 1f), s, 0);
                            RenderTexture.active = tmp;
                            eye.ReadPixels(new Rect(0, 0, eyeRT.width, eyeRT.height), 0, 0, false);
                        }
                        finally
                        {
                            RenderTexture.active = prevActive;
                            RenderTexture.ReleaseTemporary(tmp);
                        }
                    }
                    eye.Apply();
                    RenderTexture.active = prevActive;
                    eyes.Add(eye);
                }
            }

            if (eyes.Count == 0)
                return null;

            int eyeW = eyes[0].width, eyeH = eyes[0].height;
            var composite = new Texture2D(eyeW * eyes.Count, eyeH, TextureFormat.RGBA32, false, true);
            for (int i = 0; i < eyes.Count; i++)
                composite.SetPixels32(i * eyeW, 0, eyeW, eyeH, eyes[i].GetPixels32());
            composite.Apply();
            for (int i = 0; i < eyes.Count; i++)
                Object.Destroy(eyes[i]);
            return composite;
        }

        // Offsets must match UnityVRBlitMode in IUnityVR.h
        ///<summary>Enumeration specifying the eye texture to capture when using ScreenCapture.CaptureScreenshot and when stereo rendering is enabled.</summary>
        ///<remarks>The CaptureScreenshot method will default to StereoScreenCaptureMode.LeftEye. When captured in stereo mode, the resolution of the screenshot will be the size of the game window on the main display.</remarks>
        public enum StereoScreenCaptureMode
        {
            ///<summary>The Left Eye is captured. This is the default setting for the CaptureScreenshot method.</summary>
            LeftEye = 1,
            ///<summary>The Right Eye is captured.</summary>
            RightEye = 2,
            ///<summary>Both the left and right eyes are captured and composited into one image.</summary>
            BothEyes = 3,
            ///<summary>Both the left and right eyes are captured and composited into one image, showing the motion vectors.</summary>
            MotionVectors = 4,
        }
    }
}
