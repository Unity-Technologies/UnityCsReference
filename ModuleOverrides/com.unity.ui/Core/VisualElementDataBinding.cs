// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines a <see cref="VisualElement"/> property accessible for data binding.
    /// </summary>
    internal readonly struct DataBindingProperty
    {
        private readonly PropertyPath m_PropertyPath;
        private readonly string m_Path;

        /// <summary>
        /// Instantiate a new data binding property.
        /// </summary>
        /// <param name="path">The path of the property.</param>
        public DataBindingProperty(string path)
        {
            m_PropertyPath = new PropertyPath(path);
            m_Path = path;
        }

        /// <summary>
        /// Instantiate a new data binding property.
        /// </summary>
        /// <param name="path">The path of the property.</param>
        public DataBindingProperty(PropertyPath path)
        {
            m_PropertyPath = path;
            m_Path = path.ToString();
        }

        /// <summary>
        /// Converts a <see cref="DataBindingProperty"/> to a <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="vep">The property.</param>
        /// <returns>A path for the property.</returns>
        public static implicit operator PropertyPath(DataBindingProperty vep)
        {
            return vep.m_PropertyPath;
        }

        /// <summary>
        /// Converts a <see cref="DataBindingProperty"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="vep">The property.</param>
        /// <returns>A path for the property.</returns>
        public static implicit operator string(DataBindingProperty vep)
        {
            return vep.m_Path;
        }

        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="DataBindingProperty"/>.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property.</returns>
        public static implicit operator DataBindingProperty(string name)
        {
            return new DataBindingProperty(name);
        }

        /// <summary>
        /// Converts a <see cref="PropertyPath"/> to a <see cref="DataBindingProperty"/>.
        /// </summary>
        /// <param name="path">The path to the property.</param>
        /// <returns>The property.</returns>
        public static implicit operator DataBindingProperty(PropertyPath path)
        {
            return new DataBindingProperty(path);
        }

        /// <summary>
        /// Returns the data binding path as a string.
        /// </summary>
        /// <returns>The property path.</returns>
        public override string ToString()
        {
            return m_Path;
        }
    }
}
