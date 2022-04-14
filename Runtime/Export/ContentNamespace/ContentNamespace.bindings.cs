// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace Unity.Content
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Misc/ContentNamespace.h")]
    [StaticAccessor("GetContentNamespaceManager()", StaticAccessorType.Dot)]
    public struct ContentNamespace
    {
        internal UInt64 Id;

        public string GetName()
        {
            ThrowIfInvalidNamespace();
            return GetNamespaceName(this);
        }

        public bool IsValid { get { return IsNamespaceHandleValid(this); } }

        public void Delete()
        {
            if (Id == s_Default.Id)
                throw new InvalidOperationException("Cannot delete the default namespace.");
            else
            {
                ThrowIfInvalidNamespace();
                RemoveNamespace(this);
            }
        }

        void ThrowIfInvalidNamespace()
        {
            if (!IsValid)
                throw new InvalidOperationException("The provided namespace is invalid. Did you already delete it?");
        }

        static bool s_defaultInitialized = false;
        static ContentNamespace s_Default;
        public static ContentNamespace Default
        {
            get
            {
                if (!s_defaultInitialized)
                {
                    s_defaultInitialized = true;
                    s_Default = GetOrCreateNamespace("default");
                }
                return s_Default;
            }
        }

        static Regex s_ValidName = new Regex(@"^[a-zA-Z0-9]{1,16}$", RegexOptions.Compiled);
        public static ContentNamespace GetOrCreateNamespace(string name)
        {
            if (s_ValidName.IsMatch(name))
                return GetOrCreate(name);
            throw new InvalidOperationException("Namespace name can only contain alphanumeric characters and a maximum length of 16 characters.");
        }

        // Namespace Functions
        public static extern ContentNamespace[] GetAll();
        internal static extern ContentNamespace GetOrCreate(string name);

        internal static extern void RemoveNamespace(ContentNamespace ns);
        internal static extern string GetNamespaceName(ContentNamespace ns);
        internal static extern bool IsNamespaceHandleValid(ContentNamespace ns);
    }
}
