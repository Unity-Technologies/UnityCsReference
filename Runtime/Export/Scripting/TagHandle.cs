// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine;

[NativeHeader("Runtime/BaseClasses/TagManager.h")]
[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
public struct TagHandle
{
    private uint _tagIndex;

    public static TagHandle GetExistingTag(string tagName) => new() { _tagIndex = ExtractTagThrowing(tagName) };

    public override string ToString() => TagToString(_tagIndex);

    [NativeThrows]
    [FreeFunction]
    [NativeHeader("Runtime/Export/Scripting/GameObject.bindings.h")]
    private static extern uint ExtractTagThrowing(string tagName);

    private static extern string TagToString(uint tagIndex);
}
