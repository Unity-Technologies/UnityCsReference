// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/CustomRenderTextureManager.h")]
    public static class CustomRenderTextureManager
    {
        public static event Action<CustomRenderTexture> textureLoaded;

        [RequiredByNativeCode]
        private static void InvokeOnTextureLoaded_Internal(CustomRenderTexture source)
            => textureLoaded?.Invoke(source);

        public static event Action<CustomRenderTexture> textureUnloaded;

        [RequiredByNativeCode]
        private static void InvokeOnTextureUnloaded_Internal(CustomRenderTexture source)
            => textureUnloaded?.Invoke(source);

        [FreeFunction(Name = "CustomRenderTextureManagerScripting::GetAllCustomRenderTextures", HasExplicitThis = false)]
        public extern static void GetAllCustomRenderTextures(List<CustomRenderTexture> currentCustomRenderTextures);

        public static event Action<CustomRenderTexture, int> updateTriggered;

        internal static void InvokeTriggerUpdate(CustomRenderTexture crt, int updateCount) => updateTriggered?.Invoke(crt, updateCount);

        public static event Action<CustomRenderTexture> initializeTriggered;

        internal static void InvokeTriggerInitialize(CustomRenderTexture crt) => initializeTriggered?.Invoke(crt);
    }
}
