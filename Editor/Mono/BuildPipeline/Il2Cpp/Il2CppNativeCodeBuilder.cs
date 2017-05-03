// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UnityEditorInternal
{
    public abstract class Il2CppNativeCodeBuilder
    {
        /// <summary>
        /// Implement this property to tell IL2CPP about the platform for the C++ compiler.
        /// This platform must be known in the Unity.IL2CPP.Builder code.
        /// </summary>
        public abstract string CompilerPlatform { get; }

        /// <summary>
        /// Implement this property to tell IL2CPP about the architecture for the C++ compiler.
        /// This architecture must be known in the Unity.IL2CPP.Builder code.
        /// </summary>
        public abstract string CompilerArchitecture { get; }

        /// <summary>
        /// Provide any compiler flags IL2CPP should use in addition to the default ones.
        /// The default value of this property is an empty string.
        /// </summary>
        public virtual string CompilerFlags
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Provide any linker flags IL2CPP should use in addition to the default ones.
        /// The default value of this property is an empty string.
        /// </summary>
        public virtual string LinkerFlags
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// IL2CPP should not try to set up the environment for the C++ compiler internally. Instead, it will
        /// use the environment it is provided. If this is true, SetupEnvironment will be called.
        /// The default value of this property is false.
        /// </summary>
        public virtual bool SetsUpEnvironment
        {
            get { return false; }
        }

        /// <summary>
        /// Provide a cache directory for IL2CPP to use to save build artifacts used for incremental builds.
        /// If this does not exist, a full build will occur.
        /// The default value of this property is an empty string, which disables incremental builds.
        /// </summary>
        public virtual string CacheDirectory
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Provide the path to a plugin
        /// </summary>
        public virtual string PluginPath
        {
            get { return string.Empty; }
        }


        public virtual IEnumerable<string> AdditionalIl2CPPArguments
        {
            get { return new string[0]; }
        }

        /// <summary>
        /// If this property returns true the argument "--libil2cpp-static" will be used when calling il2cpp.exe

        /// </summary>
        public virtual bool LinkLibIl2CppStatically
        {
            get { return true; }
        }

        /// <summary>
        /// Change the relative include paths into absolute paths that can be passed to the C++ compiler.
        /// By default this method returns its input with each path relative to the current directory.
        /// </summary>
        /// <param name="relativeIncludePaths">The list of relative paths to convert</param>
        /// <returns>A list of full paths</returns>
        public virtual IEnumerable<string> ConvertIncludesToFullPaths(IEnumerable<string> relativeIncludePaths)
        {
            var workingDirectory = Directory.GetCurrentDirectory();
            return relativeIncludePaths.Select(path => Path.Combine(workingDirectory, path));
        }

        /// <summary>
        /// Change the relative path to the output file to an absolute path that can be passed to the C++ compiler.
        /// By default this method returns its input relative to the current directory.
        /// </summary>
        /// <param name="outputFileRelativePath">The relative output file path to convert</param>
        /// <returns>The full output file path</returns>
        public virtual string ConvertOutputFileToFullPath(string outputFileRelativePath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), outputFileRelativePath);
        }

        public void SetupStartInfo(ProcessStartInfo startInfo)
        {
            if (SetsUpEnvironment)
                SetupEnvironment(startInfo);
        }

        /// <summary>
        /// Override this method if SetsUpEnvironment is override to return true. This will allow
        /// the ProcessStartInfo for IL2CPP to be modified.
        /// </summary>
        /// <param name="startInfo">The ProcessStartInfo for IL2CPP</param>
        protected virtual void SetupEnvironment(ProcessStartInfo startInfo)
        {
        }
    }
}
