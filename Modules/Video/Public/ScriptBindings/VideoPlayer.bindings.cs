// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    ///<summary>Type of destination for the images read by a VideoPlayer.</summary>
    ///<remarks>Set this enumeration to determine where and how the VideoPlayer renders the images of a video clip.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script switches between the render modes when you press the Spacebar. 
    /// // To set up your project for this script: 
    /// // 1. Attach this script to a GameObject in your scene. 
    /// // 2. Add a VideoPlayer component to your GameObject. 
    /// // 3. Assign a Camera to the script component in the Inspector. 
    /// // 4. Add a plane to your scene. 
    /// // 5. Create a Material (right click in Asset folder, Create > Material). 
    /// // 6. Create a new RenderTexture (right click in Asset folder, Create > Material).
    /// // 7. Assign the RenderTexture to the Inspector of your material (Base Map).
    /// // 8. Assign the material to your plane. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class VideoRenderModeExample : MonoBehaviour
    ///{
    ///    VideoPlayer videoPlayer;
    ///    public RenderTexture renderTexture;
    ///    Camera mainCamera;
    ///
    ///    // Tracks the current render mode. 
    ///    private int renderModeIndex = 0; 
    ///
    ///    void Start()
    ///    {
    ///        mainCamera = Camera.main;
    ///        videoPlayer = GetComponent<VideoPlayer>();
    ///        videoPlayer.isLooping = true; 
    ///        UpdateRenderMode();
    ///    }
    ///
    ///    private void Update()
    ///    {
    ///        // If you press the Spacebar, cycle through the render modes. 
    ///        if (Input.GetKeyDown(KeyCode.Space)) 
    ///        {
    ///            renderModeIndex = (renderModeIndex + 1) % 5;
    ///            UpdateRenderMode();
    ///        }
    ///    }
    ///
    ///    void UpdateRenderMode()
    ///    {
    ///        ClearPreviousRenderMode();
    ///        // Switch render mode when you press the Spacebar. 
    ///        switch (renderModeIndex)
    ///        {
    ///            case 0:
    ///                SwitchToRenderTexture();
    ///                break;
    ///            case 1:
    ///                SwitchToCameraNearPlane();
    ///                break;
    ///            case 2:
    ///                SwitchToCameraFarPlane();
    ///                break;
    ///            case 3:
    ///                SwitchToAPIOnly();
    ///                break;
    ///            case 4:
    ///                SwitchToMaterialOverride();
    ///                break; 
    ///        }
    ///    }
    ///
    ///    // Show video on the surface of an object. 
    ///    void SwitchToRenderTexture()
    ///    {
    ///        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
    ///        videoPlayer.targetTexture = renderTexture;
    ///        Debug.Log("Switched to RenderTexture mode");
    ///    }
    ///
    ///    // Show the video on your screen in front of your Scene.  
    ///    void SwitchToCameraNearPlane()
    ///    {
    ///        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
    ///        videoPlayer.targetCamera = mainCamera;
    ///        Debug.Log("Switched to CameraNearPlane mode");
    ///    }
    ///
    ///    // Show video in the background of your Scene, behind your objects. 
    ///    void SwitchToCameraFarPlane()
    ///    {
    ///        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
    ///        videoPlayer.targetCamera = mainCamera;
    ///        Debug.Log("Switched to CameraFarPlane mode");
    ///    }
    ///
    ///    // Don't show the video anywhere. 
    ///    void SwitchToAPIOnly()
    ///    {
    ///        videoPlayer.renderMode = VideoRenderMode.APIOnly;
    ///        Debug.Log("Switched to APIOnly mode");
    ///    }
    ///
    ///    // Show the video wherever a certain material is applied. 
    ///    void SwitchToMaterialOverride()
    ///    {
    ///        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
    ///        Debug.Log("Switched to Material Override mode");
    ///    }
    ///
    ///    // Clear the previous render target from the VideoPlayer. 
    ///    void ClearPreviousRenderMode()
    ///    {
    ///        // Temporarily disable rendering. 
    ///        videoPlayer.renderMode = VideoRenderMode.APIOnly; 
    ///        videoPlayer.targetTexture = null;
    ///        videoPlayer.targetCamera = null;
    ///
    ///        // Clear the RenderTexture. 
    ///        if (renderTexture != null)
    ///        {
    ///            RenderTexture.active = renderTexture;
    ///            GL.Clear(true, true, Color.clear);
    ///            RenderTexture.active = null;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum VideoRenderMode
    {
        ///<summary>Draw video content behind a camera's scene.</summary>
        ///<remarks>Any transparency in the video will reveal the scene's actual background or skybox. Use for videos that play in the background of a scene.</remarks>
        CameraFarPlane   = 0,
        ///<summary>Draw video content in front of a camera's scene.</summary>
        ///<remarks>Scene content is visible through any transparent areas of the video content.
        ///
        ///Use for cutscenes, splashscreens and videos that overlay the scene. 
        ///Since the VideoPlayer has a transparency control, you can use this render mode to display the video on top of an active scene and still see as much of the scene as you want behind it. To control the transparency, use <see cref="VideoPlayer.targetCameraAlpha" />.
        ///
        ///The <see cref="Camera" /> clipping planes determine where video content is rendered. Video content is offset by a factor of 0.00005.
        ///To render scene content on top of the video, you have to position it between <see cref="Camera.nearClipPlane" /> and  <see cref="Camera.nearClipPlane" /> + (<see cref="Camera.farClipPlane" /> - <see cref="Camera.nearClipPlane" />) * 0.00005.</remarks>
        CameraNearPlane  = 1,
        ///<summary>Draw video content into a RenderTexture.</summary>
        ///<remarks>Use this setting to play your video on a 3D surface in your scene. For example, you can use this to show videos on TV/computer screens in your scene. 
        ///You can display your video on multiple objects that share the same RenderTexture. You can also use this to add post-processing effects and shaders to your video content. If you want to show the video on one object, it might be more efficient to use <see cref="MaterialOverride" /> instead. 
        ///When you use this mode, if the target RenderTexture has the same resolution as the content played by the VideoPlayer, this will enable an internal optimization that saves a texture copy, which skips unnecessary steps and improves performance.</remarks>
        RenderTexture    = 2,
        ///<summary>Draw the video content into a user-specified property of the current GameObject's material.</summary>
        ///<remarks>In this mode, the <see cref="VideoPlayer" /> writes each decoded frame directly into a texture property of the material on a target <see cref="Renderer" />. Use this mode to show video on the texture of scene objects such as televisions or computer screens.
        ///
        ///- Use <see cref="Video.VideoPlayer.targetMaterialRenderer" /> to select which <see cref="Renderer" /> receives the video. If you don't set this property, the <see cref="VideoPlayer" /> uses the first <see cref="Renderer" /> on the same <see cref="GameObject" />.
        ///- Use <see cref="Video.VideoPlayer.targetMaterialProperty" /> to choose which texture property the video binds to. If you don't set this property, the <see cref="VideoPlayer" /> uses the main texture of the material.
        ///
        ///Compared with <see cref="VideoRenderMode.RenderTexture" />, this mode doesn't require a <see cref="RenderTexture" /> asset or extra material setup. The video targets the existing material directly.
        ///
        ///The video writes into one specific texture property of the material. To show the same video on multiple GameObject instances with this mode, each GameObject needs its own <see cref="VideoPlayer" /> or its own material instance. To share a single video across many GameObject instances from one <see cref="VideoPlayer" />, use <see cref="VideoRenderMode.RenderTexture" /> instead. Assign the same <see cref="RenderTexture" /> to each material that displays the video.</remarks>
        MaterialOverride = 3,
        ///<summary>Don't draw the video content anywhere, but still make it available via the VideoPlayer's texture property in the API.</summary>
        ///<remarks>You can use this render mode to play the video into a Canvas' RawImage. You can assign the <see cref="VideoPlayer.texture" /> directly to &lt;a href="https://docs.unity3d.com/Packages/com.unity.ugui@3.0/manual/script-RawImage.html"&gt;
        ///RawImage texture&lt;/a&gt;. This lets you use video in your UI elements or any place where the API allows a texture to be assigned.</remarks>
        APIOnly          = 4
    }

    ///<summary>Types of 3D content layout within a video.</summary>
    ///<remarks>Use this enum to change how the VideoPlayer displays 3D stereoscopic videos. 3D videos include slightly different views for the left and right eyes, which the video combines to create the illusion of depth.
    ///
    ///These formats give you more flexibility to cater to different types of tools that were used to create the stereoscopic movie, and what are the limitations of the platform decoding the movie for rendering. 
    ///
    ///The choice of stereoscopic video layout offers you more flexibility because it's easier to adapt to the different tools used to create the 3D movie and the limitations of the platform decoding it. If the platform or decoder can't handle wide resolutions (for example 3840x1080 for side-by-side HD), you can switch to the over/under layout (e.g., 1920x2160) to solve the issue. Both layouts use the same number of pixels but differ in aspect ratio, so it's easier to adapt.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script sets a video's layout to 3D and plays the video. 
    /// // Attach a VideoPlayer that contains a 3D video clip. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class Video3DLayoutExample : MonoBehaviour
    ///{
    ///    public VideoPlayer videoPlayer;
    ///
    ///    void Start()
    ///    {
    ///        // Set the video layout to Side-by-Side 3D.
    ///        videoPlayer.targetCamera3DLayout = Video3DLayout.SideBySide3D;
    ///
    ///        // Play the video. 
    ///        videoPlayer.Play();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum Video3DLayout
    {
        ///<summary>Use this setting if the video doesn't have any 3D content.</summary>
        ///<remarks>The video doesn’t contain 3D content and the VideoPlayer will treat it as a 2D flat video. There is no distinction between what shows in the left and right eye.</remarks>
        No3D         = 0,
        ///<summary>Video contains 3D content where the left eye occupies the left half and right eye occupies the right half of video frames.</summary>
        ///<remarks>Use this setting for VR and 3D videos that support side-by-side format. VR and 3D videos need different eye perspectives to create depth. Useful for work with VR headsets, 3D TVs, and 360 degree videos.</remarks>
        SideBySide3D = 1,
        ///<summary>Video contains 3D content where the left eye occupies the upper half and right eye occupies the lower half of video frames.</summary>
        ///<remarks>Use this setting for VR and 3D videos that use the over-under format. VR and 3D videos need different eye perspectives to create depth. Useful for work with VR headsets, 3D TVs, and 360 degree videos.</remarks>
        OverUnder3D  = 2
    }

    ///<summary>Use these methods to fit a video into your target area.</summary>
    ///<remarks>This enum gives you several options to manage how the VideoPlayer renders the video to a target area. To do this, the VideoPlayer adjusts or maintains the video's original aspect ratio. 
    ///If you render a video into a <see cref="RenderTexture" />, and the aspect ratio of the <see cref="RenderTexture" /> doesn't match that of the video being rendered, the <see cref="VideoPlayer" /> automatically adds black bars around the video. If you render a video into a <see cref="Camera" />, any area around the video remains transparent.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script switches between the aspect ratios when you press the Spacebar. 
    /// // To set up your project for this script: 
    /// // 1. Attach this script to a GameObject in your Scene. 
    /// // 2. Add a VideoPlayer component to your GameObject.
    /// // 3. Assign a VideoClip to your VideoPlayer. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class VideoRenderModeExample : MonoBehaviour
    ///{
    ///    VideoPlayer videoPlayer;
    ///    
    ///    void Start()
    ///    {
    ///        videoPlayer = GetComponent<VideoPlayer>();
    ///
    ///        // Make video play over the Scene. 
    ///        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
    ///        Camera mainCamera = Camera.main; 
    ///        videoPlayer.targetCamera = mainCamera;
    ///
    ///        // Loop the video.
    ///        videoPlayer.isLooping = true;
    ///
    ///        // Set the default aspect ratio.
    ///        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
    ///
    ///        // Play the video.
    ///        videoPlayer.Play();
    ///    }
    ///
    ///    private void Update()
    ///    {
    ///        // If you press the Spacebar, cycle through the aspect ratios.
    ///        if (Input.GetKeyDown(KeyCode.Space))
    ///        {
    ///            VideoAspectRatio currentAspectRatio = videoPlayer.aspectRatio;
    ///
    ///            // Cycle through the enum values and loop back around. 
    ///            int nextAspectRatio = ((int)currentAspectRatio + 1) % System.Enum.GetValues(typeof(VideoAspectRatio)).Length;
    ///
    ///            // Apply the new aspect ratio.
    ///            videoPlayer.aspectRatio = (VideoAspectRatio)nextAspectRatio;
    ///
    ///            Debug.Log($"Switched aspect ratio to: {videoPlayer.aspectRatio}");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum VideoAspectRatio
    {
        ///<summary>Preserve the pixel size without adjusting for target area.</summary>
        ///<remarks>Use this setting if you want to keep the original aspect ratio of the video and keep its original exact resolution. If the video is smaller than the target area, black bars will show.</remarks>
        NoScaling       = 0,
        ///<summary>Resize the image proportionally so that the height fits the target area.</summary>
        ///<remarks>The <see cref="VideoPlayer" /> automatically crops the image if needed.</remarks>
        FitVertically   = 1,
        ///<summary>Resize the image proportionally so that the width fits the target area.</summary>
        ///<remarks>The <see cref="VideoPlayer" /> automatically crops the image if needed.</remarks>
        FitHorizontally = 2,
        ///<summary>Resize the image proportionally so that the content fits the target area.</summary>
        ///<remarks>Use this setting to resize the video to fit within the bounds of an object, but keep the video’s dimensions. This resize avoids distortion and cropping but might add visible black borders if needed.</remarks>
        FitInside       = 3,
        ///<summary>Resize the image proportionally so that the content fits the target area. The <see cref="VideoPlayer" /> automatically crops the image if needed.</summary>
        ///<remarks>Use this setting to make the video cover the entire object, but keep the video’s dimensions. This setting avoids distortion and black bars. Useful if you want to cover areas like skyboxes or backgrounds where you don't want black bars to show. The VideoPlayer automatically crops the image if needed.</remarks>
        FitOutside      = 4,
        ///<summary>Resize the image non-proportionally to fit the target area.</summary>
        ///<remarks>Use this setting to resize the video to cover the entire object but ignore the original dimensions of the video. Avoids black bars but might cause distortion.</remarks>
        Stretch         = 5
    }

    ///<summary>[DEPRECATED] Time source followed by the <see cref="UnityEngine.Video.VideoPlayer" /> when reading content.</summary>
    [RequiredByNativeCode]
    [System.Obsolete("VideoTimeSource is deprecated. Use TimeUpdateMode instead. (UnityUpgradable) -> VideoTimeUpdateMode")]
    public enum VideoTimeSource
    {
        ///<summary>The audio hardware clock.</summary>
        [System.Obsolete("AudioDSPTimeSource is deprecated. Use DSPTime instead. (UnityUpgradable) -> DSPTime")]
        AudioDSPTimeSource = 0,
        ///<summary>The unscaled game time as defined by Time.realtimeSinceStartup.</summary>
        [System.Obsolete("GameTimeSource is deprecated. Use GameTime instead. (UnityUpgradable) -> GameTime")]
        GameTimeSource     = 1
    }

    ///<summary>The clock that the <see cref="UnityEngine.Video.VideoPlayer" /> observes to detect and correct drift.</summary>
    ///<remarks>Use these settings to control how the playback time of the video is synchronized with time sources.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script changes the video playback speed based on a custom timer. 
    /// // Attach this script and a VideoPlayer component to a GameObject in your Scene. 
    /// // Assign a video clip to the VideoPlayer. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class VideoTimeReferenceExample : MonoBehaviour
    ///{
    ///    VideoPlayer videoPlayer; 
    ///    float customExternalTime = 0.0f;
    ///    float timeSlider = 1.0f;
    ///
    ///    private void OnGUI()
    ///    {
    ///        // Create a slider you can use to change the playback speed. 
    ///        GUI.Label(new Rect(0, 0, 300, 300),$"Time scale : {timeSlider}");
    ///        timeSlider = GUI.HorizontalSlider(new Rect(0, 30, 300, 300), timeSlider, 1.0f, 3.0f);
    ///    }
    ///
    ///    void Start()
    ///    {
    ///        videoPlayer = GetComponent<VideoPlayer>();
    ///        if (!videoPlayer)
    ///        {
    ///            Debug.LogError("VideoPlayer not assigned!");
    ///            return;
    ///        }
    ///
    ///        // Set default time reference to External time. 
    ///        videoPlayer.timeReference = VideoTimeReference.ExternalTime;
    ///        videoPlayer.Play();
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        // Increment custom time manually. 
    ///        customExternalTime += timeSlider * Time.deltaTime;
    ///        videoPlayer.externalReferenceTime = customExternalTime;
    ///    }
    ///
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum VideoTimeReference
    {
        ///<summary>The video plays without influence from external time sources.</summary>
        ///<remarks>VideoPlayer will use a reasonable free running time source and will never correct for drift.</remarks>
        Freerun         = 0,
        ///<summary>The internal reference clock the <see cref="UnityEngine.Video.VideoPlayer" /> observes to detect and correct drift.</summary>
        ///<remarks>The VideoPlayer relies on its own internal clock to manage the timing and playback of the video. Unity uses <see cref="Time.realtimeSinceStartup" /> as a point of reference to detect and correct any drift during playback.
        ///
        ///This timing is separate from Unity’s global time or any system time references. This means that if you pause or change the application's <see cref="Time.timeScale" />, the video will continue to play as normal.</remarks>
        InternalTime    = 1,
        ///<summary>The external reference clock the <see cref="UnityEngine.Video.VideoPlayer" /> observes to detect and correct drift.</summary>
        ///<remarks>The VideoPlayer plays the video based on an external time source. External time sources include your own custom timers, or a system’s time. To set this, use <see cref="VideoPlayer.externalReferenceTime" />.</remarks>
        ExternalTime    = 2
    }

    ///<summary>Source of the video content for a VideoPlayer.</summary>
    ///<remarks>A VideoPlayer can contain valid references to both a <see cref="Video.VideoClip" /> and a URL at the same time. This enum expresses which of the two should be used for playback.</remarks>
    [RequiredByNativeCode]
    public enum VideoSource
    {
        ///<summary>Use the current clip as the video content source.</summary>
        VideoClip = 0,
        ///<summary>Use the current URL as the video content source.</summary>
        Url       = 1
    }

    ///<summary>Defines the time source the VideoPlayer uses to update the timing of the video playback.</summary>
    ///<remarks>Use these settings to synchronize your video with audio, gameplay scaled time or unscaled time.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script switches between the time update modes when you press the Spacebar.
    /// // It also creates a slider that you can use to change the timescale of the game. 
    /// // The changes are visible on your video when you enter the GameTime time update mode.
    ///
    /// // To set up your project for this script: 
    /// // 1. Attach this script to a GameObject in your Scene. 
    /// // 2. Add a VideoPlayer component to your GameObject.
    /// // 3. Assign a VideoClip to your VideoPlayer. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class VideoRenderModeExample : MonoBehaviour
    ///{
    ///    VideoPlayer videoPlayer;
    ///    
    ///    void Start()
    ///    {
    ///        videoPlayer = GetComponent<VideoPlayer>();
    ///
    ///        // Make video play in background of Scene. 
    ///        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
    ///        Camera mainCamera = Camera.main;
    ///        videoPlayer.targetCamera = mainCamera;
    ///
    ///        // Loop the video.
    ///        videoPlayer.isLooping = true;
    ///
    ///        // Play the video.
    ///        videoPlayer.Play();
    ///    }
    ///
    ///    private void OnGUI()
    ///    {
    ///        // Slider to alter the time scale of the game. 
    ///        GUI.Label(new Rect(0, 20, 300, 300), $"Time scale: {Time.timeScale:F1}");
    ///        Time.timeScale = GUI.HorizontalSlider(new Rect(100, 20, 300, 1000), Time.timeScale, 0.0f, 5.0f);
    ///    }
    ///
    ///    private void Update()
    ///    {
    ///        // If you press the Spacebar, cycle through the time update modes.
    ///        if (Input.GetKeyDown(KeyCode.Space))
    ///        {
    ///            VideoTimeUpdateMode currentVideoTimeUpdateMode = videoPlayer.timeUpdateMode;
    ///            // Cycle through the enum values and loop back around. 
    ///            int nextTimeUpdate = ((int)currentVideoTimeUpdateMode + 1) % System.Enum.GetValues(typeof(VideoTimeUpdateMode)).Length;
    ///
    ///            videoPlayer.timeUpdateMode = (VideoTimeUpdateMode)nextTimeUpdate;
    ///
    ///            Debug.Log($"Switched time update to: {videoPlayer.timeUpdateMode}");
    ///
    ///            if (!videoPlayer.isPlaying)
    ///            {
    ///                videoPlayer.Play();
    ///            }
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum VideoTimeUpdateMode
    {
        ///<summary>Update time based on the DSP (Digital Signal Processing) clock. Use this value to synchronize playback with Audio.</summary>
        ///<remarks>Use this setting if you want the video to synchronize with the audio even if the frame rate drops or changes.</remarks>
        DSPTime          = 0,
        ///<summary>Update the VideoPlayer's time based on <see cref="Time.time" />.</summary>
        ///<remarks>Use this value to synchronize playback with gameplay and to pause updates when gameplay is paused.</remarks>
        GameTime         = 1,
        ///<summary>Update the VideoPlayer's time based on <see cref="Time.unscaledTime" />.</summary>
        ///<remarks>Use this value to synchronize playback with gameplay and to continue updates when gameplay is paused. For example, use this mode for menu transitions that are updated while the game is paused.</remarks>
        UnscaledGameTime = 2
    }

    ///<summary>Places where the audio embedded in a video can be sent.</summary>
    ///<remarks>Use this enum to mute your audio, output your audio through Unity’s audio system, or output the audio directly to the audio hardware.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This script changes the audio output of a video when you press the Spacebar. 
    /// // Attach this script, an AudioSource component and a VideoPlayer component to a GameObject in your Scene. 
    /// // Assign a video clip to the VideoPlayer. 
    /// // Edit the values of the AudioSource so that it edits the audio when it enters that mode. 
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class SwitchAudioOutputMode : MonoBehaviour
    ///{
    ///    VideoPlayer videoPlayer; 
    ///    AudioSource audioSource; 
    ///    private int currentAudioOutputMode = 0; 
    ///
    ///    void Start()
    ///    {
    ///        videoPlayer = GetComponent<VideoPlayer>();
    ///        audioSource = GetComponent<AudioSource>();
    ///
    ///        if (videoPlayer == null)
    ///        {
    ///            Debug.LogError("No VideoPlayer assigned!");
    ///            return;
    ///        }
    ///
    ///        // Set the initial audio output mode. 
    ///        SetAudioOutputMode(VideoAudioOutputMode.None);
    ///
    ///        videoPlayer.Play();
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        // Press Spacebar to switch between modes
    ///        if (Input.GetKeyDown(KeyCode.Space))
    ///        {
    ///            CycleAudioOutputMode();
    ///        }
    ///    }
    ///
    ///    private void CycleAudioOutputMode()
    ///    {
    ///        // Cycle through the VideoAudioOutputMode enum values. 
    ///        currentAudioOutputMode = (currentAudioOutputMode + 1) % System.Enum.GetValues(typeof(VideoAudioOutputMode)).Length;
    ///
    ///        // Apply the new audio output mode. 
    ///        SetAudioOutputMode((VideoAudioOutputMode)currentAudioOutputMode);
    ///    }
    ///
    ///    private void SetAudioOutputMode(VideoAudioOutputMode audioOutputMode)
    ///    {
    ///        // Stop video before the audio output changes. 
    ///        videoPlayer.Stop(); 
    ///        videoPlayer.audioOutputMode = audioOutputMode;
    ///
    ///        switch (audioOutputMode)
    ///        {
    ///            // The video plays without audio. 
    ///            case VideoAudioOutputMode.None:
    ///                Debug.Log("Audio Output Mode: None");
    ///                break;
    ///
    ///            // The video plays audio through an AudioSource. 
    ///            case VideoAudioOutputMode.AudioSource:
    ///                if (audioSource == null)
    ///                {
    ///                    Debug.LogError("AudioSource not assigned! Unable to set AudioSource mode.");
    ///                    return;
    ///                }
    ///
    ///                // Link the VideoPlayer to the AudioSource for playback.
    ///                videoPlayer.SetTargetAudioSource(0, audioSource);
    ///                Debug.Log("Audio Output Mode: AudioSource");
    ///                break;
    ///
    ///                // Play the audio from the video unaltered. 
    ///            case VideoAudioOutputMode.Direct:
    ///                Debug.Log("Audio Output Mode: Direct");
    ///                break;
    ///
    ///             
    ///            case VideoAudioOutputMode.APIOnly:
    ///                Debug.Log("Audio Output Mode: APIOnly (Raw audio samples exposed)");
    ///                break;
    ///
    ///            default:
    ///                Debug.LogError("Unexpected Audio Output Mode!");
    ///                break;
    ///        }
    ///
    ///        // Restart video playback with the new output mode. 
    ///        videoPlayer.Play(); 
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    public enum VideoAudioOutputMode
    {
        ///<summary>Disable the embedded audio.</summary>
        ///<remarks>The embedded audio doesn’t play. Use if you want your video to play without sound, or if you want to replace the sound with a different audio clip.</remarks>
        None        = 0,
        ///<summary>Send the embedded audio into a specified <see cref="AudioSource" />.</summary>
        ///<remarks>You can add effects and filters to your audio, or alter the settings of the AudioSource to change how your video’s audio sounds. If you use this setting, you need to assign an AudioSource to your VideoPlayer. To assign an AudioSource, use <see cref="VideoPlayer.SetTargetAudioSource" />.</remarks>
        AudioSource = 1,
        ///<summary>Send the embedded audio direct to the platform's audio hardware.</summary>
        ///<remarks>The VideoPlayer bypasses Unity’s AudioSource and plays the video with its original audio directly to audio hardware (speakers, headphones etc.). Useful if you don’t want to alter the audio or you want to save resources.</remarks>
        Direct      = 2,
        ///<summary>Send the embedded audio to the associated <see cref="UnityEngine.Experimental.Audio.AudioSampleProvider" />.</summary>
        ///<remarks>Use this setting if you want to use Unity scripting to alter the video’s raw audio data. This is useful if you want direct, low-level control over the audio, or to analyse the data. 
        ///Since the audio data goes through scripts instead of to the system's speakers or an Audio Mixer, the audio won't automatically play or be heard by the user. Therefore, if you want audio playback, you need to implement a way to process and output the data to suit your needs (via third-party plugins or custom implementations.</remarks>
        APIOnly     = 3
    }

    ///<summary>Plays video content onto a target.</summary>
    ///<remarks>Content can be either a <see cref="VideoClip" /> imported asset or a URL such as <c>file://</c> or <c>http://</c>. Video content will be projected onto one of the supported targets, such as camera background or <see cref="RenderTexture" />.
    ///If the video content includes transparency, this transparency will be present in the target, allowing objects behind the video target to be visible. When the data <see cref="VideoPlayer.source" /> is set to URL, the audio and video description of what is being played will only be initialized once the <see cref="VideoPlayer" /> preparation is completed. You can test this with <see cref="VideoPlayer.isPrepared" />.
    ///
    ///Refer to [Video file compatibility](xref:VideoSources-FileCompatibility) for more information on supported video file formats.
    ///
    ///**The following demonstrates a few features of the VideoPlayer: **</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// // Examples of VideoPlayer function
    ///
    ///using UnityEngine;
    ///using UnityEngine.Video;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        // Will attach a VideoPlayer to the main camera.
    ///        GameObject camera = GameObject.Find("Main Camera");
    ///
    ///        // VideoPlayer automatically targets the camera backplane when it is added
    ///        // to a camera object, no need to change videoPlayer.targetCamera.
    ///        var videoPlayer = camera.AddComponent<UnityEngine.Video.VideoPlayer>();
    ///
    ///        // Play on awake defaults to true. Set it to false to avoid the url set
    ///        // below to auto-start playback since we're in Start().
    ///        videoPlayer.playOnAwake = false;
    ///
    ///        // By default, VideoPlayers added to a camera will use the far plane.
    ///        // Let's target the near plane instead.
    ///        videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraNearPlane;
    ///
    ///        // This will cause our Scene to be visible through the video being played.
    ///        videoPlayer.targetCameraAlpha = 0.5F;
    ///
    ///        // Set the video to play. URL supports local absolute or relative paths.
    ///        // Here, using absolute.
    ///        videoPlayer.url = "/Users/graham/movie.mov";
    ///
    ///        // Skip the first 100 frames.
    ///        videoPlayer.frame = 100;
    ///
    ///        // Restart from beginning when done.
    ///        videoPlayer.isLooping = true;
    ///
    ///        // Each time we reach the end, we slow down the playback by a factor of 10.
    ///        videoPlayer.loopPointReached += EndReached;
    ///
    ///        // Start playback. This means the VideoPlayer may have to prepare (reserve
    ///        // resources, pre-load a few frames, etc.). To better control the delays
    ///        // associated with this preparation one can use videoPlayer.Prepare() along with
    ///        // its prepareCompleted event.
    ///        videoPlayer.Play();
    ///    }
    ///
    ///    void EndReached(UnityEngine.Video.VideoPlayer vp)
    ///    {
    ///        vp.playbackSpeed = vp.playbackSpeed / 10.0F;
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequiredByNativeCode]
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("VideoPlayer", PersistentTypeId = 328)]
    [NativeHeader("Modules/Video/Public/VideoPlayer.h")]
    public sealed partial class VideoPlayer : Behaviour
    {
        internal VideoPlayer(global::UnityEngine.EntityId id) : base(id) {}
        public VideoPlayer() {}
        ///<summary>The source that the <see cref="VideoPlayer" /> uses for playback.</summary>
        ///<remarks>It is valid to set both a <see cref="VideoClip" /> and a URL in the <see cref="VideoPlayer" />. This property controls which one will get used for playback.
        ///When setting a new clip or URL, the source will automatically change to make the associated type current.
        ///
        ///**Note:** On WebGL, only <see cref="VideoSource.Url" /> is supported.</remarks>
        public extern VideoSource source { get; set; }
        ///<summary>The clock source used by the <see cref="VideoPlayer" /> to derive its current time.</summary>
        public extern VideoTimeUpdateMode timeUpdateMode { get; set; }

        ///<summary>The file URL or web URL that the <see cref="VideoPlayer" /> reads content from.</summary>
        ///<remarks>In addition to URLs, this property also accepts raw paths to local files. The raw paths can either be absolute on the platform or relative to the Player root.
        ///
        ///If the user sets both a <see cref="VideoPlayer.clip" /> and a <see cref="VideoPlayer.url" />, the one that was set last takes precedence.
        ///
        ///**Examples**:</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine; 
        ///using UnityEngine.Video; 
        ///
        ///public class UrlExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    private void Start()
        ///    {
        ///        // Get the VideoPlayer component from the GameObject that contains this script.  
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///
        ///        // Using an absolute raw path to a local file.
        ///        videoPlayer.url = "/Users/graham/movie.mov";
        ///
        ///        // Using a relative raw path to a local file.
        ///        videoPlayer.url = "subdirectory/videofiles/movie.mov";
        ///
        ///        // Using a web URL.
        ///        videoPlayer.url = "https://ia904602.us.archive.org/25/items/big-buck-bunny_202112/Big%20Buck%20Bunny.mp4";
        ///
        ///        // Using a file URL.
        ///        videoPlayer.url = "file:///Users/graham/movie.mov";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeName("VideoUrl")]
        public extern string url { get; set; }

        ///<summary>The clip being played by the <see cref="VideoPlayer" />.</summary>
        ///<remarks>The <see cref="VideoPlayer" /> may reference both a clip and a URL at the same time. The last one that was set takes precedence.
        ///                Setting this to <c>null</c> will stop any currently prepared playback.
        ///
        ///**Note:** Not supported on WebGL. Only <see cref="VideoPlayer.url" /> is supported on this platform.</remarks>
        [NativeName("VideoClip")]
        public extern VideoClip clip { get; set; }

        ///<summary>Where the video content will be drawn.</summary>
        ///<remarks>This gets automatically set to the most appropriate value when creating a <see cref="VideoPlayer" />. For example, adding a <see cref="VideoPlayer" /> on a <see cref="Camera" /> will result in the <see cref="VideoPlayer" /> initializing its target to be the <see cref="Camera" /> background.</remarks>
        public extern VideoRenderMode renderMode { get; set; }

        ///<summary>Whether you can change the time source followed by the <see cref="VideoPlayer" />. (RO)</summary>
        ///<remarks>Certain playback engines can only follow their own internal clock.
        ///
        ///This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canSetTimeUpdateMode
        {
            [NativeName("CanSetTimeUpdateMode")]
            get;
        }

        ///<summary>
        ///  <see cref="Camera" /> component to draw to when <see cref="Video.VideoPlayer.renderMode" /> is set to either <see cref="Video.VideoRenderMode.CameraFarPlane" /> or <see cref="Video.VideoRenderMode.CameraNearPlane" />.</summary>
        [NativeHeader("Runtime/Camera/Camera.h")]
        public extern Camera targetCamera { get; set; }

        ///<summary>
        ///  <see cref="RenderTexture" /> to draw to when <see cref="Video.VideoPlayer.renderMode" /> is set to <see cref="Video.VideoRenderMode.RenderTexture" />.</summary>
        ///<remarks>If the <see cref="RenderTexture" /> is of <see cref="UnityEngine.Rendering.TextureDimension.Tex2D" /> the video frames will be drawn directly into this target. For optimal performance, <see cref="RenderTexture.width" /> and <see cref="RenderTexture.height" /> should match those of the video media exactly.
        ///
        ///If the <see cref="RenderTexture" /> is of <see cref="UnityEngine.Rendering.TextureDimension.Cube" /> the video frames will be interpreted as a cubemap in one of the 4 supported layouts (horizontal or vertical orientation of a cross or strip layout) based on video aspect ratio. The cubemap faces of the video frame are drawn to the 6 faces of the <see cref="RenderTexture" />. For a one-to-one pixel mapping, <see cref="RenderTexture.width" /> and <see cref="RenderTexture.height" /> should match the size of the individual faces contained within the video media's cubemap (eg. for a 2048x1536 horizontal cross cubemap video, the <see cref="RenderTexture" /> cube size should be set to 512x512).</remarks>
        [NativeHeader("Runtime/Graphics/RenderTexture.h")]
        public extern RenderTexture targetTexture { get; set; }

        ///<summary>
        ///  <see cref="Renderer" /> which is targeted when <see cref="Video.VideoPlayer.renderMode" /> is set to <see cref="VideoRenderMode.MaterialOverride" /></summary>
        ///<remarks>Setting this to null causes the <see cref="VideoPlayer" /> to use the first <see cref="Renderer" /> of the current <see cref="GameObject" />.</remarks>
        [NativeHeader("Runtime/Graphics/Renderer.h")]
        public extern Renderer targetMaterialRenderer { get; set; }

        ///<summary>
        ///  <see cref="Material" /> texture property which is targeted when <see cref="Video.VideoPlayer.renderMode" /> is set to <see cref="VideoRenderMode.MaterialOverride" />.</summary>
        ///<remarks>The video is sent to every <see cref="Material" /> in the <see cref="Renderer" /> that has the targeted texture property. When this property is empty, the <see cref="VideoPlayer" /> uses the name of the material's first <see cref="Material.mainTexture">main texture</see>. If no main texture is found, the <see cref="VideoPlayer" /> uses the name of the material's first texture property.</remarks>
        public extern string targetMaterialProperty { get; set; }

        [VisibleToOtherModules("UnityEditor.VideoModule")]
        internal extern string effectiveTargetMaterialProperty { get; }

        ///<summary>Defines how the video content will be stretched to fill the target area.</summary>
        public extern VideoAspectRatio aspectRatio { get; set; }

        ///<summary>Overall transparency level of the target camera plane video.</summary>
        ///<remarks>This level, in range [0.0, 1.0], is applied in addition of the transparency that may be embedded in the video frames.</remarks>
        public extern float targetCameraAlpha { get; set; }

        ///<summary>Type of 3D content contained in the source video media.</summary>
        ///<remarks>When stereoscopic rendering is used, the rendering for each eye samples the correct half of the video according to this setting. This setting is only used when <see cref="Video.VideoPlayer.renderMode" /> is set to either <see cref="VideoRenderMode.CameraFarPlane" /> or <see cref="VideoRenderMode.CameraNearPlane" />.</remarks>
        public extern Video3DLayout targetCamera3DLayout { get; set; }

        ///<summary>Internal texture in which video content is placed. (RO)</summary>
        ///<remarks>The texture is used to send the video content to the desired target. When the <see cref="Video.VideoPlayer.renderMode" /> is set to <see cref="VideoRenderMode.APIOnly" />, the content is still accessible from scripts using this property.</remarks>
        [NativeHeader("Runtime/Graphics/Texture.h")]
        public extern Texture texture { get; }

        ///<summary>Prepares the playback engine so that it's ready for playback.</summary>
        ///<remarks>To prepare, the playback engine reserves the resources vital for playback, and preloads some of the content to be played.  If the preparation succeeds, this method emits the <see cref="VideoPlayer.prepareCompleted" /> event and sets <see cref="VideoPlayer.isPrepared" /> to true. The VideoPlayer is then ready to display the frames immediately and you can access all properties related to the source. 
        ///
        ///If you don't prepare the VideoPlayer before you play a video, the <see cref="VideoPlayer.Play" /> method will do the preparation but the video won't play instantly. 
        ///                
        ///If you use <see cref="VideoPlayer.Stop" />, the VideoPlayer becomes unprepared again because it frees its resources for performance reasons. To halt a video but keep its preparation, use <see cref="VideoPlayer.Pause" /> instead.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.UI;
        ///using UnityEngine.Video;
        /// 
        /// // The button to play the video in the script only becomes interactable after the preparation is finished.
        /// // To start the preparation process, press the Space key in Play mode. 
        ///
        /// // Attach this script to the GameObject you want to play a video clip on. 
        /// // Attach a VideoPlayer component with a video clip and assign a UI Button in the Inspector.
        ///
        ///public class PrepareExample: MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///    public Button playButton;
        ///
        ///    private void Awake()
        ///    {
        ///        // Get the VideoPlayer component attached to GameObject with this script attached.  
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Attach the event handler, which triggers when the VideoPlayer finishes its preparation. 
        ///        videoPlayer.prepareCompleted += OnPrepareCompleted;
        ///        videoPlayer.playOnAwake = false;
        ///        playButton.interactable = false;
        ///    }
        ///
        ///    // Event handler for when VideoPlayer finishes the preparation process. 
        ///    void OnPrepareCompleted(VideoPlayer vp)
        ///    {
        ///        Debug.Log("Preparation complete. You can now play the video.");
        ///        
        ///        // Preparation is complete so allow interactions with the play button. 
        ///        playButton.interactable = true;
        ///        playButton.onClick.AddListener(OnPlayButtonClicked);
        ///    }
        ///
        ///    void OnPlayButtonClicked()
        ///    {
        ///        // If the play button is clicked and the preparation is done, play the video. 
        ///        if(videoPlayer.isPrepared)
        ///        {
        ///            videoPlayer.Play();
        ///        }
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        // Press Spacebar to prepare the video. 
        ///        if (Input.GetKeyDown("space"))
        ///        {
        ///            if (!videoPlayer.isPrepared)
        ///            {
        ///                videoPlayer.Prepare(); 
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.isPrepared" />
        public extern void Prepare();

        ///<summary>Returns whether the <see cref="VideoPlayer" /> has successfully prepared the content to be played.</summary>
        ///<remarks>A prepared VideoPlayer can play back the content instantly because preliminary parsing and buffering has been done.
        ///
        ///A VideoPlayer starts out as not prepared (<c>false</c>). To prepare the VideoPlayer, you need to use <see cref="VideoPlayer.Prepare" />. When preparation is done, the VideoPlayer emits the <see cref="VideoPlayer.prepareCompleted" /> event, which sets isPrepared to true. 
        ///
        ///The property goes back to false when you or the VideoPlayer calls <see cref="VideoPlayer.Stop" />.
        ///
        ///If there are preparation failures, this property might never be set to true. In this case, Unity sends an error description through the <see cref="VideoPlayer.errorReceived" /> event.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///using System.Collections;
        ///
        /// // In the Inspector of your GameObject, attach this script and a VideoPlayer component. 
        /// // Also, assign a VideoClip to your VideoPlayer component.  
        /// // Use this script to prepare a video.  
        ///
        ///public class IsPreparedExample : MonoBehaviour
        ///{
        ///    public IEnumerator Start()
        ///    {
        ///        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        ///        videoPlayer.Prepare();
        ///        // Loops until the video is ready.
        ///        // Then outputs the message to the console when the preparation is done. 
        ///        while (!videoPlayer.isPrepared)
        ///        {
        ///            yield return null;
        ///        }
        ///        Debug.Log("Preparation completed!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.Prepare" />
        public extern bool isPrepared
        {
            [NativeName("IsPrepared")]
            get;
        }


        ///<summary>Determines whether the <see cref="VideoPlayer" /> will wait for the first frame to be loaded into the texture before starting playback when <see cref="Video.VideoPlayer.playOnAwake" /> is on.</summary>
        ///<remarks>When set to <c>true</c>, drawing into the target will only start once the <see cref="VideoPlayer" /> preparation is done and the first frame is available in texture memory. Otherwise, the playback will start immediately even if frames are not ready, leading to the first few frames possibly being skipped.
        ///
        ///**Note:** Depending on how long the preparation takes and the underlying platform capabilities, catching up with current time after preparation completes may result in many consecutive skips.</remarks>
        public extern bool waitForFirstFrame { get; set; }

        ///<summary>Whether the content will start playing back as soon as the component awakes.</summary>
        ///<remarks>**Note:** This plays each time the component is enabled, not only once from Awake.</remarks>
        public extern bool playOnAwake { get; set; }

        ///<summary>Starts or resumes the playback of a video.</summary>
        ///<remarks>If the video isn't prepared, this method will prepare the video before it starts playback, but playback won't be instant. To make playback instant, use <see cref="VideoPlayer.Prepare" /> and wait for preparation to finish (when <see cref="VideoPlayer.prepareCompleted" /> fires), before you use this method.
        ///
        ///If you use <see cref="VideoPlayer.Prepare" /> and then play the video before preparation finishes, the VideoPlayer will finish preparation and then play the video.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class PlayExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Get the VideoPlayer attached to this GameObject.
        ///        // You need to attach a VideoPlayer component in the Inspector for this script to work. 
        ///        var videoPlayer = GetComponent<VideoPlayer>();
        ///
        ///        videoPlayer.Play();
        ///
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.isPlaying" />
        ///<seealso cref="VideoPlayer.Pause" />
        ///<seealso cref="VideoPlayer.started" />
        public extern void Play();

        ///<summary>Pauses the playback and leaves the current time intact.</summary>
        ///<remarks>Use this method to pause the playback of a video. This method sets <see cref="VideoPlayer.isPlaying" /> to false. To play again, use <see cref="VideoPlayer.Play" />. 
        ///
        ///If you pause the video when the VideoPlayer isn't prepared, this method triggers preparation and shows the first frame of the video. 
        ///
        ///If you seek through to a different point in the video and then call <c>Pause()</c> before the VideoPlayer finishes preparation, it triggers preparation and shows the frame that was the seek target. For example, if you set <see cref="VideoPlayer.time" /> to /10.0f/ and then call <c>Pause()</c>, it shows the frame at 10 seconds.</remarks>
        ///<example>
        ///  <code><![CDATA[ // In the Inspector of a GameObject, attach this script and a VideoPlayer component. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class PauseExample: MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer; 
        ///
        ///    void Start()
        ///    {
        ///        // Get the VideoPlayer component from the GameObject with this script attached. 
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        // Press the Spacebar to pause the video if it's playing. 
        ///        if (Input.GetKeyDown("space"))
        ///        {
        ///            // If the VideoPlayer is currently playing a video, pause the video. 
        ///            if (videoPlayer.isPlaying)
        ///            {
        ///                videoPlayer.Pause(); 
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.Play" />
        public extern void Pause();

        ///<summary>Stops the playback and sets the current time to 0.</summary>
        ///<remarks>This also destroys all internal resources such as textures or buffered content. After calling /Stop()/, <see cref="VideoPlayer.isPrepared" /> becomes <c>false</c>.</remarks>
        public extern void Stop();

        ///<summary>Returns whether the VideoPlayer is currently playing the content.</summary>
        ///<remarks>This variable returns false if the video is paused. If you call <see cref="VideoPlayer.Play" />, it might not always set isPlaying to true. The <see cref="VideoPlayer" /> must successfully prepare the content before it starts to play. To prepare the content before you use <see cref="VideoPlayer.Play" />, use <see cref="VideoPlayer.Prepare" />.</remarks>
        ///<example>
        ///  <code><![CDATA[ // In the Inspector of a GameObject, attach this script and a VideoPlayer component. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class IsPlayingExample: MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer; 
        ///
        ///    void Start()
        ///    {
        ///        // Get the VideoPlayer component from the GameObject with this script attached. 
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        // Press the Spacebar to pause the video if it's playing. 
        ///        if (Input.GetKeyDown("space"))
        ///        {
        ///            // If the VideoPlayer is currently playing a video, pause the video. 
        ///            if(videoPlayer.isPlaying)
        ///            {
        ///                videoPlayer.Pause(); 
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.Play" />
        ///<seealso cref="VideoPlayer.isPaused" />
        ///<seealso cref="VideoPlayer.Pause" />
        public extern bool isPlaying
        {
            [NativeName("IsPlaying")]
            get;
        }
        ///<summary>Whether playback is paused. (RO)</summary>
        public extern bool isPaused
        {
            [NativeName("IsPaused")]
            get;
        }

        ///<summary>Whether you can change the current time using the <see cref="VideoPlayer.time" /> or <see cref="VideoPlayer.frame" /> properties. (RO)</summary>
        ///<remarks>Seeking is not supported in all contexts. For example, seeking in a HTTP live stream.
        ///
        ///This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canSetTime
        {
            [NativeName("CanSetTime")]
            get;
        }

        ///<summary>The presentation time of the currently available frame in <see cref="VideoPlayer.texture" /> in seconds.</summary>
        ///<remarks>Use <c>VideoPlayer.time</c> to achieve the following actions: 
        ///
        ///* Start the video at a certain time.
        ///* Search through a video.
        ///* Synchronize a part of your clip with another element- for example, with sounds, visual effects or events.
        ///
        ///When you set <c>VideoPlayer.time</c>, it initiates a seek operation. For example, if you set <c>VideoPlayer.time = 10 </c>, the VideoPlayer:
        ///
        ///1. Starts to move the video towards the 10 second mark.
        ///2. Fires the <see cref="VideoPlayer.seekCompleted" /> event when it reaches 10 seconds.
        ///3. Prepares the frame at this time for display.
        ///4. Triggers <see cref="VideoPlayer.frameReady" /> when the frame is prepared and displays the frame.
        ///
        ///The time value only properly settles when the VideoPlayer displays the frame.
        ///
        ///If you set time to another value during this operation, the VideoPlayer creates a new seek operation and adds it to a queue. The new operation will start when the previous one completes.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine; 
        ///using UnityEngine.Video; 
        ///
        ///public class TimeExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    private void Start()
        ///    {
        ///        // Get the VideoPlayer component from the GameObject that contains this script.  
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Skip to 10 seconds into the video. 
        ///        videoPlayer.time = 10.0f;
        ///        // Play the video. 
        ///        videoPlayer.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.Play" />
        ///<seealso cref="VideoPlayer.texture" />
        [NativeName("SecPosition")]
        public extern double time { get; set; }

        ///<summary>The frame index of the currently available frame in <see cref="VideoPlayer.texture" />.</summary>
        ///<remarks>The frame index is 0 for the first frame of the clip, 1 for the second frame, and so on. A frame index of -1 indicates that no valid frame is available.
        ///
        ///**Note:** On WebGL, because the frame rate is not known, the frame index assumes a rate of 24FPS. See <see cref="VideoPlayer.frameRate" />.</remarks>
        [NativeName("FramePosition")]
        public extern long frame { get; set; }

        ///<summary>The clock time that the <see cref="VideoPlayer" /> follows to schedule its samples. The clock time is expressed in seconds. (RO)</summary>
        public extern double clockTime { get; }

        ///<summary>Returns <c>true</c> if the <see cref="VideoPlayer" /> can step forward through the video content. (RO)</summary>
        ///<remarks>Stepping is done with <see cref="VideoPlayer.StepForward" />.
        ///
        ///This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canStep
        {
            [NativeName("CanStep")]
            get;
        }

        ///<summary>Immediately advance the current time by one frame.</summary>
        ///<remarks>If the video is currently playing, this method will pause the video before it advances to the next frame. However, if the VideoPlayer isn't prepared, this method will trigger preparation and display the first frame, but will not skip to the next frame. It steps forward from non-initialized to frame 0. 
        ///
        ///This method is useful if you want to: 
        ///
        ///* Analyze each frame of a video.
        ///* Debug issues related to the video or elements that play at certain frames.
        ///* Take finer control over the playback speed, because you can choose exactly when the next frame will appear. However, the WebGL implementation is unable to provide frame-accurate control due to platform limitations.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Video;
        ///using System.Collections;
        ///
        /// // In the Inspector of your GameObject, attach this script and a VideoPlayer component. 
        /// // Also, assign a VideoClip to your VideoPlayer component.  
        /// // Use this script to cycle through a video frame by frame. 
        ///
        ///public class StepForwardExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///
        ///    public void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        videoPlayer.Pause();
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        if (Input.GetKeyDown("space") && videoPlayer.isPrepared)
        ///        {
        ///            Debug.Log("Space key pressed."); 
        ///            // Go forward one frame in the video when you press the Spacebar. 
        ///            videoPlayer.StepForward(); 
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void StepForward();

        ///<summary>Whether you can change the playback speed. (RO)</summary>
        ///<remarks>This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canSetPlaybackSpeed
        {
            [NativeName("CanSetPlaybackSpeed")]
            get;
        }

        ///<summary>Factor by which the basic playback rate will be multiplied.</summary>
        ///<remarks>Only works if <see cref="Video.VideoPlayer.canSetPlaybackSpeed" /> is <c>true</c>. Setting this to 2.0 will play the clip twice as fast as its normal speed. Support for negative values is platform dependent.</remarks>
        public extern float playbackSpeed { get; set; }

        ///<summary>Determines whether the <see cref="VideoPlayer" /> restarts from the beginning when it reaches the end of the clip.</summary>
        ///<remarks>If set to <c>false</c>, the <see cref="VideoPlayer" /> will stop playback at the end of the clip and will not reset the time to the start of the clip.</remarks>
        [NativeName("Loop")]
        public extern bool isLooping { get; set; }

        ///<summary>[DEPRECATED] Whether you can change the time source followed by the <see cref="VideoPlayer" />. (RO)</summary>
        ///<remarks>Certain playback engines can only follow their own internal clock.
        ///
        ///This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        [System.Obsolete("VideoPlayer.canSetTimeSource is deprecated. Use canSetTimeUpdateMode instead. (UnityUpgradable) -> canSetTimeUpdateMode")]
        public extern bool canSetTimeSource
        {
            [NativeName("CanSetTimeSource")]
            get;
        }

        ///<summary>[DEPRECATED] The source used by the <see cref="VideoPlayer" /> to derive its current time.</summary>
        [System.Obsolete("VideoPlayer.timeSource is deprecated. Use timeUpdateMode instead. (UnityUpgradable) -> timeUpdateMode")]
        public extern VideoTimeSource timeSource { get; set; }

        ///<summary>The clock that the <see cref="UnityEngine.Video.VideoPlayer" /> observes to detect and correct drift.</summary>
        public extern VideoTimeReference timeReference { get; set; }

        ///<summary>Reference time of the external clock the <see cref="UnityEngine.Video.VideoPlayer" /> uses to correct its drift.</summary>
        ///<remarks>Only relevant when <see cref="VideoPlayer.timeReference" /> is set to <see cref="Video.VideoTimeReference.ExternalTime" />.</remarks>
        public extern double externalReferenceTime { get; set; }

        ///<summary>Whether frame-skipping to maintain synchronization can be controlled. (RO)</summary>
        ///<remarks>When <c>true</c>, the value of <see cref="VideoPlayer.skipOnDrop" /> can be changed. When <c>false</c>, the value cannot be changed. This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canSetSkipOnDrop
        {
            [NativeName("CanSetSkipOnDrop")]
            get;
        }

        ///<summary>Whether the <see cref="VideoPlayer" /> is allowed to skip frames to catch up with current time.</summary>
        ///<remarks>Only settable if <see cref="VideoPlayer.canSetSkipOnDrop" /> is <c>true</c>.</remarks>
        public extern bool skipOnDrop { get; set; }

        ///<summary>Number of frames in the current video content. (RO)</summary>
        ///<remarks>This value may be adjusted as the frameCount changes during playback. The value is most accurate after the video completes a full playthrough. 
        ///
        ///For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.
        ///
        ///**Note:** On WebGL, because the frame rate is not known, the frame count assumes a rate of 24FPS. See <see cref="VideoPlayer.frameRate" />.</remarks>
        public extern ulong frameCount { get; }

        ///<summary>The frame rate of the clip or URL in frames/second. (RO)</summary>
        ///<remarks>For URL sources, this is only set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />. This property is most accurate after the video does a complete playthrough. 
        ///
        ///**Note:** On WebGL, the frame rate is always set to 24FPS because the underlying implementation, the javascript API for HTML5 &lt;video&gt;, does not expose frame rate information.</remarks>
        public extern float frameRate { get; }

        ///<summary>The length of the <see cref="VideoClip" />, or the URL, in seconds. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        [NativeName("Duration")]
        public extern double length
        {
            get;
        }

        ///<summary>The width of the images in the <see cref="VideoClip" />, or URL, in pixels. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        public extern uint width { get; }

        ///<summary>The height of the images in the <see cref="VideoClip" />, or URL, in pixels. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        public extern uint height { get; }

        ///<summary>Numerator of the pixel aspect ratio (num:den) for the <see cref="VideoClip" /> or the URL. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        public extern uint pixelAspectRatioNumerator { get; }

        ///<summary>Denominator of the pixel aspect ratio (num:den) for the <see cref="VideoClip" /> or the URL. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        public extern uint pixelAspectRatioDenominator { get; }

        ///<summary>Number of audio tracks found in the data source currently configured. (RO)</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        public extern ushort audioTrackCount { get; }

        ///<summary>Returns the language code, if any, for the specified track.</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        ///<param name="trackIndex">Index of the audio track to query.</param>
        ///<returns>Language code.</returns>
        public extern string GetAudioLanguageCode(ushort trackIndex);

        ///<summary>The number of audio channels in the specified audio track.</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        ///<param name="trackIndex">Index for the audio track being queried.</param>
        ///<returns>Number of audio channels.</returns>
        public extern ushort GetAudioChannelCount(ushort trackIndex);

        ///<summary>Gets the audio track sampling rate in Hertz.</summary>
        ///<remarks>For URL sources, this will only be set once the source preparation is completed.  See <see cref="Video.VideoPlayer.Prepare" />.</remarks>
        ///<param name="trackIndex">Index of the audio track to query.</param>
        ///<returns>The sampling rate in Hertz.</returns>
        public extern uint GetAudioSampleRate(ushort trackIndex);

        ///<summary>Maximum number of audio tracks that can be controlled. (RO)</summary>
        ///<remarks>When playing audio from a URL, the number of audio tracks is not known in advance.  It is up to the user to specify the number of controlled audio tracks through <see cref="Video.VideoPlayer.controlledAudioTrackCount" />.  Other tracks will be ignored and silenced.  In this scenario, <see cref="Video.VideoPlayer.audioTrackCount" /> will be set to the actual number of tracks during playback, after preparation is complete.</remarks>
        ///<seealso cref="Video.VideoPlayer.Prepare" />
        public static extern ushort controlledAudioTrackMaxCount { get; }

        ///<summary>Number of audio tracks that this <see cref="VideoPlayer" /> will take control of.</summary>
        ///<remarks>The other audio tracks will be silent. The maximum allowed number of tracks is defined by <see cref="VideoPlayer.controlledAudioTrackMaxCount" />.
        ///
        ///The actual number of audio tracks cannot be known in advance when playing URLs, which is why this value is independent of the <see cref="Video.VideoPlayer.audioTrackCount" /> property.</remarks>
        public ushort controlledAudioTrackCount
        {
            get
            {
                return GetControlledAudioTrackCount();
            }

            set
            {
                int maxNumTracks = controlledAudioTrackMaxCount;
                if (value > maxNumTracks)
                    throw new ArgumentException(string.Format("Cannot control more than {0} tracks.", maxNumTracks), "value");

                SetControlledAudioTrackCount(value);
            }
        }

        private extern ushort GetControlledAudioTrackCount();

        private extern void SetControlledAudioTrackCount(ushort value);

        ///<summary>Enable/disable audio track decoding. Only effective when the <see cref="VideoPlayer" /> is not currently playing.</summary>
        ///<remarks>Disabling audio tracks helps save processing resources by disabling audio decoding completely. This is different to muting a track during playback, which turns the audio track volume to 0 but still decodes the audio samples.</remarks>
        ///<param name="trackIndex">Index of the audio track to enable/disable.</param>
        ///<param name="enabled">True for enabling the track. <c>False</c> for disabling the track.</param>
        public extern void EnableAudioTrack(ushort trackIndex, bool enabled);

        ///<summary>Whether decoding for the specified audio track is enabled. See <see cref="Video.VideoPlayer.EnableAudioTrack" /> for distinction with mute.</summary>
        ///<param name="trackIndex">Index of the audio track being queried.</param>
        ///<returns>Returns <c>true</c> if decoding for the specified audio track is enabled.</returns>
        public extern bool IsAudioTrackEnabled(ushort trackIndex);

        ///<summary>Destination for the audio embedded in the video.</summary>
        ///<remarks>**Note:** WebGL only fully supports <see cref="VideoAudioOutputMode.None" /> and <see cref="VideoAudioOutputMode.Direct" />. If you set the output mode to <see cref="VideoAudioOutputMode.AudioSource" />, Unity ignores all AudioSource fields except mute. This is because 3D spatialization of video playback is not available on the web.</remarks>
        public extern VideoAudioOutputMode audioOutputMode { get; set; }

        ///<summary>Whether direct-output volume controls are supported for the current platform and video format. (RO)</summary>
        ///<remarks>This value is only valid after the movie has been prepared. See <see cref="VideoPlayer.Prepare" />.</remarks>
        public extern bool canSetDirectAudioVolume
        {
            [NativeName("CanSetDirectAudioVolume")]
            get;
        }

        ///<summary>Return the direct-output volume for specified track.</summary>
        ///<param name="trackIndex">Track index for which the volume is queried.</param>
        ///<returns>Volume, between 0 and 1.</returns>
        public extern float GetDirectAudioVolume(ushort trackIndex);

        ///<summary>Set the direct-output audio volume for the specified track.</summary>
        ///<param name="trackIndex">Track index for which the volume is set.</param>
        ///<param name="volume">New volume, between 0 and 1.</param>
        public extern void SetDirectAudioVolume(ushort trackIndex, float volume);

        ///<summary>Gets the direct-output audio mute status for the specified track.</summary>
        public extern bool GetDirectAudioMute(ushort trackIndex);

        ///<summary>Set the direct-output audio mute status for the specified track.</summary>
        ///<param name="trackIndex">Track index for which the mute is set.</param>
        ///<param name="mute">Mute on/off.</param>
        public extern void SetDirectAudioMute(ushort trackIndex, bool mute);

        ///<summary>Gets the <see cref="AudioSource" /> that will receive audio samples for the specified track if <see cref="UnityEngine.Video.VideoPlayer.audioOutputMode" /> is set to <see cref="Video.VideoAudioOutputMode.AudioSource" />.</summary>
        ///<param name="trackIndex">Index of the audio track for which the <see cref="AudioSource" /> is wanted.</param>
        ///<returns>The source associated with the audio track.</returns>
        [NativeHeader("Modules/Audio/Public/AudioSource.h")]
        public extern AudioSource GetTargetAudioSource(ushort trackIndex);

        ///<summary>Sets the <see cref="AudioSource" /> that will receive audio samples for the specified track if this audio target is selected with <see cref="UnityEngine.Video.VideoPlayer.audioOutputMode" />.</summary>
        ///<param name="trackIndex">Index of the audio track to associate with the specified <see cref="AudioSource" />.</param>
        ///<param name="source">
        ///  <see cref="AudioSource" /> to associate with the audio track.</param>
        public extern void SetTargetAudioSource(ushort trackIndex, AudioSource source);

        ///<summary>Delegate type for all events without parameters emitted by <see cref="VideoPlayer" />s.</summary>
        ///<remarks>Use this EventHandler to define what you want to happen when the <see cref="VideoPlayer" /> emits certain events. This EventHandler accepts the following VideoPlayer events: 
        ///
        ///* <see cref="VideoPlayer.loopPointReached" />
        ///* <see cref="VideoPlayer.started" />
        ///* <see cref="VideoPlayer.prepareCompleted" />
        ///* <see cref="VideoPlayer.seekCompleted" /> 
        ///
        ///This EventHandler doesn't accept events that have parameters.</remarks>
        ///<param name="source">The <see cref="VideoPlayer" /> that emits the event.</param>
        ///<example>
        ///  <code><![CDATA[ // This script sets up a generic EventHandler to process a few different parameter-less events from the VideoPlayer. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class EventHandlerExample : MonoBehaviour
        ///{
        ///    public VideoPlayer videoPlayer;
        ///
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///
        ///        // Define a generic EventHandler for events that have the same signature.
        ///        VideoPlayer.EventHandler eventHandler = OnVideoPlayerEvent;
        ///
        ///        // Assign the generic EventHandler to manage multiple VideoPlayer events. 
        ///        videoPlayer.prepareCompleted += eventHandler;
        ///        videoPlayer.started += eventHandler;
        ///        videoPlayer.loopPointReached += eventHandler;
        ///        videoPlayer.seekCompleted += eventHandler;
        ///
        ///        videoPlayer.Prepare();
        ///    }
        ///
        ///    // All those events will invoke this same function and be handled the same way. 
        ///    private void OnVideoPlayerEvent(VideoPlayer vp)
        ///    {
        ///        Debug.Log("An event occurred on VideoPlayer.");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.ErrorEventHandler" />
        ///<seealso cref="VideoPlayer.errorReceived" />
        ///<seealso cref="VideoPlayer.FrameReadyEventHandler" />
        ///<seealso cref="VideoPlayer.frameReady" />
        ///<seealso cref="VideoPlayer.TimeEventHandler" />
        public delegate void EventHandler(VideoPlayer source);
        ///<summary>Delegate type for <see cref="VideoPlayer" /> events that contain an error message.</summary>
        ///<param name="source">The <see cref="VideoPlayer" /> that is emitting the event.</param>
        ///<param name="message">Message describing the error just encountered.</param>
        public delegate void ErrorEventHandler(VideoPlayer source, string message);
        ///<summary>Delegate type for <see cref="VideoPlayer" /> events that carry a frame number.</summary>
        ///<param name="source">The <see cref="VideoPlayer" /> that is emitting the event.</param>
        ///<param name="frameIdx">The current frame of the <see cref="VideoPlayer" />.</param>
        public delegate void FrameReadyEventHandler(VideoPlayer source, long frameIdx);
        ///<summary>Delegate type for <see cref="VideoPlayer" /> events that carry a time position.</summary>
        ///<param name="source">The <see cref="VideoPlayer" /> that is emitting the event.</param>
        ///<param name="seconds">Time position.</param>
        public delegate void TimeEventHandler(VideoPlayer source, double seconds);

        ///<summary>The <see cref="VideoPlayer" /> invokes this event when the video is ready for playback.</summary>
        ///<remarks>The <see cref="VideoPlayer" /> uses <see cref="VideoPlayer.Prepare" /> to ready a video for playback. When the preparation finishes, the <see cref="VideoPlayer" /> invokes this callback. If you start playback after this callback is invoked, frames become available instantly.
        ///If you call <see cref="VideoPlayer.Play" /> without using <see cref="VideoPlayer.Prepare" /> first, the <see cref="VideoPlayer" /> invokes <c>Prepare()</c> anyway. If preparation succeeds, the <see cref="VideoPlayer" /> still invokes this event.</remarks>
        ///<example>
        ///  <code><![CDATA[ // In this script, you can only interact with the button to play the video after the VideoPlayer 
        /// // finishes its preparation of the video. To start the preparation process, press Spacebar in Play mode. 
        ///
        /// // Attach this script to the GameObject you want to play a video clip on. 
        /// // Attach a VideoPlayer component with a video clip and assign a Button in the Inspector.
        ///
        ///using UnityEngine;
        ///using UnityEngine.UI;
        ///using UnityEngine.Video;
        ///
        ///public class PrepareExample: MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///    public Button playButton;
        ///
        ///    private void Awake()
        ///    {
        ///        // Get the VideoPlayer component from GameObject with this script attached.  
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        // Attach the event handler, which triggers when the VideoPlayer finishes its preparation. 
        ///        videoPlayer.prepareCompleted += OnPrepareCompleted;
        ///        videoPlayer.playOnAwake = false;
        ///        playButton.interactable = false;
        ///    }
        ///
        ///    // Event handler for when VideoPlayer finishes the preparation process. 
        ///    void OnPrepareCompleted(VideoPlayer vp)
        ///    {
        ///        Debug.Log("Preparation complete. You can now play the video.");
        ///        
        ///        // Preparation is complete so allow interactions with the play button. 
        ///        playButton.interactable = true;
        ///        playButton.onClick.AddListener(OnPlayButtonClicked);
        ///    }
        ///
        ///    void OnPlayButtonClicked()
        ///    {
        ///        // If the play button is clicked and the preparation is done, play the video. 
        ///        if (videoPlayer.isPrepared)
        ///        {
        ///            videoPlayer.Play();
        ///        }
        ///    }
        ///
        ///    private void Update()
        ///    {
        ///        // Press Spacebar to prepare the video. 
        ///        if (Input.GetKeyDown("space"))
        ///        {
        ///            if (!videoPlayer.isPrepared)
        ///            {
        ///                videoPlayer.Prepare(); 
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.Prepare" />
        ///<seealso cref="Video.VideoPlayer.EventHandler" />
        public event EventHandler prepareCompleted;
        ///<summary>The VideoPlayer emits this event when the video reaches the end of its playback.</summary>
        ///<remarks>If you set the <see cref="VideoPlayer.isLooping" /> property to <c>true</c>, this event makes the video play again. Otherwise the <see cref="VideoPlayer" /> stops. You can also set the **Loop** property in the Inspector window of the <see cref="VideoPlayer" /> component.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script plays a Particle System when the video finishes, and then loops the video. 
        /// // Attach this script and a VideoPlayer component to a GameObject. Also attach a ParticleSystem in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class LoopPointReachedExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///    public ParticleSystem particles; 
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        particles.playOnAwake = false; 
        ///
        ///        // When the video playback is done, restart the video. 
        ///        videoPlayer.isLooping = true;
        ///
        ///        // Each time the video reaches the end, call this function. 
        ///        videoPlayer.loopPointReached += OnLoopPointReached;
        ///
        ///        videoPlayer.Play();
        ///    }
        ///
        ///    void OnLoopPointReached(VideoPlayer vp)
        ///    {
        ///        // Play the particle effect when the video reaches the end.  
        ///        Debug.Log("Loop finished, play particle effect.");
        ///        particles.Play();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.isLooping" />
        ///<seealso cref="VideoPlayer.started" />
        ///<seealso cref="Video.VideoPlayer.EventHandler" />
        public event EventHandler loopPointReached;
        ///<summary>The VideoPlayer emits this event when the video starts to play.</summary>
        ///<remarks>After the VideoPlayer prepares the video and plays it, it emits this event. This event is useful if you want to play sounds, visual effects, timers or similar effects when the video starts.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script plays some audio when the video starts. 
        /// // Make sure to assign a [[VideoPlayer]] and [[AudioSource]] to your GameObject in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class VideoStartExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///    public AudioSource audioSource;
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///
        ///        if (videoPlayer != null)
        ///        {
        ///            // Call these functions when the video is prepared and started. 
        ///            videoPlayer.prepareCompleted += OnPrepareCompleted;
        ///            videoPlayer.started += OnVideoStarted;
        ///            // Prepare the VideoPlayer. 
        ///            videoPlayer.Prepare();
        ///        }  
        ///    }
        ///
        ///    void OnPrepareCompleted(VideoPlayer vp)
        ///    {
        ///        Debug.Log("Preparation done.");
        ///        videoPlayer.Play();
        ///    }
        ///
        ///    void OnVideoStarted(VideoPlayer vp)
        ///    {
        ///        // Play an audio clip when the video starts.
        ///        Debug.Log("Video has started.");
        ///        if (audioSource != null)
        ///        {
        ///            audioSource.Play();
        ///        }
        ///        else Debug.Log("OnVideoStarted tried to play an AudioSource that doesn't exist.");
        ///    }
        ///}]]></code>
        ///</example>
        ///<seealso cref="VideoPlayer.loopPointReached" />
        ///<seealso cref="Video.VideoPlayer.EventHandler" />
        public event EventHandler started;
        ///<summary>Invoked when the video decoder does not produce a frame as per the time source during playback.</summary>
        public event EventHandler frameDropped;
        ///<summary>The VideoPlayer uses this callback to report various types of errors.</summary>
        ///<remarks>The types of errors the VideoPlayer reports include: 
        ///
        ///* HTTP connection problems. 
        ///* Issues finding the file. 
        ///* Unsupported file types. 
        ///* Permission issues.
        ///* Runtime issues.
        ///
        ///This is useful if you want to log errors and debug so that it’s easier to diagnose issues. You can also use it to implement fallback solutions, for example, you can display an error message or try to play an alternative video.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Video;
        ///
        ///public class ErrorReceivedExample : MonoBehaviour
        ///{
        ///    public VideoPlayer videoPlayer;
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///        if (videoPlayer != null)
        ///        {
        ///            // When the VideoPlayer detects an error, call this OnErrorReceived function. 
        ///            videoPlayer.errorReceived += OnErrorReceived;
        ///            videoPlayer.Play();
        ///        }
        ///    }
        ///
        ///    void OnErrorReceived(VideoPlayer vp, string message)
        ///    {
        ///        Debug.LogError("Error received: " + message);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Video.VideoPlayer.ErrorEventHandler" />
        public event ErrorEventHandler errorReceived;
        ///<summary>Invoke after a seek operation completes.</summary>
        ///<remarks>Seek operations are done by changing the time or timeFrames property. Seek duration may be noticeably long depending on the codec performance and the parameters chosen at encoding time.</remarks>
        public event EventHandler seekCompleted;
        ///<summary>Invoked when the <see cref="VideoPlayer" /> clock is synced back to its <see cref="Video.VideoTimeReference" />.</summary>
        ///<remarks>The new <see cref="VideoPlayer" /> time is provided in the <see cref="Video.VideoPlayer.TimeEventHandler" />.</remarks>
        public event TimeEventHandler clockResyncOccurred;

        ///<summary>Enables the frameReady events.</summary>
        ///<remarks>If set to <c>true</c>, any delegates registered with <see cref="Video.VideoPlayer.frameReady" /> will be invoked when a frame is ready to be drawn. If set to <c>false</c>, the registered delegates will not be invoked.</remarks>
        public extern bool sendFrameReadyEvents
        {
            [NativeName("AreFrameReadyEventsEnabled")]
            get;
            [NativeName("EnableFrameReadyEvents")]
            set;
        }

        ///<summary>The VideoPlayer invokes this event when a new frame is ready to be displayed.</summary>
        ///<remarks>Use this event to: 
        ///
        ///* Analyze certain frames of a video.
        ///* Track progress of the video. 
        ///* Play other effects such as animations or sound effects at a certain frame. 
        ///
        ///To enable this event so that the VideoPlayer emits it, set the <see cref="VideoPlayer.sendFrameReadyEvents" /> property to <c>true</c>. This event is likely to tax the CPU, so set <see cref="VideoPlayer.sendFrameReadyEvents" /> back to <c>false</c> when you don’t need it. 
        ///
        ///The VideoPlayer also emits this event if you call <see cref="VideoPlayer.Pause" /> on a VideoPlayer that's not prepared yet or isn’t currently playing. When you call <c>Pause()</c> on a VideoPlayer that's not prepared or playing, it behaves as if you called <c>Play()</c> and then immediately called <c>Pause()</c>. This allows you to seek to a certain point in the video, and pause to give it time to prepare the frame before you play it.</remarks>
        ///<example>
        ///  <code><![CDATA[ // This script plays some audio when the VideoPlayer reaches the frame ( /targetFrame/ ) you set.  
        /// // Make sure to assign a VideoPlayer component to your GameObject and assign an AudioSource in the Inspector. 
        ///
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///using UnityEngine.Video;
        ///
        ///public class FrameReadyExample : MonoBehaviour
        ///{
        ///    VideoPlayer videoPlayer;
        ///    public AudioSource audioSource;
        ///
        ///    // The frame you want to play the sound at (set this value in the Inspector). 
        ///    public int targetFrame;
        ///
        ///    void Start()
        ///    {
        ///        videoPlayer = GetComponent<VideoPlayer>();
        ///
        ///        if (videoPlayer != null)
        ///        {
        ///            // Prepare the VideoPlayer to play the video. 
        ///            videoPlayer.prepareCompleted += OnPrepareCompleted;
        ///            videoPlayer.Prepare();
        ///        }
        ///        else
        ///            Debug.LogWarning("Your GameObject doesn't have a VideoPlayer component.");
        ///    }
        ///
        ///    void OnPrepareCompleted(VideoPlayer vp)
        ///    {
        ///        // Clamp targetFrame to be within the frame count of the video. 
        ///        var totalFrames = videoPlayer.frameCount; 
        ///        targetFrame = Mathf.Clamp(targetFrame, 0, (int)totalFrames - 1);
        ///
        ///        videoPlayer.sendFrameReadyEvents = true;
        ///        // When frameReady event is invoked, call this function. 
        ///        videoPlayer.frameReady += OnFrameReady;
        ///
        ///        videoPlayer.Play(); 
        ///    }
        ///
        ///    void OnFrameReady(VideoPlayer vp, long frameToPlay)
        ///    {
        ///        Debug.Log("Frame " + frameToPlay + " is ready.");
        ///
        ///        // Play the audio when the VideoPlayer video reaches the target frame.
        ///        if (frameToPlay == targetFrame)
        ///        {
        ///            if (audioSource != null)
        ///            {
        ///                audioSource.Play();
        ///            }
        ///            else
        ///                Debug.LogWarning("AudioSource component is missing."); 
        ///           
        ///            videoPlayer.sendFrameReadyEvents = false;
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        public event FrameReadyEventHandler frameReady;

        [RequiredByNativeCode]
        private static void InvokePrepareCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.prepareCompleted != null)
                source.prepareCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameReadyCallback_Internal(VideoPlayer source, long frameIdx)
        {
            if (source.frameReady != null)
                source.frameReady(source, frameIdx);
        }

        [RequiredByNativeCode]
        private static void InvokeLoopPointReachedCallback_Internal(VideoPlayer source)
        {
            if (source.loopPointReached != null)
                source.loopPointReached(source);
        }

        [RequiredByNativeCode]
        private static void InvokeStartedCallback_Internal(VideoPlayer source)
        {
            if (source.started != null)
                source.started(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameDroppedCallback_Internal(VideoPlayer source)
        {
            if (source.frameDropped != null)
                source.frameDropped(source);
        }

        [RequiredByNativeCode]
        private static void InvokeErrorReceivedCallback_Internal(VideoPlayer source, string errorStr)
        {
            if (source.errorReceived != null)
                source.errorReceived(source, errorStr);
        }

        [RequiredByNativeCode]
        private static void InvokeSeekCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.seekCompleted != null)
                source.seekCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeClockResyncOccurredCallback_Internal(VideoPlayer source, double seconds)
        {
            if (source.clockResyncOccurred != null)
                source.clockResyncOccurred(source, seconds);
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<string> analyticsSent;

        [RequiredByNativeCode]
        private static void InvokeAnalyticsSentCallback_Internal(string analytics)
        {
            if (analyticsSent != null)
                analyticsSent(analytics);
        }

        [RequiredByNativeCode]
        private static bool AnalyticsEventHandlerAttached_Internal()
        {
            return analyticsSent != null;
        }
    }
}
