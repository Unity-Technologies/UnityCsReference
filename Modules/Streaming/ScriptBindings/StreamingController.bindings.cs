// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEngine
{
    ///<summary>A StreamingController controls the streaming settings for an individual camera location.</summary>
    ///<remarks>The StreamingController component is used to control texture streaming settings for a camera location.
    ///This component supports the preloading of textures in advance of a <see cref="Camera" /> becoming enabled. See <see cref="SetPreloading" />
    ///
    ///The <see cref="QualitySettings.streamingMipmapsActive" /> must be enabled and active for this feature to work.
    ///
    ///The Camera is not considered for texture streaming when this component is disabled.
    ///When this component is enabled the Camera is considered for texture streaming if the Camera is enabled or the StreamingController is in the preloading state.
    ///
    ///A mipmap bias can be applied for texture streaming calculations. See <see cref="streamingMipmapBias" /> for details.</remarks>
    ///<seealso href="xref:class-Camera">camera component</seealso>
    [RequireComponent(typeof(Camera))]
    [NativeHeader("Modules/Streaming/StreamingController.h")]
    [NativeClass("StreamingController", PersistentTypeId = 0x5BF715FE)]
    public class StreamingController : Behaviour
    {
        internal StreamingController(global::UnityEngine.EntityId id) : base(id) {}
        public StreamingController() {}
        ///<summary>Offset applied to the mipmap level chosen by the texture streaming system for any textures visible from this camera. This Offset can take either a positive or negative value.</summary>
        ///<remarks>When texture streaming is active, Unity loads mipmap levels for textures based on their distance from all active cameras. This bias is added to all textures visible from this camera and allows you to force smaller or larger mipmap levels to be loaded for textures visible from this camera.</remarks>
        extern public float streamingMipmapBias { get; set; }

        ///<summary>Initiate preloading of streaming data for this camera.</summary>
        ///<remarks>Activate texture streaming at this camera location. This is for preloading texture mipmaps prior to a <see cref="Camera" /> being activated a short time later.
        ///When the Camera component on the same GameObject becomes enabled, preloading will be disabled.
        ///Preloading can be manually disabled with <see cref="StreamingController.CancelPreloading" />
        ///
        ///The function will do nothing if called when the associated Camera is already enabled.</remarks>
        ///<param name="timeoutSeconds">Optional timeout before stopping preloading. Set to 0.0f when no timeout is required.</param>
        ///<param name="activateCameraOnTimeout">Set to True to activate the connected <see cref="Camera" /> component when timeout expires.</param>
        ///<param name="disableCameraCuttingFrom">
        ///  <see cref="Camera" /> to deactivate on timeout (if <paramref name="activateCameraOnTimeout" /> is True). This parameter can be null.</param>
        extern public void SetPreloading(float timeoutSeconds = 0.0f, bool activateCameraOnTimeout = false, Camera disableCameraCuttingFrom = null);
        ///<summary>Abort preloading.</summary>
        ///<remarks>This cancels texture mipmap preloading for this camera location.</remarks>
        extern public void CancelPreloading();
        ///<summary>Used to find out whether the StreamingController is currently preloading texture mipmaps.</summary>
        ///<returns>True if in a preloading state, otherwise False.</returns>
        extern public bool IsPreloading();
    }
}
