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
        PackageSelectionObject GetPackageSelectionObject(string packageUniqueId, bool createIfNotFound = false);
        PackageSelectionObject GetPackageSelectionObject(IPackage package, bool createIfNotFound = false);
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
            var selectionIds = new List<string>();
            var selectedPackages = new List<IPackage>();
            foreach (var selectionObject in m_Selection.objects)
            {
                var packageSelectionObject = selectionObject as PackageSelectionObject;
                if (packageSelectionObject == null)
                    return;
                var package = m_PackageDatabase.GetPackage(packageSelectionObject.packageUniqueId);
                if (package == null)
                    continue;
                selectedPackages.Add(package);
                selectionIds.Add(packageSelectionObject.packageUniqueId);
            }

            if (!selectionIds.Any())
                return;

            var page = m_PageManager.FindPage(selectedPackages);
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

        public PackageSelectionObject GetPackageSelectionObject(string packageUniqueId, bool createIfNotFound = false)
        {
            var package = m_PackageDatabase.GetPackage(packageUniqueId);
            return GetPackageSelectionObject(package, createIfNotFound);
        }

        // The virtual keyword is needed for unit tests
        public virtual PackageSelectionObject GetPackageSelectionObject(IPackage package, bool createIfNotFound = false)
        {
            if (package == null)
                return null;

            var uniqueId = package.uniqueId;
            var packageSelectionObject = m_PackageSelectionObjects.Get(uniqueId);
            if (packageSelectionObject == null && createIfNotFound)
            {
                packageSelectionObject = ScriptableObject.CreateInstance<PackageSelectionObject>();
                packageSelectionObject.hideFlags = HideFlags.DontSave;
                packageSelectionObject.name = package.displayName;
                packageSelectionObject.displayName = package.displayName;
                packageSelectionObject.packageUniqueId = package.uniqueId;
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
                    m_PackageSelectionObjects.Remove(packageSelectionObject.packageUniqueId);
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
                    m_PackageSelectionObjects[packageSelectionObject.packageUniqueId] = packageSelectionObject;
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
