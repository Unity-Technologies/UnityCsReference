// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface ITransform
    {
        Vector3 position { get; set; }
        Quaternion rotation { get; set; }
        Vector3 scale { get; set; }
        Matrix4x4 matrix { get; }
    }
}
