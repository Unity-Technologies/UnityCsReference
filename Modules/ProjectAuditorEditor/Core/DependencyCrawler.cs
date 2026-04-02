// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Core
{
    class CallInfo : IEquatable<CallInfo>
    {
        public MethodReference Callee { get; }
        public MethodReference Caller { get; }
        public Location Location { get; }
        public bool IsPerfCriticalContext { get; }
        public CallTreeNode Hierarchy { get; set; }

        public CallInfo(
            MethodReference callee,
            MethodReference caller,
            Location location,
            bool isPerfCriticalContext)
        {
            Callee = callee;
            Caller = caller;
            Location = location;
            IsPerfCriticalContext = isPerfCriticalContext;
        }

        public bool Equals(CallInfo other)
        {
            return Callee == other.Callee && Caller == other.Caller;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CallInfo))
                return false;

            return Equals((CallInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Callee.GetHashCode(), Caller.GetHashCode());
        }
    }

    class DependencyCrawler
    {
        readonly Dictionary<string, HashSet<CallInfo>> m_BucketedCalls = new Dictionary<string, HashSet<CallInfo>>(512);            // key: callee name, value: lists of all callers
        readonly Dictionary<string, HashSet<string>> m_AssetDependencyReverseLookup = new Dictionary<string, HashSet<string>>(512);
        readonly List<Material> m_Materials = new List<Material>();
        readonly Dictionary<SpriteAtlas, string> m_SpriteAtlasPaths = new Dictionary<SpriteAtlas, string>();
        readonly HashSet<string> m_VisitedAssets = new HashSet<string>(512);

        public void AddToCodeCache(MethodReference callee, MethodReference caller, Location location, bool isPerfCriticalContext)
        {
            var key = callee.FastFullName();
            if (!m_BucketedCalls.TryGetValue(key, out var calls))
            {
                calls = new HashSet<CallInfo>();
                m_BucketedCalls.Add(key, calls);
            }
            calls.Add(new CallInfo(callee, caller, location, isPerfCriticalContext));
        }

        public void AddToAssetDependencyCache(string assetPath)
        {
            if (!m_VisitedAssets.Add(assetPath))
                return;

            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            foreach (var dep in dependencies)
            {
                if (m_AssetDependencyReverseLookup.TryGetValue(dep, out var list))
                    list.Add(assetPath);
                else
                    m_AssetDependencyReverseLookup.Add(dep, new HashSet<string> { assetPath });

                AddToAssetDependencyCache(dep);
            }
        }

        public void AddToMaterialCache(Material material)
        {
            m_Materials.Add(material);
        }

        public void AddToSpriteAtlasCache(SpriteAtlas atlas)
        {
            m_SpriteAtlasPaths[atlas] = AssetDatabase.GetAssetPath(atlas);
        }

        public void BuildHierarchies(IReadOnlyCollection<ReportItem> issues)
        {
            // Dependencies can be shared by multiple issues, e.g. see AnalyzeMethodBody
            var uniqueDependencyNodes = new HashSet<DependencyNode>(issues.Count);
            foreach (var issue in issues)
            {
                var root = issue.Dependencies;
                if (root == null)
                    continue;

                uniqueDependencyNodes.Add(root);

                // temp fix for null location (code analysis was unable to get sequence point)
                if (issue.Location == null)
                    issue.Location = root.Location;
            }

            var context = BuildContext();
            foreach (var root in uniqueDependencyNodes)
                root.BuildHierarchy(0, context);
        }

        DependencyBuildContext BuildContext()
        {
            var shaderToMaterials = new Dictionary<string, HashSet<string>>();
            var textureToMaterials = new Dictionary<string, HashSet<string>>();

            foreach (var mat in m_Materials)
            {
                var matPath = AssetDatabase.GetAssetPath(mat);

                var shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                if (!string.IsNullOrEmpty(shaderPath))
                {
                    if (!shaderToMaterials.TryGetValue(shaderPath, out var matSet))
                        shaderToMaterials[shaderPath] = matSet = new HashSet<string>();
                    matSet.Add(matPath);
                }

                foreach (var nameID in mat.GetTexturePropertyNameIDs())
                {
                    var texture = mat.GetTexture(nameID);
                    if (texture == null)
                        continue;

                    var texturePath = AssetDatabase.GetAssetPath(texture);
                    if (string.IsNullOrEmpty(texturePath))
                        continue;

                    if (!textureToMaterials.TryGetValue(texturePath, out var texSet))
                        textureToMaterials[texturePath] = texSet = new HashSet<string>();
                    texSet.Add(matPath);
                }
            }

            return new DependencyBuildContext(m_BucketedCalls, m_AssetDependencyReverseLookup, shaderToMaterials, textureToMaterials, m_SpriteAtlasPaths);
        }
    }
}
