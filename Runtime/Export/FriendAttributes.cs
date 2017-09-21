// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// PLEASE DO NOT ADD ANYTHING NEW HERE.
// Modules should not have dependencies to Core module internals.
// Ideally, try to make your module self-contained with it's internals, or make the dependencies public APIs.
// If that is not feasible, make a separate module with the dependencies, so we don't have to expose all Core internals to your module.

// needed for UnityEngine.Playables internal methods
[assembly: InternalsVisibleTo("UnityEngine.AudioModule")]
[assembly: InternalsVisibleTo("UnityEngine.AnimationModule")]
[assembly: InternalsVisibleTo("UnityEngine.VideoModule")]

// needed for Graphics.Internal_DrawTexture
[assembly: InternalsVisibleTo("UnityEngine.IMGUIModule")]

// needed for External/CSSLayout (this probably should not live in Core module!)
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]

// needed for UnityEngine.UnsafeUtility (this probably should not live in Core module!)
[assembly: InternalsVisibleTo("UnityEngine.InputModule")]
