// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    [NativeType(Header = "Modules/PresetsEditor/Public/PresetType.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct PresetType : IEquatable<PresetType>
    {
        int m_NativeTypeID;
        MonoScript m_ManagedTypePPtr;
        string m_ManagedTypeFallback;

        public PresetType(Object o)
        {
            m_NativeTypeID = 0;
            m_ManagedTypePPtr = null;
            m_ManagedTypeFallback = string.Empty;
            Init_Internal(o);
        }

        internal PresetType(SerializedProperty property)
        {
            m_NativeTypeID = property.FindPropertyRelative("m_NativeTypeID").intValue;
            m_ManagedTypePPtr = property.FindPropertyRelative("m_ManagedTypePPtr").objectReferenceValue as MonoScript;
            m_ManagedTypeFallback = property.FindPropertyRelative("m_ManagedTypeFallback").stringValue;
        }

        internal PresetType(int nativeTypeID)
        {
            m_NativeTypeID = nativeTypeID;
            m_ManagedTypePPtr = null;
            m_ManagedTypeFallback = string.Empty;
        }

        internal PresetType(Type type)
        {
            m_NativeTypeID = 0;
            m_ManagedTypePPtr = null;
            m_ManagedTypeFallback = string.Empty;
            InitFromType_Internal(type);
        }

        internal Texture2D GetIcon()
        {
            Texture2D icon = null;
            if (m_ManagedTypePPtr != null)
                icon = AssetPreview.GetMiniThumbnail(m_ManagedTypePPtr);
            if (icon == null)
                icon = AssetPreview.GetMiniTypeThumbnailFromClassID(m_NativeTypeID);

            return icon;
        }

        public override bool Equals(object obj)
        {
            return obj is PresetType && Equals((PresetType)obj);
        }

        [NativeName("GetHashCode")]
        private extern int GetHash();

        public override int GetHashCode()
        {
            return GetHash();
        }

        public static bool operator==(PresetType a, PresetType b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(PresetType a, PresetType b)
        {
            return !a.Equals(b);
        }

        public extern bool IsValid();
        public extern bool IsValidDefault();
        public extern string GetManagedTypeName();

        private extern void Init_Internal([NotNull] Object target);
        private extern void InitFromType_Internal([NotNull] Type type);

        public bool Equals(PresetType other)
        {
            return m_NativeTypeID == other.m_NativeTypeID &&
                m_ManagedTypePPtr == other.m_ManagedTypePPtr &&
                m_ManagedTypeFallback == other.m_ManagedTypeFallback;
        }
    }
}
