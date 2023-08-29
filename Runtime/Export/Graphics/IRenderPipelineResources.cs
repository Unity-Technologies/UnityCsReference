// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Classes implementing this interface contain resources for SRP. 
    /// They appear in GraphicsSettings as other <see cref="IRenderPipelineGraphicsSettings"/>.
    /// Inside it, all fields with <see cref="ResourcePathAttribute"/> will be reloaded to the given resource if their value is null.
    /// </summary>
    public interface IRenderPipelineResources : IRenderPipelineGraphicsSettings
    { }

    /// <summary> Where to search the resource. </summary>
    public enum SearchType
    {
        /// <summary> Used for resources inside the project (e.g.: in packages) </summary>
        ProjectPath,

        /// <summary> Used for builtin resources </summary>
        BuiltinPath,

        /// <summary> Used for builtin extra resources </summary>
        BuiltinExtraPath,

        /// <summary> Used for shader that should be found by their name </summary>
        ShaderName,
    }
    
    /// <summary>
    /// Abstract attribute specifying information about the path where this resources are located.
    /// This is only used in the editor and doesn't have any effect at runtime.
    /// To use it, Create a child class implementing it or use <see cref="ResourcePathAttribute"/>, <see cref="ResourcePathsAttribute"/> or <see cref="ResourceFormattedPathsAttribute"/>.
    /// See <see cref="IRenderPipelineResources"/> for usage.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public abstract class ResourcePathsBaseAttribute : Attribute
    {
        /// <summary> The lookup method. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
        public SearchType location { get; private set; }
        /// <summary> Search paths. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
        public string[] paths { get; private set; }
        /// <summary> Disambiguish array of 1 element and fields. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
        public bool isField { get; private set; }

        protected ResourcePathsBaseAttribute(string[] paths, bool isField, SearchType location)
        {
            this.paths = paths;
            this.isField = isField;
            this.location = location;
        }
    }
    
    /// <summary>
    /// Attribute specifying information about the path where this resource is located.
    /// This is only used in the editor and doesn't have any effect at runtime.
    /// See <see cref="IRenderPipelineResources"/> for usage.
    /// </summary>
    public sealed class ResourcePathAttribute : ResourcePathsBaseAttribute
    {
        /// <summary>
        /// Creates a new <see cref="ResourcePathAttribute"/> for a single resource.
        /// </summary>
        /// <param name="path">Path targetting the resource</param>
        public ResourcePathAttribute(string path, SearchType location = SearchType.ProjectPath)
            : base(
                  new[] { path }
                  , true, location)
        { }
    }
    
    /// <summary>
    /// Attribute specifying information about the paths where these resources are located.
    /// This is only used in the editor and doesn't have any effect at runtime.
    /// See <see cref="IRenderPipelineResources"/> for usage.
    /// </summary>
    public sealed class ResourcePathsAttribute : ResourcePathsBaseAttribute
    {
        /// <summary>
        /// Creates a new <see cref="ResourcePathsAttribute"/> for an array's elements by specifying each resource. Defaults to Project resource path location.
        /// </summary>
        /// <param name="paths">Paths targetting the resources</param>
        public ResourcePathsAttribute(string[] paths, SearchType location = SearchType.ProjectPath)
            : base(paths, false, location)
        { }
    }

    /// <summary>
    /// Attribute specifying information about the paths where these resources are located.
    /// This is only used in the editor and doesn't have any effect at runtime.
    /// See <see cref="IRenderPipelineResources"/> for usage.
    /// </summary>
    public sealed class ResourceFormattedPathsAttribute : ResourcePathsBaseAttribute
    {
        /// <summary>
        /// Creates a new <see cref="ResourceFormattedPathsAttribute"/> for an array's elements using formatted path names.
        /// </summary>
        /// <param name="pathFormat">The format used for the path</param>
        /// <param name="rangeMin">The array start index (inclusive)</param>
        /// <param name="rangeMax">The array end index (exclusive)</param>
        public ResourceFormattedPathsAttribute(string pathFormat, int rangeMin, int rangeMax, SearchType location = SearchType.ProjectPath)
            : base(
                  CreateFormattedPaths(pathFormat, rangeMin, rangeMax)
                  , false, location)
        { }

        static string[] CreateFormattedPaths(string format, int rangeMin, int rangeMax)
        {
            var paths = new string[rangeMax - rangeMin];
            for (int index = rangeMin, i = 0; index < rangeMax; ++index, ++i) {
                paths[i] = string.Format(format, index);
            }
            return paths;
        }
    }
}
