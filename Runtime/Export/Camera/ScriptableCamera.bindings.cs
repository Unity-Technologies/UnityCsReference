// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /*
    * Class Concept: Provides a class Camera like that can be derived safely
    *
    * Motivation: We need to add specific data along the cameras in SRP. Having this in a separate class
    * will allows component to evolve more easily. (Same as for MonoBehaviour regarding Behaviour)
    *
    * Additional Info: Making the Camera constructor internal will prevent User to try inheriting from Camera.
    */
    [NativeHeader("Runtime/Camera/ScriptableCamera.h")]
    [UsedByNativeCode]
    [ExtensionOfNativeClass]
    [Preserve]
    public abstract class ScriptableCamera : Camera
    {
    }
}
