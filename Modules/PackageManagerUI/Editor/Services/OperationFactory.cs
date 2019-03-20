// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal static class OperationFactory
    {
        private static IOperationFactory _instance;

        public static IOperationFactory Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UpmOperationFactory();
                return _instance;
            }
            internal set
            {
                _instance = value;
            }
        }

        internal static void Reset()
        {
            _instance = null;
        }
    }
}
