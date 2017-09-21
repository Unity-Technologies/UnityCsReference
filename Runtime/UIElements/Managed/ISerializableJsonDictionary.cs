// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.Experimental.UIElements
{
    internal interface ISerializableJsonDictionary
    {
        void Set<T>(string key, T value) where T : class;

        T Get<T>(string key) where T : class;

        T GetScriptable<T>(string key) where T : ScriptableObject;

        void Overwrite(object obj, string key);

        bool ContainsKey(string key);

        void OnBeforeSerialize();

        void OnAfterDeserialize();
    }
}
