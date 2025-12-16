// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.UIToolkit.Editor;

[Flags]
internal enum VisualElementEditFlags
{
    None = 0,
    Attributes = 1, // Mode used when setting attribute overrides.
    Styles = 2,
    FullyEditable = Attributes | Styles
}
