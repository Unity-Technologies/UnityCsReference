// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Sprites
{
    public sealed partial class Packer
    {
        public enum Execution
        {
            Normal = 0,
            ForceRegroup
        }

        public static string kDefaultPolicy = typeof(DefaultPackerPolicy).Name;

        private static string[] m_policies = null;
        public static string[] Policies
        {
            get
            {
                RegenerateList();
                return m_policies;
            }
        }

        private static string m_selectedPolicy = null;
        private static void SetSelectedPolicy(string value)
        {
            m_selectedPolicy = value;
            PlayerSettings.spritePackerPolicy = m_selectedPolicy;
        }

        public static string SelectedPolicy
        {
            get
            {
                RegenerateList();
                return m_selectedPolicy;
            }
            set
            {
                RegenerateList();
                if (value == null)
                    throw new ArgumentNullException();
                if (!m_policies.Contains(value))
                    throw new ArgumentException("Specified policy {0} is not in the policy list.", value);
                SetSelectedPolicy(value);
            }
        }

        private static Dictionary<string, Type> m_policyTypeCache = null;
        private static void RegenerateList()
        {
            if (m_policies != null)
                return;

            List<Type> types = new List<Type>();

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    Type[] asst = assembly.GetTypes();
                    foreach (var t in asst)
                    {
                        if (typeof(IPackerPolicy).IsAssignableFrom(t) && (t != typeof(IPackerPolicy)))
                            types.Add(t);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(string.Format("SpritePacker failed to get types from {0}. Error: {1}", assembly.FullName, ex.Message));
                }
            }

            m_policies = types.Select(t => t.Name).ToArray();

            m_policyTypeCache = new Dictionary<string, Type>();
            foreach (var t in types)
            {
                if (m_policyTypeCache.ContainsKey(t.Name))
                {
                    Type otherT = m_policyTypeCache[t.Name];
                    Debug.LogError(string.Format("Duplicate Sprite Packer policies found: {0} and {1}. Please rename one.", t.FullName, otherT.FullName));
                    continue;
                }
                else
                    m_policyTypeCache[t.Name] = t;
            }

            m_selectedPolicy = String.IsNullOrEmpty(PlayerSettings.spritePackerPolicy) ? kDefaultPolicy : PlayerSettings.spritePackerPolicy;

            // Did policies change?
            if (!m_policies.Contains(m_selectedPolicy))
                SetSelectedPolicy(kDefaultPolicy);
        }

        internal static string GetSelectedPolicyId()
        {
            RegenerateList();

            Type t = m_policyTypeCache[m_selectedPolicy];
            IPackerPolicy policy = Activator.CreateInstance(t) as IPackerPolicy;
            string versionString = string.Format("{0}::{1}", t.AssemblyQualifiedName, policy.GetVersion());

            return versionString;
        }

        internal static bool AllowSequentialPacking()
        {
            RegenerateList();

            Type t = m_policyTypeCache[m_selectedPolicy];
            IPackerPolicy policy = Activator.CreateInstance(t) as IPackerPolicy;
            return policy.AllowSequentialPacking;
        }

        internal static void ExecuteSelectedPolicy(BuildTarget target, int[] textureImporterInstanceIDs)
        {
            RegenerateList();

            Type t = m_policyTypeCache[m_selectedPolicy];
            IPackerPolicy policy = Activator.CreateInstance(t) as IPackerPolicy;
            policy.OnGroupAtlases(target, new PackerJob(), textureImporterInstanceIDs);
        }

        internal static void SaveUnappliedTextureImporterSettings()
        {
            foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
            {
                ActiveEditorTracker activeEditor = i.tracker;
                foreach (Editor e in activeEditor.activeEditors)
                {
                    TextureImporterInspector inspector = e as TextureImporterInspector;
                    if (inspector == null)
                        continue;
                    if (!inspector.HasModified())
                        continue;
                    TextureImporter importer = inspector.target as TextureImporter;
                    if (EditorUtility.DisplayDialog("Unapplied import settings", "Unapplied import settings for \'" + importer.assetPath + "\'", "Apply", "Revert"))
                    {
                        inspector.ApplyAndImport(); // No way to apply/revert only some assets. Bug: 564192.
                    }
                }
            }
        }
    }

    public interface IPackerPolicy
    {
        bool AllowSequentialPacking { get; }
        void OnGroupAtlases(BuildTarget target, PackerJob job, int[] textureImporterInstanceIDs);
        int GetVersion();
    }
}
