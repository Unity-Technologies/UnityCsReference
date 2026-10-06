// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    ///<summary>A container for video assets that can be used in the Vide.VideoPlayer component.</summary>
    ///<remarks>A VideoClip stores the video portion of a movie file using a codec that is appropriate for the target platform. The <see cref="UnityEngine.Video.VideoPlayer" /> class references VideoClips.
    ///
    ///The following example shows how to assign a video clip to a video player and play it.</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class PlayClip : MonoBehaviour
    ///{
    ///    public VideoClip myVideoClip;
    ///    public VideoPlayer videoPlayer;
    ///
    ///    void Start()
    ///    {
    ///        // Assign the clip to the player
    ///        videoPlayer.clip = myVideoClip;
    ///
    ///        // Optionally configure video player settings
    ///
    ///        videoPlayer.Play();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="UnityEngine.Video.VideoPlayer" />
    ///<seealso href="xref:class-VideoClip" />
    [RequiredByNativeCode]
    [global::UnityEngine.NativeClass("VideoClip", PersistentTypeId = 329)]
    [NativeHeader("Modules/Video/Public/VideoClip.h")]
    public sealed class VideoClip : Object
    {
        internal VideoClip(global::UnityEngine.EntityId id) : base(id) {}
        private VideoClip() {}

        ///<summary>Gets the original video clip file path as it was imported into Unity. (Read Only).</summary>
        ///<remarks>Use this property to find the original location of the file when you imported it into Unity. If you move the video clip file, this property’s value remains as the original location. The value only updates if you reimport the file. 
        ///
        ///**Note**: When you import your video file, if you enable **Transcode** in the VideoClip Import Settings, <c>originalPath</c> returns the new video format instead of the original format. For example, if you do the following:
        ///
        ///1. Import an MP4 video file into Unity.
        ///2. Click on the file to show the Import Settings window.
        ///3. Enable **Transcode**. 
        ///3. Set **Codec** to **VP8**.
        ///
        ///Unity imports the video file in the WebM format, and <c>originalPath</c> returns <c>.webm</c> instead of <c>.mp4</c>. To get your original format instead, use <see cref="M:UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Object)" />.</remarks>
        ///<example>
        ///  <code><![CDATA[ // The script outputs the clip’s original path to the console. 
        /// // Assign this script and a [[VideoPlayer]] component to a GameObject in your Scene.
        /// // In the [[VideoPlayer]] component, assign a video clip. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///using UnityEditor;
        ///
        ///public class OriginalPathExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        ///        VideoClip videoClip = videoPlayer.clip;
        ///        // Get the original video file path as it was imported into Unity.
        ///        string originalPath = videoClip.originalPath;
        ///        Debug.Log("Original Path: " + originalPath);
        ///
        ///        // Verify if the original file exists. 
        ///        if (System.IO.File.Exists(originalPath))
        ///        {
        ///            Debug.Log("Original video file found at: " + originalPath);
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Original video file not found at: " + originalPath +
        ///                ". Checking AssetDatabase instead.");
        ///            // Check Asset database instead for the video clip. 
        ///            string assetPath = AssetDatabase.GetAssetPath(videoClip);
        ///            if (System.IO.File.Exists(assetPath))
        ///            {
        ///                Debug.Log("Original video file found at " + assetPath);
        ///            }
        ///            else Debug.LogWarning("Original video file not found!");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso href="https://docs.unity3d.com/Manual/class-VideoClip.html">VideoClip Importer</seealso>
        public extern string originalPath { get; }

        ///<summary>The length of the video clip in frames. (Read Only).</summary>
        ///<remarks>It’s useful to know the length of a video in frames to ensure any effects you play on a video play within the time frame of the video. <see cref="VideoClip" /> extracts the frame count from the metadata of the file. 
        ///
        ///**Note**: The length VideoClip returns can be inaccurate as the external encoder can be imprecise. Use <see cref="VideoPlayer.frameCount" /> to get a more accurate value. However, <see cref="VideoPlayer.frameCount" /> becomes more precise after one playthrough, so won’t be completely accurate until after the clip finishes once.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script uses both the [[VideoPlayer]] and [[VideoClip]] components' frame count and frame rate
        /// // to calculate the length of the video in seconds. Sometimes this can return different results, 
        /// // but the VideoPlayer is more accurate, especially after a full playthrough.
        /// // The script recalculates the counts on each loop. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class VideoClipLengthCalculator : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    void Start()
        ///    {
        ///        if (videoPlayer != null)
        ///        {
        ///            videoPlayer = GetComponent<VideoPlayer>();
        ///            videoPlayer.isLooping = true;
        ///            VideoClip videoClip = videoPlayer.clip;
        ///
        ///            if (videoClip != null)
        ///            {
        ///                CalculateVideoLength(videoClip);
        ///                videoPlayer.loopPointReached += OnLoop;
        ///                videoPlayer.Play();
        ///            }
        ///            else
        ///            {
        ///                Debug.LogWarning("VideoClip is not assigned.");
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogWarning("VideoPlayer is not assigned.");
        ///        }
        ///    }
        ///
        ///    void CalculateVideoLength(VideoClip clip)
        ///    {
        ///        // Get frame count and frame rate from the VideoClip. 
        ///        ulong videoClipFrameCount = clip.frameCount;
        ///        double videoClipFrameRate = clip.frameRate;
        ///
        ///        // Calculate the length in seconds (VideoClip) and output to console. 
        ///        double videoClipLengthInSeconds = videoClipFrameCount / videoClipFrameRate; 
        ///        Debug.Log($"Calculated VideoClip length: {videoClipLengthInSeconds} seconds.");
        ///
        ///        // Get frame count and frame rate from the VideoPlayer. 
        ///        ulong videoPlayerFrameCount = videoPlayer.frameCount;
        ///        double videoPlayerFrameRate = videoPlayer.frameRate;
        ///
        ///        // Calculate the length in seconds (VideoPlayer) and output to console. 
        ///        double videoPlayerLengthInSeconds = videoPlayerFrameCount / videoPlayerFrameRate;
        ///        Debug.Log($"Calculated VideoPlayer Length: {videoPlayerLengthInSeconds} seconds.");
        ///
        ///    }
        ///
        ///    void OnLoop(VideoPlayer vp)
        ///    {
        ///        // Recalculate the video length after loop happens. 
        ///        CalculateVideoLength(vp.clip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.frameRate" />
        ///<seealso cref="VideoPlayer.frameCount" />
        public extern ulong frameCount { get; }

        ///<summary>The frame rate of the clip in frames per second. (Read Only).</summary>
        ///<remarks>The frame rate is the number of frames that are displayed in one second of the video clip. This is useful if you want to synchronize with other effects in your project, and monitor performance. However, <see cref="VideoPlayer.frameRate" /> usually gives a more accurate result.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script uses both the [[VideoPlayer]] and [[VideoClip]] components' frame count and frame rate
        /// // to calculate the length of the video in seconds. Sometimes this can return different results, 
        /// // but the VideoPlayer is more accurate, especially after a full playthrough.
        /// // The script recalculates the counts on each loop. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class VideoClipLengthCalculator : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    void Start()
        ///    {
        ///        if (videoPlayer != null)
        ///        {
        ///            videoPlayer = GetComponent<VideoPlayer>();
        ///            videoPlayer.isLooping = true;
        ///            VideoClip videoClip = videoPlayer.clip;
        ///
        ///            if (videoClip != null)
        ///            {
        ///                CalculateVideoLength(videoClip);
        ///                videoPlayer.loopPointReached += OnLoop;
        ///                videoPlayer.Play();
        ///            }
        ///            else
        ///            {
        ///                Debug.LogWarning("VideoClip is not assigned.");
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogWarning("VideoPlayer is not assigned.");
        ///        }
        ///    }
        ///
        ///    void CalculateVideoLength(VideoClip clip)
        ///    {
        ///        // Get frame count and frame rate from the VideoClip. 
        ///        ulong videoClipFrameCount = clip.frameCount;
        ///        double videoClipFrameRate = clip.frameRate;
        ///
        ///        // Calculate the length in seconds (VideoClip) and output to console. 
        ///        double videoClipLengthInSeconds = videoClipFrameCount / videoClipFrameRate; 
        ///        Debug.Log($"Calculated VideoClip length: {videoClipLengthInSeconds} seconds.");
        ///
        ///        // Get frame count and frame rate from the VideoPlayer. 
        ///        ulong videoPlayerFrameCount = videoPlayer.frameCount;
        ///        double videoPlayerFrameRate = videoPlayer.frameRate;
        ///
        ///        // Calculate the length in seconds (VideoPlayer) and output to console. 
        ///        double videoPlayerLengthInSeconds = videoPlayerFrameCount / videoPlayerFrameRate;
        ///        Debug.Log($"Calculated VideoPlayer Length: {videoPlayerLengthInSeconds} seconds.");
        ///
        ///    }
        ///
        ///    void OnLoop(VideoPlayer vp)
        ///    {
        ///        // Recalculate the video length after loop happens. 
        ///        CalculateVideoLength(vp.clip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.frameRate" />
        ///<seealso cref="VideoPlayer.frameCount" />
        public extern double frameRate { get; }

        ///<summary>The length of the video clip in seconds. (Read Only).</summary>
        ///<remarks>You can use this property to ensure any events, sounds, visual effects, logic etc. you want to trigger during the video happen within the time limits of the video. 
        ///
        ///**Note**: The VideoClip can return an inaccurate length because the external encoder can be imprecise. Use <see cref="Video.VideoPlayer.frameCount" /> and <see cref="VideoPlayer.frameRate" /> to get more accurate results.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script outputs the length of the video clip given by the [[VideoClip]] component
        /// // and the length given by calculating the [[VideoPlayer]] frame rate and frame count. These can 
        /// // sometimes give different results, but the VideoPlayer is more accurate, especially after the video
        /// // plays through once. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class VideoClipExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer; 
        ///
        ///    void Start()
        ///    {
        ///        if(videoPlayer != null)
        ///        {
        ///            videoPlayer = GetComponent<VideoPlayer>();
        ///            videoPlayer.isLooping = true;
        ///            VideoClip videoClip = videoPlayer.clip; 
        ///
        ///            if (videoClip != null)
        ///            {
        ///                CalculateVideoLength(videoClip);
        ///                videoPlayer.loopPointReached += OnLoop; 
        ///                videoPlayer.Play(); 
        ///            }
        ///            else
        ///            {
        ///                Debug.LogWarning("VideoClip is not assigned.");
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogWarning("VideoPlayer is not assigned.");
        ///        }
        ///    }
        ///
        ///    void CalculateVideoLength(VideoClip clip)
        ///    {
        ///        // Get frame count and frame rate from the VideoPlayer. 
        ///        ulong frameCount = videoPlayer.frameCount;
        ///        double frameRate = videoPlayer.frameRate;
        ///
        ///        // Calculate the length in seconds. 
        ///        double lengthInSeconds = frameCount / frameRate;
        ///
        ///        // Output the length from the VideoClip and the calculated length. 
        ///        Debug.Log($"Initial clip length: {clip.length} seconds.");
        ///        Debug.Log($"Calculated Length: {lengthInSeconds} seconds.");
        ///    }
        ///
        ///    void OnLoop(VideoPlayer vp)
        ///    {
        ///        // Recalculate the video length with each loop. 
        ///        CalculateVideoLength(vp.clip); 
        ///    }
        ///}]]></code>
        ///</example>
        [NativeName("Duration")]
        public extern double length { get; }

        ///<summary>The width of the images in the video clip in pixels. (Read Only).</summary>
        ///<remarks>You can use this property to help get the dimensions of your video and help display your video properly. Also, you can use it to calculate the image aspect ratio.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates a plane and uses the width and height of the VideoClip to resize the plane. It also plays the video on the plane. 
        /// // You need to assign this script to a GameObject in your Scene, and assign a VideoClip and a Camera in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class WidthHeightExample : MonoBehaviour
        ///{
        ///    public VideoClip videoClip;
        ///    public Camera mainCamera;
        ///
        ///    void Start()
        ///    {
        ///        if (videoClip != null)
        ///        {
        ///            // Create a plane to project the video clip onto.
        ///            GameObject videoPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ///            // Create the VideoPlayer and assign a VideoClip. 
        ///            VideoPlayer videoPlayer = videoPlane.AddComponent<VideoPlayer>();
        ///            videoPlayer.clip = videoClip;
        ///
        ///            // Set the plane to the same position as your GameObject.
        ///            videoPlane.transform.position = transform.position;
        ///            // Rotate the plane to face the camera and adjust. 
        ///            videoPlane.transform.LookAt(mainCamera.transform);
        ///            videoPlane.transform.Rotate(90, 0, 0);
        ///
        ///            // Get the width and height of the VideoClip so that you can resize the plane using these. 
        ///            float videoWidth = videoClip.width;
        ///            float videoHeight = videoClip.height;
        ///
        ///            // Define the scaling factor to control the size of the video plane.
        ///            float scaleFactor = 1000.0f; 
        ///
        ///            // Scale the plane to match the video's aspect ratio.
        ///            float planeWidth = videoWidth / scaleFactor;
        ///            float planeHeight = videoHeight / scaleFactor;
        ///            videoPlane.transform.localScale = new Vector3(planeWidth, 1, planeHeight);
        ///
        ///            videoPlayer.Play();
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("Please assign a video clip.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoClip.height" />
        public extern uint width { get; }

        ///<summary>The height of the images in the video clip in pixels. (Read Only).</summary>
        ///<remarks>You can use this property to help get the dimensions of your video and help display your video properly. Also, you can use it to calculate the image aspect ratio.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates a plane and uses the width and height of the [[VideoClip]] to resize the plane. It also plays the video on the plane. 
        /// // Assign this script to a GameObject in your Scene, and assign a [[VideoClip]] and a [[Camera]] in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class WidthHeightExample : MonoBehaviour
        ///{
        ///    public VideoClip videoClip;
        ///    public Camera mainCamera;
        ///
        ///    void Start()
        ///    {
        ///        if (videoClip != null)
        ///        {
        ///            // Create a plane to project the video clip onto.
        ///            GameObject videoPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ///            // Create the VideoPlayer and assign a VideoClip. 
        ///            VideoPlayer videoPlayer = videoPlane.AddComponent<VideoPlayer>();
        ///            videoPlayer.clip = videoClip;
        ///
        ///            // Set the plane to the same position as your GameObject.
        ///            videoPlane.transform.position = transform.position;
        ///            // Rotate the plane to face the camera and adjust. 
        ///            videoPlane.transform.LookAt(mainCamera.transform);
        ///            videoPlane.transform.Rotate(90, 0, 0);
        ///
        ///            // Get the width and height of the VideoClip so that you can resize the plane using these. 
        ///            float videoWidth = videoClip.width;
        ///            float videoHeight = videoClip.height;
        ///
        ///            // Define the scaling factor to control the size of the video plane.
        ///            float scaleFactor = 1000.0f; 
        ///
        ///            // Scale the plane to match the video's aspect ratio.
        ///            float planeWidth = videoWidth / scaleFactor;
        ///            float planeHeight = videoHeight / scaleFactor;
        ///            videoPlane.transform.localScale = new Vector3(planeWidth, 1, planeHeight);
        ///
        ///            videoPlayer.Play();
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("Please assign a video clip.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoClip.width" />
        ///<seealso cref="VideoClip.length" />
        public extern uint height { get; }

        ///<summary>Returns the numerator of the pixel aspect ratio (numerator:denominator). (Read Only).</summary>
        ///<remarks>The pixel aspect ratio (for example, 10:11) determines the shape of each pixel. The first number (10) is the numerator and is the width of the pixel. The second number (11) is the denominator and is the height of the pixel. You can use the pixel aspect ratio to resize a video to appear less stretched.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates a plane, adds a VideoPlayer component and plays a video on it. 
        /// // It uses the pixel aspect ratio denominator and numerator to resize the plane to prevent the video appearing stretched. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class PixelAspectRatioExample : MonoBehaviour
        ///{
        ///    public VideoClip videoClip;
        ///    public Camera mainCamera;
        ///    void Start()
        ///    {
        ///        if (videoClip != null)
        ///        {
        ///            float numerator = (float)videoClip.pixelAspectRatioNumerator;
        ///            float denominator = (float)videoClip.pixelAspectRatioDenominator;
        ///
        ///            // Create a plane to project the video clip onto.
        ///            GameObject videoPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ///            // Create the VideoPlayer and assign a VideoClip. 
        ///            VideoPlayer videoPlayer = videoPlane.AddComponent<VideoPlayer>();
        ///            videoPlayer.clip = videoClip;
        ///
        ///            // Set the plane to the same position as your GameObject.
        ///            videoPlane.transform.position = transform.position;
        ///
        ///            // Rotate the plane to face the camera and adjust. 
        ///            videoPlane.transform.LookAt(mainCamera.transform);
        ///            videoPlane.transform.Rotate(90, 0, 0);
        ///
        ///            // Scale the plane to match the video's aspect ratio.
        ///            float planeWidth = videoPlane.transform.localScale.x * numerator / denominator;
        ///            videoPlane.transform.localScale = new Vector3(planeWidth, 1, 1);
        ///
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoClip assigned in the Inspector.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoClip.pixelAspectRatioDenominator" />
        public extern uint pixelAspectRatioNumerator { get; }

        ///<summary>Returns the denominator of the pixel aspect ratio (numerator:denominator). (Read Only).</summary>
        ///<remarks>The pixel aspect ratio (for example, 10:11) determines the shape of each pixel. The first number (10) is the numerator and is the width of the pixel. The second number (11) is the denominator and is the height of the pixel. You can use the pixel aspect ratio to resize a video to appear less stretched.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script creates a plane, adds a VideoPlayer component and plays a video on it. 
        /// // It uses the pixel aspect ratio denominator and numerator to resize the plane to prevent the video appearing stretched. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class PixelAspectRatioExample : MonoBehaviour
        ///{
        ///    public VideoClip videoClip;
        ///    public Camera mainCamera;
        ///    void Start()
        ///    {
        ///        if (videoClip != null)
        ///        {
        ///            float numerator = (float)videoClip.pixelAspectRatioNumerator;
        ///            float denominator = (float)videoClip.pixelAspectRatioDenominator;
        ///
        ///            // Create a plane to project the video clip onto.
        ///            GameObject videoPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ///            // Create the VideoPlayer and assign a VideoClip. 
        ///            VideoPlayer videoPlayer = videoPlane.AddComponent<VideoPlayer>();
        ///            videoPlayer.clip = videoClip;
        ///
        ///            // Set the plane to the same position as your GameObject.
        ///            videoPlane.transform.position = transform.position;
        ///
        ///            // Rotate the plane to face the camera and adjust. 
        ///            videoPlane.transform.LookAt(mainCamera.transform);
        ///            videoPlane.transform.Rotate(90, 0, 0);
        ///
        ///            // Scale the plane to match the video's aspect ratio.
        ///            float planeWidth = videoPlane.transform.localScale.x * numerator / denominator;
        ///            videoPlane.transform.localScale = new Vector3(planeWidth, 1, 1);
        ///
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoClip assigned in the Inspector.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoClip.pixelAspectRatioNumerator" />
        public extern uint pixelAspectRatioDenominator { get; }

        ///<summary>Whether the imported clip contains sRGB color data (Read Only).</summary>
        ///<remarks>This setting controls whether sRGB-&gt;Linear color space conversion is done when the <see cref="VideoPlayer" /> is loading the video data into textures. This setting is only relevant when [Linear color space](xref:LinearLighting) is used.
        ///
        ///Most movies store color data in sRGB color space. Set <see cref="P:UnityEditor.VideoClipImporter.sRGBClip" /> to true in most cases.
        ///
        ///Non-color movies are commonly stored as linear values, and the GPU should not perform color space conversions. Set to false in the <see cref="T:UnityEditor.VideoClipImporter" /> for non-color movies.
        ///
        ///This setting corresponds to <see cref="P:UnityEditor.VideoClipImporter.sRGBClip" /> in the video clip importer.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class SRGBExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Get the VideoClip from the VideoPlayer
        ///        VideoClip clip = videoPlayer.clip;
        ///
        ///        if (clip != null)
        ///        {
        ///            // Output if the clip contains sRGB color data.  
        ///            Debug.Log("Does this clip use sRGB color data? : " + clip.sRGB);
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoClip assigned to the VideoPlayer.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="P:UnityEditor.PlayerSettings.colorSpace" />
        public extern bool sRGB { [NativeName("IssRGB")] get; }

        ///<summary>Gets the number of audio tracks that are embedded in the video clip. (RO).</summary>
        ///<remarks>A video clip can contain multiple audio tracks. It can have separate audio tracks for different languages, commentary, accessibility, or to separate music, sound effects and voices. This is useful because you can:
        /// 
        ///* Monitor the different audio tracks.
        ///* Play certain tracks depending on the context or user’s choice. 
        ///* Change the volume on certain tracks.  
        ///* Mute certain tracks. 
        ///
        ///To enable or deactivate a certain audio track from the clip, use <see cref="VideoPlayer.EnableAudioTrack" />.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class AudioTrackCountExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    int currentTrack;
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        VideoClip videoClip = videoPlayer.clip;
        ///
        ///        // Loop through all tracks and deactivate all except the first.
        ///        for (ushort i = 1; i < videoClip.audioTrackCount; i++)
        ///        {
        ///            videoPlayer.EnableAudioTrack(i, false);
        ///        }
        ///        videoPlayer.Play();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Press the Spacebar to change audio track. 
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            ChangeAudioTrack();
        ///        }
        ///    }
        ///
        ///    public void ChangeAudioTrack()
        ///    {
        ///        // VideoPlayer needs to stop before it can change track. 
        ///        videoPlayer.Stop();
        ///
        ///        videoPlayer.EnableAudioTrack((ushort)currentTrack, false);
        ///        currentTrack = (currentTrack + 1) % videoPlayer.audioTrackCount;
        ///        videoPlayer.EnableAudioTrack((ushort)currentTrack, true);
        ///
        ///        videoPlayer.Play();
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoClip.GetAudioLanguage" />
        ///<seealso cref="VideoPlayer.EnableAudioTrack" />
        ///<seealso cref="VideoPlayer.SetDirectAudioMute" />
        ///<seealso cref="VideoPlayer.SetDirectAudioVolume" />
        public extern ushort audioTrackCount { get; }

        ///<summary>Returns the number of channels in the audio track. For example, if the audio track is a stereo track, this function returns 2.</summary>
        ///<remarks>Video files can have multiple audio tracks for various reasons. For example, they can use different tracks to separate: 
        ///
        ///* different languages. 
        ///* accessibility options. 
        ///* high sampling from low sampling tracks. 
        ///* music from sound effects. 
        ///* sounds with different channel counts. 
        ///
        ///This function lets you specify an audio track within a video clip to check the channel counts of each one. 
        ///
        ///The following are further examples of track types and what this function returns for each type: 
        ///
        ///* Mono returns 1.
        ///* Stereo returns 2. 
        ///* Surround sound returns 3 for 2.1, 6 for 5.1, or 8 for 7.1.
        ///
        ///This function is useful because you can use the channel count to adapt to different video clips with different audio qualities.</remarks>
        ///<param name="audioTrackIdx">Use this index to specify which audio track in the video to use.</param>
        ///<returns>The number of channels.</returns>
        ///<example>
        ///  <code><![CDATA[ // This script cycles through a video clip's audio tracks, enables tracks that have 2 channels, and deactivates others. 
        /// // Assign this script and a [[VideoPlayer]] component to a GameObject in your Scene.
        /// // Then assign a video to the VideoPlayer in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class AudioChannelCountExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        VideoPlayer videoPlayer;
        ///        VideoClip videoClip;
        ///        // The amount of channels you want your audio track to have. 
        ///        int preferredAudioChannel = 2;
        ///
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        if(videoPlayer != null )
        ///        {
        ///            videoClip = videoPlayer.clip;
        ///            
        ///            if (videoClip != null)
        ///            {
        ///                // Get the number of audio tracks in the VideoClip.
        ///                int audioTrackCount = videoClip.audioTrackCount;
        ///
        ///                // Loop through each audio track and get the number of channels.
        ///                for (ushort i = 0; i < audioTrackCount; i++)
        ///                {
        ///                    ushort channelCount = videoClip.GetAudioChannelCount(i);
        ///
        ///                    // Enable the track if it has your preferred audio channel count. 
        ///                    if (channelCount == preferredAudioChannel)
        ///                    {
        ///                        videoPlayer.EnableAudioTrack(i, true);
        ///                        Debug.Log("Enabled audio track " + i + " because it has " + channelCount + " channels.");
        ///                    }
        ///                    // Otherwise, deactivate the track. 
        ///                    else
        ///                    {
        ///                        videoPlayer.EnableAudioTrack(i, false);
        ///                        Debug.Log("Deactivated audio track " + i + " because it has " + channelCount + " channels.");
        ///                    }
        ///                }
        ///                videoPlayer.Play();
        ///            }
        ///            else
        ///            {
        ///                Debug.LogError("No VideoClip assigned to VideoPlayer.");
        ///            }
        ///
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoPlayer assigned to GameObject.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.GetAudioChannelCount" />
        ///<seealso cref="VideoPlayer.EnableAudioTrack" />
        ///<seealso cref="VideoClip.GetAudioLanguage" />
        ///<seealso cref="VideoPlayer.GetAudioSampleRate" />
        public extern ushort GetAudioChannelCount(ushort audioTrackIdx);

        ///<summary>Get the audio track sampling rate in hertz (Hz).</summary>
        ///<remarks>The audio sampling rate is the number of times per second a sample of audio is captured. Higher sample rates usually result in more realistic sounds and better sound quality, but files are larger. 
        ///This is useful to know so that you can cater your audio to different devices. A VideoClip could have multiple audio tracks for different quality levels, which you can change depending on the device.</remarks>
        ///<param name="audioTrackIdx">Index of the audio queried audio track.</param>
        ///<returns>The sampling rate in hertz.</returns>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class AudioSampleRateExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Get the VideoClip from the VideoPlayer
        ///        VideoClip clip = videoPlayer.clip;
        ///
        ///        if (clip != null)
        ///        {
        ///            // Get the number of audio tracks in the VideoClip
        ///            int audioTrackCount = clip.audioTrackCount;
        ///
        ///            // Loop through each audio track and output their audio sample rate. 
        ///            for (ushort i = 0; i < audioTrackCount; i++)
        ///            {
        ///                Debug.Log("Audio track " + i + " has the following audio sampling rate: " + clip.GetAudioSampleRate(i));
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoClip assigned to the VideoPlayer.");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.EnableAudioTrack" />
        public extern uint GetAudioSampleRate(ushort audioTrackIdx);

        ///<summary>Gets the language of the video clip’s audio tracks, if the audio tracks have an assigned language.</summary>
        ///<remarks>This returns nothing if the track was created without a specified language. 
        ///You can use this function to switch out audio tracks in your video depending on the language preference of the user. The audio language is normally a 2 or 3 letter language code following the ISO 639-2/T or 639-2/B standards. For example, the code for English is normally “en” or “eng”. Check your audio files to see what codes they have. Some audio tracks don't have language information, in which case this function returns an empty string.</remarks>
        ///<param name="audioTrackIdx">Index of the audio track you want to query in the video.</param>
        ///<returns>The abbreviated name of the language.</returns>
        ///<example>
        ///  <code><![CDATA[ // This script loops through each of the audio tracks to check their languages. If their language matches your preferred language (userLanguage),
        /// // this script enables that audio track and deactivates the other tracks. 
        /// // You need to assign this script to a GameObject in your Scene, and assign a VideoPlayer component to it in the Inspector. 
        /// // Also set userLanguage to the language you want. 
        ///
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public enum Language
        ///{
        ///    English, French, Spanish, German, Japanese, Chinese, Italian, Portuguese, Russian, Korean, Arabic, Danish, Dutch, Finnish, Icelandic
        ///}
        ///
        ///public class GetAudioLanguageExample : MonoBehaviour
        ///{
        ///    // Dictionary to map user-friendly language names to shorthand codes. The codes for your files might be different. 
        ///    public static readonly Dictionary<Language, string> LanguageCodes = new Dictionary<Language, string>
        ///    {
        ///        { Language.English, "eng" },
        ///        { Language.French, "fra" },
        ///        { Language.Spanish, "spa" },
        ///        { Language.German, "deu" },
        ///        { Language.Japanese, "jpn" },
        ///        { Language.Chinese, "zho" },
        ///        { Language.Italian, "ita" },
        ///        { Language.Portuguese, "por" },
        ///        { Language.Russian, "rus" },
        ///        { Language.Korean, "kor" },
        ///        { Language.Arabic, "ara" },
        ///        { Language.Danish, "dan" },
        ///        { Language.Dutch, "nld" },
        ///        { Language.Finnish, "fin"},
        ///        { Language.Icelandic, "isl"}
        ///    };
        ///
        ///    VideoPlayer videoPlayer;
        ///
        ///    // Choose your language. 
        ///    public Language userLanguage;
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Get the VideoClip from the VideoPlayer
        ///        VideoClip clip = videoPlayer.clip;
        ///
        ///        if (clip != null)
        ///        {
        ///            // Get the number of audio tracks in the VideoClip
        ///            int audioTrackCount = clip.audioTrackCount;
        ///            Debug.Log("User chose " + userLanguage.ToString());
        ///
        ///            // Search the dictionary for the user's choice to get the language code. 
        ///            if (LanguageCodes.TryGetValue(userLanguage, out string userLanguageCode))
        ///            {
        ///                Debug.Log("User language was " + userLanguage + " and the code is : " + userLanguageCode);
        ///                // Loop through each audio track see if they have an assigned language that matches your language choice. 
        ///                for (ushort i = 0; i < audioTrackCount; i++)
        ///                {
        ///                    string audioLanguage = clip.GetAudioLanguage(i);
        ///                    Debug.Log("Audio track " + i + " has language: " + audioLanguage);
        ///
        ///                    // If the audio track has the preferred language, enable this audio track. 
        ///                    if (audioLanguage == userLanguageCode)
        ///                    {
        ///                        videoPlayer.EnableAudioTrack(i, true);
        ///                        Debug.Log("Audio track " + i + " was enabled.");
        ///                    }
        ///                    // If the audio track doesn't have the chosen language, disable the track. 
        ///                    else
        ///                    {
        ///                        videoPlayer.EnableAudioTrack(i, false);
        ///                        Debug.Log("Audio track " + i + " was disabled.");
        ///                    }
        ///                }
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.LogError("No VideoClip assigned to the VideoPlayer.");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.EnableAudioTrack" />
        public extern string GetAudioLanguage(ushort audioTrackIdx);
    }
}
