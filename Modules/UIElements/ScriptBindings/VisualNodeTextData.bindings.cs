// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

[NativeType(Header = "Modules/UIElements/VisualNodeTextData.h")]
[StructLayout(LayoutKind.Sequential)]
struct VisualNodeTextData
{
    public LanguageDirection LanguageDirection;
    public LanguageDirection LocalLanguageDirection;
}
