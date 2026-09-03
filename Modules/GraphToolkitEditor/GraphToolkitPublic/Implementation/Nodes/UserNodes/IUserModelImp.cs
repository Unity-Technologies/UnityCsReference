// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor.Implementation
{
    /// <summary>
    /// Shared contract for the internal models that back a user-defined node or state and forward the
    /// <see cref="Node.OnEnable"/>/<see cref="Node.OnDisable"/> (and <see cref="State"/> equivalents) lifecycle to them.
    /// </summary>
    interface IUserModelImp
    {
        bool IsMissingDefinition { get; }

        bool OnEnableCalled { get; set; }

        void CallOnEnable();

        void CallOnDisable();
    }
}
