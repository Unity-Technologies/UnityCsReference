// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class UnityAPICompatibilityVersionAttribute : Attribute
    {
        [Obsolete("This overload of the attribute has been deprecated. Use the constructor that takes the version and a boolean", true)]
        public UnityAPICompatibilityVersionAttribute(string version)
        {
            _version = version;
        }

        ///<summary>
        ///Instructs APIUpdater to only check Unity version when deciding whether assemblies should
        ///be inspected for obsolete API usages, ignoring any other sources of update configurations.
        ///This overload is meant to be used only during development cycle in scenarios in which
        ///game code is built outside unity and imported as assemblies. As an alternative you should consider
        ///passing disable-assembly-updater as a command line argument (see Unity manual for more details.)
        ///</summary>
        ///<params>
        ///</param>
        public UnityAPICompatibilityVersionAttribute(string version, bool checkOnlyUnityVersion)
        {
            if (!checkOnlyUnityVersion)
                throw new ArgumentException("You must pass 'true' to checkOnlyUnityVersion parameter.");

            _version = version;
        }

        public UnityAPICompatibilityVersionAttribute(string version, string[] configurationAssembliesHashes)
        {
            _version = version;
            _configurationAssembliesHashes = configurationAssembliesHashes;
        }

        public string version { get { return _version; } }
        internal string[] configurationAssembliesHashes => _configurationAssembliesHashes;

        private string _version;
        private string[] _configurationAssembliesHashes;
    }
}
