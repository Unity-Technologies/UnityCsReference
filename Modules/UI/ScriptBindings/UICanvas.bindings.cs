// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    ///<summary>RenderMode for the Canvas.</summary>
    ///<example>
    ///  <code><![CDATA[
    /// //Attach this script to your Canvas GameObject
    ///
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    enum RenderModeStates { camera, overlay, world };
    ///    RenderModeStates m_RenderModeStates;
    ///
    ///    Canvas m_Canvas;
    ///
    ///    // Use this for initialization
    ///    void Start()
    ///    {
    ///        m_Canvas = GetComponent<Canvas>();
    ///    }
    ///
    ///    // Update is called once per frame
    ///    void Update()
    ///    {
    ///        //Press the space key to switch between render mode states
    ///        if (Input.GetKeyDown(KeyCode.Space))
    ///        {
    ///            ChangeState();
    ///        }
    ///    }
    ///
    ///    void ChangeState()
    ///    {
    ///        switch (m_RenderModeStates)
    ///        {
    ///            case RenderModeStates.camera:
    ///                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
    ///                m_RenderModeStates = RenderModeStates.overlay;
    ///                break;
    ///
    ///            case RenderModeStates.overlay:
    ///                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    ///                m_RenderModeStates = RenderModeStates.world;
    ///                break;
    ///            case RenderModeStates.world:
    ///                m_Canvas.renderMode = RenderMode.WorldSpace;
    ///                m_RenderModeStates = RenderModeStates.camera;
    ///
    ///                break;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public enum RenderMode
    {
        ///<summary>Render at the end of the Scene using a 2D Canvas.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to your Canvas GameObject
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    enum RenderModeStates { camera, overlay, world };
        ///    RenderModeStates m_RenderModeStates;
        ///
        ///    Canvas m_Canvas;
        ///
        ///    // Use this for initialization
        ///    void Start()
        ///    {
        ///        m_Canvas = GetComponent<Canvas>();
        ///    }
        ///
        ///    // Update is called once per frame
        ///    void Update()
        ///    {
        ///        //Press the space key to switch between render mode states
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            ChangeState();
        ///        }
        ///    }
        ///
        ///    void ChangeState()
        ///    {
        ///        switch (m_RenderModeStates)
        ///        {
        ///            case RenderModeStates.camera:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        ///                m_RenderModeStates = RenderModeStates.overlay;
        ///                break;
        ///
        ///            case RenderModeStates.overlay:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ///                m_RenderModeStates = RenderModeStates.world;
        ///                break;
        ///            case RenderModeStates.world:
        ///                m_Canvas.renderMode = RenderMode.WorldSpace;
        ///                m_RenderModeStates = RenderModeStates.camera;
        ///
        ///                break;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<summary>Render at the end of the Scene using a 2D Canvas.</summary>
        ScreenSpaceOverlay = 0,
        ///<summary>Render using the <see cref="Camera" /> configured on the Canvas.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to your Canvas GameObject
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    enum RenderModeStates { camera, overlay, world };
        ///    RenderModeStates m_RenderModeStates;
        ///
        ///    Canvas m_Canvas;
        ///
        ///    // Use this for initialization
        ///    void Start()
        ///    {
        ///        m_Canvas = GetComponent<Canvas>();
        ///    }
        ///
        ///    // Update is called once per frame
        ///    void Update()
        ///    {
        ///        //Press the space key to switch between render mode states
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            ChangeState();
        ///        }
        ///    }
        ///
        ///    void ChangeState()
        ///    {
        ///        switch (m_RenderModeStates)
        ///        {
        ///            case RenderModeStates.camera:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        ///                m_RenderModeStates = RenderModeStates.overlay;
        ///                break;
        ///
        ///            case RenderModeStates.overlay:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ///                m_RenderModeStates = RenderModeStates.world;
        ///                break;
        ///            case RenderModeStates.world:
        ///                m_Canvas.renderMode = RenderMode.WorldSpace;
        ///                m_RenderModeStates = RenderModeStates.camera;
        ///
        ///                break;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ScreenSpaceCamera = 1,
        ///<summary>Render using any <see cref="Camera" /> in the Scene that can render the layer.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to your Canvas GameObject
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    enum RenderModeStates { camera, overlay, world };
        ///    RenderModeStates m_RenderModeStates;
        ///
        ///    Canvas m_Canvas;
        ///
        ///    // Use this for initialization
        ///    void Start()
        ///    {
        ///        m_Canvas = GetComponent<Canvas>();
        ///    }
        ///
        ///    // Update is called once per frame
        ///    void Update()
        ///    {
        ///        //Press the space key to switch between render mode states
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            ChangeState();
        ///        }
        ///    }
        ///
        ///    void ChangeState()
        ///    {
        ///        switch (m_RenderModeStates)
        ///        {
        ///            case RenderModeStates.camera:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        ///                m_RenderModeStates = RenderModeStates.overlay;
        ///                break;
        ///
        ///            case RenderModeStates.overlay:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ///                m_RenderModeStates = RenderModeStates.world;
        ///                break;
        ///            case RenderModeStates.world:
        ///                m_Canvas.renderMode = RenderMode.WorldSpace;
        ///                m_RenderModeStates = RenderModeStates.camera;
        ///
        ///                break;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        WorldSpace = 2
    }

    ///<summary>Enum used to determine if a Canvas should be resized when a manual Camera.Render call is performed.</summary>
    public enum StandaloneRenderResize
    {
        ///<summary>Resize the Canvas when a manual Camera.Render call is performed.</summary>
        Enabled = 0,
        ///<summary>Do not resize the Canvas when a manual Camera.Render call is performed.</summary>
        Disabled = 1
    }

    ///<summary>Enum mask of possible shader channel properties that can also be included when the <see cref="Canvas" /> mesh is created.</summary>
    [Flags]
    public enum AdditionalCanvasShaderChannels
    {
        ///<summary>No additional shader parameters are needed.</summary>
        None = 0,
        ///<summary>Include UV1 on the mesh vertices.</summary>
        TexCoord1 = 1 << 0,
        ///<summary>Include UV2 on the mesh vertices.</summary>
        TexCoord2 = 1 << 1,
        ///<summary>Include UV3 on the mesh vertices.</summary>
        TexCoord3 = 1 << 2,
        ///<summary>Include the normals on the mesh vertices.</summary>
        ///<remarks>This channel isn't likely needed unless you are expecting lighting to be applied to the <see cref="Canvas" />. Reminder that a Overlay <see cref="Canvas" /> can not have lighting applied.</remarks>
        Normal = 1 << 3,
        ///<summary>Include the Tangent on the mesh vertices.</summary>
        ///<remarks>This channel isn't likely needed unless you are expecting lighting to be applied to the <see cref="Canvas" />. Reminder that a Overlay <see cref="Canvas" /> can not have lighting applied.</remarks>
        Tangent = 1 << 4
    }

    ///<summary>Element that can be used for screen rendering.</summary>
    ///<remarks>Elements on a canvas are rendered AFTER Scene rendering, either from an attached camera or using overlay mode.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections;
    ///using System.Collections.Generic;
    ///using UnityEditor;
    ///using UnityEngine;
    ///using UnityEngine.UI;
    ///
    /// // Create a Canvas that holds a Text GameObject.
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        GameObject myGO;
    ///        GameObject myText;
    ///        Canvas myCanvas;
    ///        Text text;
    ///        RectTransform rectTransform;
    ///
    ///        // Canvas
    ///        myGO = new GameObject();
    ///        myGO.name = "TestCanvas";
    ///        myGO.AddComponent<Canvas>();
    ///
    ///        myCanvas = myGO.GetComponent<Canvas>();
    ///        myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    ///        myGO.AddComponent<CanvasScaler>();
    ///        myGO.AddComponent<GraphicRaycaster>();
    ///
    ///        // Text
    ///        myText = new GameObject();
    ///        myText.transform.parent = myGO.transform;
    ///        myText.name = "wibble";
    ///
    ///        text = myText.AddComponent<Text>();
    ///        text.font = (Font)Resources.Load("MyFont");
    ///        text.text = "wobble";
    ///        text.fontSize = 100;
    ///
    ///        // Text position
    ///        rectTransform = text.GetComponent<RectTransform>();
    ///        rectTransform.localPosition = new Vector3(0, 0, 0);
    ///        rectTransform.sizeDelta = new Vector2(400, 200);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [RequireComponent(typeof(RectTransform)),
     NativeClass("UI::Canvas", PersistentTypeId = 223),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/CanvasManager.h"),
     NativeHeader("Modules/UI/UIStructs.h")]
    [UIModuleHelpURL("class-Canvas")]
    public sealed partial class Canvas : Behaviour
    {
        internal Canvas(global::UnityEngine.EntityId id) : base(id) {}
        public Canvas() {}
        // Controls gating of PlayerUpdateCanvases by OnDemandRendering in Player.cpp
        // In sync with CanvasManager::s_BatchingInterval
        ///<summary>Options for when Unity updates canvases for batching.</summary>
        ///<remarks>Use this enum with the <see cref="Canvas.batchingInterval" /> property to choose the batching update behavior. This property applies only at runtime in Player builds and has no effect in the Editor.</remarks>
        public enum BatchingInterval
        {
            ///<summary>Updates canvases for batching only when rendering is required.</summary>
            ///<remarks>Use this default option to update canvas batching only when Unity performs rendering. For example, when you use <see cref="UnityEngine.Rendering.OnDemandRendering" />, batching updates run only on rendered frames, which can improve performance. This applies only at runtime in Player builds and has no effect in the Editor.</remarks>
            GatedByRendering = 0,
            ///<summary>Updates canvases for batching on every update, even when rendering is not required.</summary>
            ///<remarks>Use this option to update canvas batching independently from rendering. For example, when you use <see cref="UnityEngine.Rendering.OnDemandRendering" />, batching updates run on every frame, even when rendering is skipped. This applies only at runtime in Player builds and has no effect in the Editor.</remarks>
            AlwaysUpdate = 1
        }

        ///<summary>Specifies whether Unity updates canvases for batching only when rendering is performed, such as with <see cref="UnityEngine.Rendering.OnDemandRendering" />, or every update at runtime in Player builds.</summary>
        ///<remarks>This property applies only at runtime in Player builds and has no effect in the Editor. The default value is <see cref="Canvas.BatchingInterval.GatedByRendering" />.</remarks>
        public static BatchingInterval batchingInterval
        {
            get => (BatchingInterval)Internal_GetBatchingInterval();
            set
            {
                int intValue = (int)value;

                if (!Enum.IsDefined(typeof(BatchingInterval), intValue))
                {
                    intValue = 0; // default fallback to GatedByRendering
                    Debug.LogWarning($"Invalid value for Canvas.batchingInterval: {value}. Defaulting to BatchingInterval.GatedByRendering.");
                }

                Internal_SetBatchingInterval(intValue);
            }
        }

        [FreeFunction("UI::CanvasManager::SetBatchingInterval")]
        internal static extern void Internal_SetBatchingInterval(int value);

        [FreeFunction("UI::CanvasManager::GetBatchingInterval")]
        internal static extern int Internal_GetBatchingInterval();

        ///<exclude />
        public delegate void WillRenderCanvases();
        ///<summary>Event that is called just before <see cref="Canvas" /> rendering happens.</summary>
        ///<remarks>This allows you to delay processing / updating of canvas based elements until just before they are rendered.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event WillRenderCanvases preWillRenderCanvases;
        ///<summary>Event that is called before <see cref="Canvas" /> is rendered.</summary>
        ///<remarks>Use this event to delay the processing or updating of canvas-based elements until they are about to be rendered.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event WillRenderCanvases willRenderCanvases;

        ///<summary>Is the <see cref="Canvas" /> in World or Overlay mode?</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to your Canvas GameObject
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    enum RenderModeStates { camera, overlay, world };
        ///    RenderModeStates m_RenderModeStates;
        ///
        ///    Canvas m_Canvas;
        ///
        ///    // Use this for initialization
        ///    void Start()
        ///    {
        ///        m_Canvas = GetComponent<Canvas>();
        ///    }
        ///
        ///    // Update is called once per frame
        ///    void Update()
        ///    {
        ///        //Press the space key to switch between render mode states
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            ChangeState();
        ///        }
        ///    }
        ///
        ///    void ChangeState()
        ///    {
        ///        switch (m_RenderModeStates)
        ///        {
        ///            case RenderModeStates.camera:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        ///                m_RenderModeStates = RenderModeStates.overlay;
        ///                break;
        ///
        ///            case RenderModeStates.overlay:
        ///                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ///                m_RenderModeStates = RenderModeStates.world;
        ///                break;
        ///            case RenderModeStates.world:
        ///                m_Canvas.renderMode = RenderMode.WorldSpace;
        ///                m_RenderModeStates = RenderModeStates.camera;
        ///
        ///                break;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern RenderMode renderMode { get; set; }
        ///<summary>Is this the root <see cref="Canvas" />?</summary>
        public extern bool isRootCanvas { get; }
        ///<summary>Get the render rect for the <see cref="Canvas" />.</summary>
        ///<remarks>If in overlay mode this will be the screen dimensions. If in world mode this will be the camera screen viewport rect.</remarks>
        public extern Rect pixelRect { get; }
        ///<summary>Scales the entire canvas, ensuring it fits the screen. It only applies when <see cref="Canvas.renderMode" /> is set to Screen Space.</summary>
        public extern float scaleFactor { get; set; }
        ///<summary>The number of pixels per unit that is considered the default.</summary>
        ///<remarks>Sprites have a Pixels Per Unit that control the pixel density of the sprite. For sprites that have the same Pixels Per Unit as the Reference Pixels Per Unit in the Canvas, the pixel density will be one to one.</remarks>
        public extern float referencePixelsPerUnit { get; set; }
        ///<summary>Allows for nested canvases to override pixelPerfect settings inherited from parent canvases.</summary>
        public extern bool overridePixelPerfect { get; set; }
        ///<summary>Should the Canvas vertex color always be in gamma space before passing to the UI shaders in linear color space work flow.</summary>
        ///<remarks>Keeping the Canvas vertex color in gamma space will allow the gamma to linear conversion to happen in UI shaders, where colors have higher precision.
        ///                This enhances UI color precision in linear color space workflow, especially for darker colors.
        ///                Buit-in UI shaders include gamma to linear conversion. However, in custom UI shaders, user needs to provide gamma to linear conversion.</remarks>
        public extern bool vertexColorAlwaysGammaSpace { get; set; }
        ///<summary>Enables reflections on the Canvas when <see cref="Canvas.renderMode" /> is set to World Space.</summary>
        ///<remarks>When enabled, the Canvas is registered with the Reflection Probe system. The system selects the most appropriate reflection probe based on the Canvas's world position and binds it to all shaders on the Canvas.
        ///               All shaders on the Canvas can then access the reflection probe cube map and the normal vector to calculate reflections accurately.</remarks>
        public extern bool useReflectionProbes { get; set; }
        ///<summary>Forces pixel alignment for elements in the canvas. It only applies when <see cref="Canvas.renderMode" /> is set to Screen Space.</summary>
        ///<remarks>Enabling pixelPerfect can make elements appear sharper and prevent blurriness. However, if many elements are scaled or rotated, or use subtle animated position or scaling, it may be advantageous to disable pixelPerfect, since the movement will be smoother without.</remarks>
        public extern bool pixelPerfect { get; set; }
        ///<summary>How far away from the camera is the Canvas generated? It only applies when <see cref="Canvas.renderMode" /> is set to <see cref="RenderMode.ScreenSpaceCamera" />.</summary>
        public extern float planeDistance { get; set; }
        ///<summary>The render order in which the canvas is being emitted to the Scene. (RO)</summary>
        ///<remarks>**Note:** Currently only <c>Screen Space - Overlay</c> canvases are ordered correctly as <c>Screen Space - Camera</c> and <c>World Space</c> are emitted and sorted based upon distance from the camera.</remarks>
        public extern int renderOrder { get; }
        ///<summary>Allows for nested canvases to override the <see cref="Canvas.sortingOrder" /> from parent canvases.</summary>
        ///<remarks>If set, nested canvases can ignore the parent draw order, and either draw on top of or below the parent draw order.</remarks>
        public extern bool overrideSorting  { get; set; }
        ///<summary>Canvas' order within a sorting layer.</summary>
        ///<remarks>When comparing canvases in the same sorting layer, the one with a higher sorting order is displayed above the one with a lower sorting order.
        ///**Note**: Internally the value is stored as a signed 16-bit integer (short) and is constrained within the range of <c>-32,768</c> to <c>32,767</c>.
        ///See <see cref="Renderer" />.</remarks>
        public extern int sortingOrder  { get; set; }
        ///<summary>For Overlay mode, display index on which the UI canvas will appear.</summary>
        ///<remarks>This setting makes a Canvas render into the specified display.
        ///Maximum number of secondary displays (eg. monitors) supported is 8.</remarks>
        ///<seealso cref="Display" />
        ///<seealso cref="Camera.targetDisplay" />
        public extern int targetDisplay  { get; set; }
        ///<summary>Unique ID of the Canvas' sorting layer.</summary>
        ///<remarks>See <see cref="Renderer" />.</remarks>
        public extern int sortingLayerID { get; set; }
        ///<summary>Cached calculated value based upon SortingLayerID.</summary>
        public extern int cachedSortingLayerValue { get; }
        ///<summary>Get or set the mask of additional shader channels to be used when creating the <see cref="Canvas" /> mesh.</summary>
        ///<remarks>The <see cref="Canvas" /> will always include Position, Color, and Uv0 shader channels when generating the mesh for a overlay <see cref="Canvas" /> and will also include Normal and Tangent for ScreenSpace.Camera and World space <see cref="Canvas" />. These are the optional additional parameters to be copied.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class SetCanvasShaderChannels : MonoBehaviour
        ///{
        ///    public Canvas canvas;
        ///
        ///    void Start()
        ///    {
        ///        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
        ///        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        ///        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }
        ///<summary>Name of the Canvas' sorting layer.</summary>
        ///<remarks>See <see cref="Renderer" />.</remarks>
        public extern string sortingLayerName { get; set; }
        ///<summary>Returns the <see cref="Canvas" /> closest to root, by checking through each parent and returning the last canvas found. If no other canvas is found then the canvas will return itself.</summary>
        ///<seealso cref="isRootCanvas" />
        public extern Canvas rootCanvas { get; }

        ///<summary>Provides the pixel dimensions of the display area where the UI canvas is rendered.</summary>
        ///<remarks>This size is typically determined by the Game view or the current screen resolution and is affected by the canvas's render mode, scaling, and display settings.
        ///
        ///* **Screen Space - Overlay Canvases:** Corresponds directly to the screen resolution.
        ///* ** Screen Space - Camera Canvases:** Represents the screen area covered by the camera.
        ///* **World Space Canvases:** The <see cref="RectTransform" /> width and height.</remarks>
        public extern Vector2 renderingDisplaySize { get; }
        ///<summary>Should the Canvas size be updated based on the render target when a manual Camera.Render call is performed.</summary>
        public extern StandaloneRenderResize updateRectTransformForStandalone { get; set; }

        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int> externBeginRenderOverlays
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int, int> externRenderOverlaysBefore
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }
        [AutoStaticsCleanupOnCodeReload]
        internal static Action<int> externEndRenderOverlays
        {
            get;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        [FreeFunction("UI::CanvasManager::SetExternalCanvasEnabled")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetExternalCanvasEnabled(bool enabled);

        ///<summary>
        ///  <see cref="Camera" /> used for sizing the <see cref="Canvas" /> when in Screen Space - Camera. Also used as the <see cref="Camera" /> that events will be sent through for a World Space <see cref="Canvas" />.</summary>
        [NativeProperty("Camera", false, TargetType.Function)] public extern Camera worldCamera { get; set; }
        ///<summary>The normalized grid size that the canvas will split the renderable area into.</summary>
        ///<remarks>During rendering, the canvas splits the renderable area (bounds of all UI elements) into a grid. This is the normalized size of that grid. For example if you have a renderable area of 100 units with a sortingGridNormalizedSize of 0.1f then each grid cell would be 10 units.
        ///
        ///Note: a value of 0 will default to 0.1f.</remarks>
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern float normalizedSortingGridSize { get; set; }

        ///<summary>The normalized grid size that the canvas will split the renderable area into.</summary>
        ///<remarks>During rendering, the canvas splits the renderable area (bounds of all UI elements) into a grid. This is the normalized size of that grid. For example if you have a renderable area of 100 units with a sortingGridNormalizedSize of 0.1f then each grid cell would be 10 units.
        ///
        ///Note: a value of 0 will default to 0.1f.</remarks>
        [Obsolete("Setting normalizedSize via a int is not supported. Please use normalizedSortingGridSize", false)]
        [NativeProperty("SortingBucketNormalizedSize", false, TargetType.Function)] public extern int sortingGridNormalizedSize { get; set; }

        ///<summary>Returns the default material that can be used for rendering text elements on the Canvas.</summary>
        [Obsolete("Shared default material now used for text and general UI elements, call Canvas.GetDefaultCanvasMaterial()", false)]
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasTextMaterial();

        ///<summary>Returns the default material that can be used for rendering normal elements on the Canvas.</summary>
        [FreeFunction("UI::GetDefaultUIMaterial")] public static extern Material GetDefaultCanvasMaterial();
        ///<summary>Gets or generates the ETC1 Material.</summary>
        ///<remarks>Uses the UI/DefaultETC1 Shader which must be specified in the Always Included Shader list.</remarks>
        ///<returns>The generated ETC1 Material from the Canvas.</returns>
        [FreeFunction("UI::GetETC1SupportedCanvasMaterial")] public static extern Material GetETC1SupportedCanvasMaterial();

        internal extern void UpdateCanvasRectTransform(bool alignWithCamera);

        internal extern byte stagePriority { get; set; }

        ///<summary>Force all canvases to update their content.</summary>
        ///<remarks>A canvas performs its layout and content generation calculations at the end of a frame, just before rendering, in order to ensure that it's based on all the latest changes that may have happened during that frame. This means that in the Start callback and the first Update callback, the layout and content under the canvas may not be up-to-date.
        ///
        ///Code that relies on up-to-date layout or content can call this method to ensure it before executing code that relies on it.</remarks>
        public static void ForceUpdateCanvases()
        {
            SendPreWillRenderCanvases();
            SendWillRenderCanvases();
        }

        [RequiredByNativeCode]
        private static void SendPreWillRenderCanvases()
        {
            preWillRenderCanvases?.Invoke();
        }

        [RequiredByNativeCode]
        private static void SendWillRenderCanvases()
        {
            willRenderCanvases?.Invoke();
        }

        [RequiredByNativeCode]
        private static void BeginRenderExtraOverlays(int displayIndex)
        {
            externBeginRenderOverlays?.Invoke(displayIndex);
        }

        [RequiredByNativeCode]
        private static void RenderExtraOverlaysBefore(int displayIndex, int sortingOrder)
        {
            externRenderOverlaysBefore?.Invoke(displayIndex, sortingOrder);
        }

        [RequiredByNativeCode]
        private static void EndRenderExtraOverlays(int displayIndex)
        {
            externEndRenderOverlays?.Invoke(displayIndex);
        }
    }

    ///<exclude />
    [IgnoredByDeepProfiler]
    [NativeHeader("Modules/UI/Canvas.h"),
     StaticAccessor("UI::SystemProfilerApi", StaticAccessorType.DoubleColon)]
    public static class UISystemProfilerApi
    {
        ///<exclude />
        public enum SampleType
        {
            ///<exclude />
            Layout,
            ///<exclude />
            Render
        }

        ///<exclude />
        public static extern void BeginSample(SampleType type);
        ///<exclude />
        public static extern void EndSample(SampleType type);
        ///<exclude />
        public static extern void AddMarker(string name, Object obj);
    }
}
