// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from Module will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    internal abstract class Module
    {
        protected HashSet<DescriptorId> m_Ids;

        public abstract string Name
        {
            get;
        }

        public IssueCategory[] Categories
        {
            get { return SupportedLayouts.Select(l => l.Category).ToArray(); }
        }

        public IReadOnlyCollection<DescriptorId> SupportedDescriptorIds => m_Ids != null ? m_Ids.ToArray() : Array.Empty<DescriptorId>();

        public abstract IReadOnlyCollection<IssueLayout> SupportedLayouts
        {
            get;
        }

        public static string[] GetAssetPaths(AnalysisContext context)
        {
            return FilterAssetPathsArray(context, AssetDatabase.GetAllAssetPaths());
        }

        public static string[] GetAssetPathsByFilter(string filter, AnalysisContext context)
        {
            var assetsEnumerable = AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath);
            if (context.Params.AssetPathFilter != null)
            {
                assetsEnumerable = assetsEnumerable.Where(path => context.Params.AssetPathFilter(path));
            }
            return assetsEnumerable.ToArray();
        }

        static string[] FilterAssetPathsArray(AnalysisContext context, string[] assets)
        {
            var filter = context.Params.AssetPathFilter;
            if (filter != null)
            {
                var readIndex = 0;
                var writeIndex = 0;
                for (; readIndex < assets.Length; readIndex++)
                {
                    var asset = assets[readIndex];
                    if (filter(asset))
                    {
                        assets[writeIndex] = asset;
                        writeIndex++;
                    }
                }
                if (writeIndex == 0)
                {
                    return Array.Empty<string>();
                }
                if (writeIndex < readIndex)
                {
                    var newArray = new string[writeIndex];
                    Array.Copy(assets, newArray, writeIndex);
                    return newArray;
                }
            }
            return assets;
        }

        public virtual void Initialize()
        {
            m_Ids = new HashSet<DescriptorId>();
        }

        public void RegisterDescriptor(Descriptor descriptor)
        {
            // Don't register descriptors that aren't applicable to this Unity version, or to platforms that aren't supported
            if (!descriptor.IsPlatformSupported())
                return;

            if (!descriptor.IsVersionCompatible())
                return;

            DescriptorLibrary.RegisterDescriptor(descriptor.Id, descriptor);

            if (!m_Ids.Add(descriptor.Id))
                throw new Exception("Duplicate descriptor with Id: " + descriptor.Id);
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="analysisParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null);
    }
}
