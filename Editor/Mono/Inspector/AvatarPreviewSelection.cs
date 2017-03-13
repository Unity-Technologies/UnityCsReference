// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AvatarPreviewSelection : ScriptableSingleton<AvatarPreviewSelection>
    {
        [SerializeField]
        GameObject[] m_PreviewModels;

        void Awake()
        {
            int length = (int)ModelImporterAnimationType.Human + 1;
            if (m_PreviewModels == null || m_PreviewModels.Length != length)
                m_PreviewModels = new GameObject[length];
        }

        static public void SetPreview(ModelImporterAnimationType type, GameObject go)
        {
            if (!System.Enum.IsDefined(typeof(ModelImporterAnimationType), type))
                return;

            if (instance.m_PreviewModels[(int)type] != go)
            {
                instance.m_PreviewModels[(int)type] = go;
                instance.Save(false);
            }
        }

        static public GameObject GetPreview(ModelImporterAnimationType type)
        {
            if (!System.Enum.IsDefined(typeof(ModelImporterAnimationType), type))
                return null;

            return instance.m_PreviewModels[(int)type];
        }
    } // class AvatarPreviewSelection
} // namespace UnityEditor
