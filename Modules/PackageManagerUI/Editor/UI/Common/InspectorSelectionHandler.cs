// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IInspectorSelectionHandler : IService
    {
        PackageSelectionObject GetPackageSelectionObject(PackageAndVersionIdPair pair, bool createIfNotFound = false);
        PackageSelectionObject GetPackageSelectionObject(IPackage package, IPackageVersion version = null, bool createIfNotFound = false);
    }

    [Serializable]
    internal class InspectorSelectionHandler : BaseService<IInspectorSelectionHandler>, IInspectorSelectionHandler, ISerializationCallbackReceiver
    {
        [SerializeField]
        private int[] m_SerializedPackageSelectionInstanceIds = Array.Empty<int>();

        private Dictionary<string, PackageSelectionObject> m_PackageSelectionObjects = new();

        private readonly ISelectionProxy m_Selection;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        public InspectorSelectionHandler(ISelectionProxy selection, IPackageDatabase packageDatabase, IPageManager pageManager)
        {
            m_Selection = RegisterDependency(selection);
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_PageManager = RegisterDependency(pageManager);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPackageSelectionInstanceIds = m_PackageSelectionObjects.Select(kp => kp.Value.GetInstanceID()).ToArray();
        }

        public void OnAfterDeserialize() {}

        private void OnEditorSelectionChanged()
        {
            var selectionIds = new List<PackageAndVersionIdPair>();
            var selectedVersions = new List<IPackageVersion>();
            foreach (var selectionObject in m_Selection.objects)
            {
                var packageSelectionObject = selectionObject as PackageSelectionObject;
                if (packageSelectionObject == null)
                    return;
                m_PackageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out var package, out var version);
                if (package == null && version == null)
                    continue;
                selectedVersions.Add(version ?? package?.versions.primary);
                selectionIds.Add(new PackageAndVersionIdPair(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId));
            }

            if (!selectionIds.Any())
                return;

            var page = m_PageManager.FindPage(selectedVersions);
            if (page != null)
            {
                m_PageManager.activePage = page;
                page.SetNewSelection(selectionIds);
            }
        }

        private void OnPageSelectionChanged(PageSelectionChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            // There are 3 situations when we want to select a package in the inspector
            // 1) we explicitly/force to select it as a result of a manual action (in this case, isExplicitUserSelection should be set to true)
            // 2) currently there's no active selection at all
            // 3) currently another package is selected in inspector, hence we are sure that we are not stealing selection from some other window
            if (args.isExplicitUserSelection || m_Selection.activeObject == null || m_Selection.activeObject is PackageSelectionObject)
            {
                var packageSelectionObjects = args.selection.Select(s => GetPackageSelectionObject(s, true)).ToArray();
                m_Selection.objects = packageSelectionObjects;
            }
        }

        public PackageSelectionObject GetPackageSelectionObject(PackageAndVersionIdPair pair, bool createIfNotFound = false)
        {
            m_PackageDatabase.GetPackageAndVersion(pair?.packageUniqueId, pair?.versionUniqueId, out var package, out var version);
            return GetPackageSelectionObject(package, version, createIfNotFound);
        }

        // The virtual keyword is needed for unit tests
        public virtual PackageSelectionObject GetPackageSelectionObject(IPackage package, IPackageVersion version = null, bool createIfNotFound = false)
        {
            if (package == null)
                return null;

            var uniqueId = version?.uniqueId ?? package.uniqueId;
            var packageSelectionObject = m_PackageSelectionObjects.Get(uniqueId);
            if (packageSelectionObject == null && createIfNotFound)
            {
                packageSelectionObject = ScriptableObject.CreateInstance<PackageSelectionObject>();
                packageSelectionObject.hideFlags = HideFlags.DontSave;
                packageSelectionObject.name = package.displayName;
                packageSelectionObject.displayName = package.displayName;
                packageSelectionObject.packageUniqueId = package.uniqueId;
                packageSelectionObject.versionUniqueId = version?.uniqueId;
                m_PackageSelectionObjects[uniqueId] = packageSelectionObject;
            }
            return packageSelectionObject;
        }

        public void OnPackagesChanged(PackagesChangeArgs args)
        {
            foreach (var package in args.removed)
            {
                var packageSelectionObject = GetPackageSelectionObject(package);
                if (packageSelectionObject != null)
                {
                    m_PackageSelectionObjects.Remove(packageSelectionObject.uniqueId);
                    UnityEngine.Object.DestroyImmediate(packageSelectionObject);
                }
            }
        }

        private void InitializeSelectionObjects()
        {
            foreach (var id in m_SerializedPackageSelectionInstanceIds)
            {
                var packageSelectionObject = UnityEngine.Object.FindObjectFromInstanceID(id) as PackageSelectionObject;
                if (packageSelectionObject != null)
                    m_PackageSelectionObjects[packageSelectionObject.uniqueId] = packageSelectionObject;
            }
        }

        public override void OnEnable()
        {
            InitializeSelectionObjects();

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PageManager.onSelectionChanged += OnPageSelectionChanged;
            m_Selection.onSelectionChanged += OnEditorSelectionChanged;
        }

        public override void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PageManager.onSelectionChanged -= OnPageSelectionChanged;
            m_Selection.onSelectionChanged -= OnEditorSelectionChanged;
        }
    }
}
