// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ISelectionProxy : IService
    {
        event Action onSelectionChanged;

        UnityEngine.Object[] objects { get; set; }
        UnityEngine.Object activeObject { get; set; }
    }

    [ExcludeFromCodeCoverage]
    internal class SelectionProxy : BaseService<ISelectionProxy>, ISelectionProxy
    {
        public event Action onSelectionChanged = delegate {};

        public SelectionProxy()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        public UnityEngine.Object[] objects
        {
            get { return Selection.objects; }
            set { Selection.objects = value; }
        }

        public UnityEngine.Object activeObject
        {
            get { return Selection.activeObject; }
            set { Selection.activeObject = value; }
        }

        private void OnSelectionChanged()
        {
            onSelectionChanged?.Invoke();
        }
    }
}
