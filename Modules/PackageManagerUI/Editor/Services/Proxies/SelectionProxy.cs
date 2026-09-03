// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Packman not yet converted
using System;
using System.Diagnostics.CodeAnalysis;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ISelectionProxy : IService
    {
        event Action onSelectionChanged;

        Object[] objects { get; set; }
        Object activeObject { get; set; }
    }

    [ExcludeFromCodeCoverage]
    internal class SelectionProxy : BaseService<ISelectionProxy>, ISelectionProxy
    {
        public event Action onSelectionChanged = delegate {};

        public SelectionProxy()
        {
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            Selection.selectionChanged += OnSelectionChanged;
            #pragma warning restore UAL0015
        }

        public Object[] objects
        {
            get { return Selection.objects; }
            set { Selection.objects = value; }
        }

        public Object activeObject
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
