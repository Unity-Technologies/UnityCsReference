// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Scripting.APIUpdating
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MovedFromAttributeData
    {
        public void Set(bool autoUpdateAPI, string sourceNamespace = null, string sourceAssembly = null, string sourceClassName = null)
        {
            className = sourceClassName;
            classHasChanged = className != null;
            nameSpace = sourceNamespace;
            nameSpaceHasChanged = nameSpace != null;
            assembly = sourceAssembly;
            assemblyHasChanged = assembly != null;
            autoUdpateAPI = autoUpdateAPI;
        }

        public string className;
        public string nameSpace;
        public string assembly;
        public bool classHasChanged;
        public bool nameSpaceHasChanged;
        public bool assemblyHasChanged;
        public bool autoUdpateAPI;
    }

    //----------------------------------------------------------------------------------------------------------------------
    // What is this : Attribute that can be used to indicate that a type has been moved/renamed.
    // Motivation(s):
    //  - When a class is moved from one namespace to an other (potentialy in a different assembly), the APIUpdater needs
    //     a way to be informed of this.
    //  - Serialization by reference of plain C# classes needs to be informed of classes being renamed so that it can
    //      read data that was saved by an earlier version.
    //
    // Notes:
    //  - IMPORTANT: the APIUpdater does **NOT** support renaming a klass through this attribute. To do so, a custom
    //      configuration is required. Talk to the APIUPdater team for details.
    //----------------------------------------------------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class MovedFromAttribute : Attribute
    {
        // If automatic udpate of scripts is not required (accepted breaking change), then this attribute can be set to be ignored
        // by the APIUpdater tool (autoUpdateAPI == false), in which case any combination of changes to
        // [assembly, namespace, class name] are supported, but *ONLY* by the serialization system.
        // Note: any null string is interpreted as "has not changed" and it's actual value will be extracted from the decorated type.
        public MovedFromAttribute(bool autoUpdateAPI, string sourceNamespace = null, string sourceAssembly = null, string sourceClassName = null)
        {
            data.Set(autoUpdateAPI, sourceNamespace, sourceAssembly, sourceClassName);
        }

        public MovedFromAttribute(string sourceNamespace)
        {
            data.Set(true, sourceNamespace, null, null);
        }

        internal bool AffectsAPIUpdater
        {
            get { return !data.classHasChanged && !data.assemblyHasChanged; }
        }

        public bool IsInDifferentAssembly
        {
            get { return data.assemblyHasChanged; }
        }

        internal MovedFromAttributeData data;
    }
}
