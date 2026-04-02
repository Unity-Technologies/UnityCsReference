// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer
{
    /// <summary>
    /// Marks a MonoBehaviour as multiplayer role restricted so it cannot be
    /// stripped in Server or Client platforms.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class MultiplayerRoleRestrictedAttribute : Attribute
    {
    }
}
