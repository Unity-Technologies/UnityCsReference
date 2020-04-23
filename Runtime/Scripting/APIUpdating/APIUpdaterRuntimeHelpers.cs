// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine._Scripting.APIUpdating // Funky name because bindings gen is tripping noodles with 'UnityEngine._Scripting.APIUpdating'
{
    //----------------------------------------------------------------------------------------------------------------------
    // What is this: Collection of helper functions that deal will handling changes to scripts, like type rename or
    //                  relocation to other assemblies.
    // Motivation  : Since we support renamig types and moving them arround, the serialization layer sometimes needs
    //                  to track how types have moves/changed so that it can adjust accordingly.
    //----------------------------------------------------------------------------------------------------------------------
    internal class APIUpdaterRuntimeHelpers
    {
        [RequiredByNativeCode]
        internal static bool GetMovedFromAttributeDataForType(Type sourceType, out string assembly, out string nsp, out string klass)
        {
            klass = null;
            nsp = null;
            assembly = null;

            var attrs = sourceType.GetCustomAttributes(typeof(MovedFromAttribute), false);

            if (attrs.Length != 1)
                return false;
            var attr = (MovedFromAttribute)attrs[0];
            klass = attr.data.className;
            nsp = attr.data.nameSpace;
            assembly = attr.data.assembly;

            return true;
        }

        // Notes:
        // -Expected format of obsolete string: comments (UnityUpgradable) -> [assembly] namespace.typeName/typeName
        // - where:
        //     - [Assembly] can occur [0,1] times
        //     - namespace can occur [0,n] times
        //     - typeName can occur [1,n] times
        // - Nested types (class inside a class) with have list of types separated by '/' stored in the className argument.
        [RequiredByNativeCode]
        internal static bool GetObsoleteTypeRedirection(Type sourceType, out string assemblyName, out string nsp, out string className)
        {
            var attrs = sourceType.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            assemblyName = null;
            nsp = null;
            className = null;

            if (attrs.Length != 1)
                return false;

            var attr = (ObsoleteAttribute)attrs[0];
            string x = attr.Message;
            if (string.IsNullOrEmpty(x))
                return false;

            string marker = "(UnityUpgradable) -> ";
            int index = x.IndexOf(marker);
            if (index >= 0)
            {
                var upgradeMsg = x.Substring(index + marker.Length).Trim();
                if (upgradeMsg.Length == 0)
                    return false;

                // format:  [UnityEngine] UnityEngine.BuildCompression/Uncompressed
                int skip = 0;

                // Extract assembly
                if (upgradeMsg[0] == '[')
                {
                    skip = upgradeMsg.IndexOf(']');
                    if (skip == -1)
                        return false;
                    assemblyName = upgradeMsg.Substring(1, skip - 1);
                    upgradeMsg = upgradeMsg.Substring(skip + 1).Trim();
                }
                else
                    assemblyName = sourceType.Assembly.GetName().Name;

                // Extract class name (with nested/parent class)
                skip = upgradeMsg.LastIndexOf('.');
                if (skip > -1)
                {
                    className = upgradeMsg.Substring(skip + 1);
                    upgradeMsg = upgradeMsg.Substring(0, skip);
                }
                else
                {
                    className = upgradeMsg;
                    upgradeMsg = "";
                }

                // extract namespace
                if (upgradeMsg.Length > 0)
                    nsp = upgradeMsg;
                else
                {
                    nsp = sourceType.Namespace;
                }

                return true;
            }
            else
                return false;
        }
    }
}
