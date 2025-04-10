// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Overlays
{
    sealed class OverlayPreset : ScriptableObject, IOverlayPreset, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        string m_RawWindowType;

        [SerializeField, HideInInspector]
        SaveData[] m_SaveData;

        Type m_TargetType;

        public Type targetWindowType
        {
            get => m_TargetType;
            set => m_TargetType = value;
        }

        public SaveData[] saveData
        {
            get => m_SaveData;
            set => m_SaveData = value;
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
        }

        public void OnBeforeSerialize()
        {
            m_RawWindowType = targetWindowType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            targetWindowType = Type.GetType(m_RawWindowType);
        }

        public void ApplyCustomData(OverlayCanvas canvas) {}

        public bool CanApplyToWindow(Type windowType)
        {
            return targetWindowType != null && targetWindowType.IsAssignableFrom(windowType);
        }
    }
}
