// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets;

[NativeHeader("Modules/UIElements/Core/Native/StylePropertyNativeUtils.h")]
internal class StylePropertyNativeUtils
{
    public static extern int PropertyFieldOffset(int propertyId);
    public static extern int PropertyFieldSize(int propertyId);
}
