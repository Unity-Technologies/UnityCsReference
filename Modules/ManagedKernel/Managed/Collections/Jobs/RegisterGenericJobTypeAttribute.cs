// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Jobs
{
    /// <summary>
    /// When added as an assembly-level attribute, allows creating job reflection data for instances of generic jobs.
    /// </summary>
    /// <remarks>
    /// This attribute allows specific instances of generic jobs to be registered for reflection data generation.
    /// </remarks>
    [MovedFrom(true, "Unity.Entities", "Unity.Entities")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterGenericJobTypeAttribute : Attribute
    {
        /// <summary>
        /// Fully closed generic job type to register with the job system
        /// </summary>
        public Type ConcreteType;

        /// <summary>
        /// Registers a fully closed generic job type with the job system
        /// </summary>
        /// <param name="type"></param>
        public RegisterGenericJobTypeAttribute(Type type)
        {
            ConcreteType = type;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    internal class DOTSCompilerGeneratedAttribute : Attribute
    {}
}
