//
// These types are shared with the Editor
//

using System.Collections.Generic;
using System.Diagnostics;

//linker or editor may not make use of all fields so suppress unused fields warn
#pragma warning disable CS0649

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class EditorToLinkerData
    {
        [UnityEngine.SerializeField]
        public TypeInSceneData[] typesInScenes;

        [UnityEngine.SerializeField]
        public NativeTypeData[] allNativeTypes;

        [UnityEngine.SerializeField]
        public string[] forceIncludeModules;

        [UnityEngine.SerializeField]
        public string[] forceExcludeModules;

        [System.Serializable]
        public class TypeInSceneData
        {
            [UnityEngine.SerializeField]
            public string managedAssemblyName;
            [UnityEngine.SerializeField]
            public string nativeClass;
            [UnityEngine.SerializeField]
            public string fullManagedTypeName;
            [UnityEngine.SerializeField]
            public string moduleName;
            [UnityEngine.SerializeField]
            public string[] usedInScenes;

            /// <summary>
            /// For deserializing
            /// </summary>
            public TypeInSceneData()
            {
            }

            public TypeInSceneData(string managedAssemblyName, string fullManagedTypeName, string nativeClass, string moduleName, string[] usedInScenes)
            {
                this.managedAssemblyName = managedAssemblyName;
                this.nativeClass = nativeClass;
                this.fullManagedTypeName = fullManagedTypeName;
                this.moduleName = moduleName;
                this.usedInScenes = usedInScenes;
            }

            /// <summary>
            /// Overridden for easier reading when debugging
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                var managedPart = string.IsNullOrEmpty(fullManagedTypeName) ? "None" : $"{managedAssemblyName}::{fullManagedTypeName}";
                var nativeName = string.IsNullOrEmpty(nativeClass) ? "None" : $"{moduleName}::{nativeClass}";
                return $"{managedPart} [{nativeName}]";
            }
        }

        [DebuggerDisplay("{module}:{name}")]
        [System.Serializable]
        public class NativeTypeData
        {
            [UnityEngine.SerializeField]
            public string name;
            [UnityEngine.SerializeField]
            public string module;

            [UnityEngine.SerializeField]
            public string baseName;
            [UnityEngine.SerializeField]
            public string baseModule;
        }
    }

    [System.Serializable]
    internal class LinkerToEditorData
    {
        [UnityEngine.SerializeField]
        public ReportData report;

        [System.Serializable]
        public class ReportData
        {
            [UnityEngine.SerializeField]
            public List<Module> modules;

            [System.Serializable]
            [DebuggerDisplay("{name}")]
            public class Module
            {
                [UnityEngine.SerializeField]
                public string name;
                [UnityEngine.SerializeField]
                public List<Dependency> dependencies;
            }

            [System.Serializable]
            [DebuggerDisplay("{name}")]
            public class Dependency
            {
                [UnityEngine.SerializeField]
                public string name;
                [UnityEngine.SerializeField]
                public string[] scenes;
                [UnityEngine.SerializeField]
                public DependencyType dependencyType;
                [UnityEngine.SerializeField]
                public string icon;
            }

            public enum DependencyType
            {
                ManagedType,
                NativeType,
                Module,
                Custom
            }
        }
    }
}
