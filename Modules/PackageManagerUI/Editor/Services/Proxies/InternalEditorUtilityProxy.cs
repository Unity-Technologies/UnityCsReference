// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IInternalEditorUtilityProxy : IService
    {
        string userAppDataFolder { get; }

        string GetFullUnityVersion();
        Version GetUnityVersion();
        int GetUnityVersionDate();
        Texture2D GetIconForFile(string fileName);
        bool OpenFileAtLineExternal(string fileName, int line, int column);
        void SaveToSerializedFileAndForget(Object[] objects, string path, bool allowTextSerialization);
        Object[] LoadSerializedFileAndForget(string path);
    }

    [ExcludeFromCodeCoverage]
    internal class InternalEditorUtilityProxy : BaseService<IInternalEditorUtilityProxy>, IInternalEditorUtilityProxy
    {
        public string userAppDataFolder => InternalEditorUtility.userAppDataFolder;

        public string GetFullUnityVersion()
        {
            return InternalEditorUtility.GetFullUnityVersion();
        }

        public Version GetUnityVersion()
        {
            return InternalEditorUtility.GetUnityVersion();
        }

        public int GetUnityVersionDate()
        {
            return InternalEditorUtility.GetUnityVersionDate();
        }

        public Texture2D GetIconForFile(string fileName)
        {
            return InternalEditorUtility.GetIconForFile(fileName);
        }

        public bool OpenFileAtLineExternal(string fileName, int line, int column)
        {
            return InternalEditorUtility.OpenFileAtLineExternal(fileName, line, column);
        }

        public void SaveToSerializedFileAndForget(Object[] objects, string path, bool allowTextSerialization)
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(objects, path, allowTextSerialization);
        }

        public Object[] LoadSerializedFileAndForget(string path)
        {
            return InternalEditorUtility.LoadSerializedFileAndForget(path);
        }
    }
}
