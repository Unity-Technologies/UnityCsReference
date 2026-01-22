// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            get { return SupportedLayouts.Select(l => l.Category).ToArray(); }
#pragma warning restore RS0030
        }

        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IReadOnlyCollection<DescriptorId> SupportedDescriptorIds => m_Ids != null ? m_Ids.ToArray() : Array.Empty<DescriptorId>();
#pragma warning restore RS0030

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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var assetsEnumerable = AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath);
#pragma warning restore RS0030
            if (context.Params.AssetPathFilter != null)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                assetsEnumerable = assetsEnumerable.Where(path => context.Params.AssetPathFilter(path));
#pragma warning restore RS0030
            }
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return assetsEnumerable.ToArray();
#pragma warning restore RS0030
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

        public AnalysisCoroutine QueueAnalysisCoroutine(IEnumerator routine, Module owner, Action<long> elapsedTimeDelegate)
        {
            return new AnalysisCoroutine(routine, owner, elapsedTimeDelegate);
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

        protected bool AdvanceAsyncProgress(IProgress progress, AsyncProgressState state, string description = "")
        {
            if (progress != null)
            {
                progress.Advance(state, description);
                if (progress.IsCancelled)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="analysisParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract IEnumerator Audit(AnalysisParams analysisParams, IProgress progress);
    }
}
