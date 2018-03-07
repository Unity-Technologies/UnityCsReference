// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEngine
{
    [RequireComponent(typeof(Camera))]
    [NativeHeader("Modules/Streaming/StreamingController.h")]
    public class StreamingController : Behaviour
    {
        extern public float streamingMipmapBias { get; set; }

        extern public void SetPreloading(float timeoutSeconds = 0.0f, bool activateCameraOnTimeout = false, Camera disableCameraCuttingFrom = null);
        extern public void CancelPreloading();
        extern public bool IsPreloading();
    }
}
