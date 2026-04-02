// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Timeline.Foundation.Model
{
    /// <summary>
    /// Contains a <cref>FieldDelegateLookup</cref> and can be inherited to customize how delegates are acquired from the FieldDelegateLookup
    /// </summary>
    /// <typeparam name="T">Type of objects that this monitor's delegates act on</typeparam>
    class SerializedObjectChangeMonitor<T>
    {
        public FieldDelegateLookup<T> lookup { get; } = new();

        public void RegisterDelegate<TDelegateKey>(FieldDelegate<T> del, FieldDelegateLookup<T>.Comparer customComparer = null) where TDelegateKey : Object
        {
            lookup.RegisterDelegate<TDelegateKey>(del, customComparer);
        }

        public void RegisterDelegate<TDelegateKey>(string field, FieldDelegate<T> del, FieldDelegateLookup<T>.Comparer customComparer = null) where TDelegateKey : Object
        {
            lookup.RegisterDelegate<TDelegateKey>(field, del, customComparer);
        }

        protected void RegisterDelegate<TDelegateKey>(List<string> fields, FieldDelegate<T> del, FieldDelegateLookup<T>.Comparer customComparer = null) where TDelegateKey : Object
        {
            lookup.RegisterDelegate<TDelegateKey>(fields, del, customComparer);
        }

        /// <summary>
        /// Finds the delegates to invoke given two SerializedObjects that will be compared
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="checkBaseTypes"></param>
        /// <returns>Collection of <c>Action&lt;IPlayer, UnityEngine.Object></c></returns>
        public virtual IEnumerable<FieldDelegate<T>> GetDelegates(SerializedObject lhs, SerializedObject rhs, bool checkBaseTypes = true)
        {
            foreach (FieldDelegate<T> del in lookup.GetDelegates(lhs, rhs, checkBaseTypes))
                yield return del;
        }
    }
}
