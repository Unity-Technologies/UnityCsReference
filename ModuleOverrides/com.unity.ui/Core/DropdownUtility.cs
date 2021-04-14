// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal static class DropdownUtility
    {
        static Func<IGenericMenu> s_MakeDropdownFunc;

        internal static IGenericMenu CreateDropdown()
        {
            return s_MakeDropdownFunc != null ? s_MakeDropdownFunc.Invoke() : new GenericDropdownMenu();
        }

        internal static void RegisterMakeDropdownFunc(Func<IGenericMenu> makeClient)
        {
            if (s_MakeDropdownFunc != null)
                throw new UnityException($"The MakeDropdownFunc has already been registered. Registration denied.");

            s_MakeDropdownFunc = makeClient;
        }
    }
}
