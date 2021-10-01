// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal static class DropdownUtility
    {
        internal static Func<IGenericMenu> MakeDropdownFunc;

        internal static IGenericMenu CreateDropdown()
        {
            return MakeDropdownFunc != null ? MakeDropdownFunc.Invoke() : new GenericDropdownMenu();
        }
    }
}
