using System;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace Unity.DataModel
{
    [AutoStaticsCleanup(CleanupStrategy = CleanupStrategy.Clear)]
    [VisibleToOtherModules]
    internal class UdmInterop
    {
	[VisibleToOtherModules]
        internal static IUdmInterop Instance
        {
            get
            {
                // Getter logic here
                return _instance;
            }
            set
            {
                // Setter logic here
                _instance = value;
            }
        }

        internal void Clear()
        {
            _instance.cleanup();
        }

        private static IUdmInterop _instance = default; // Backing field for the property
    }
}
