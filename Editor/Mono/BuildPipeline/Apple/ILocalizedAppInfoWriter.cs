// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.Apple
{
    // The localization module resolves the localized values but cannot write them: the property list and Xcode
    // project types live in the Apple platform support assemblies, which no engine module can reference. It hands
    // this payload to whichever implementation the installed Apple platform support provides. The attribute carries
    // no assembly list on purpose, because the implementations live in platform support assemblies rather than
    // modules and cannot be named ahead of time.
    [VisibleToOtherModules]
    internal readonly struct LocalizedAppInfo
    {
        // Xcode's development region, which names the localization the operating system falls back to.
        public string DevelopmentRegion { get; }

        // One entry per locale, each holding the property list keys to write for it.
        public IReadOnlyList<LocalizedAppInfoLocale> Locales { get; }

        public LocalizedAppInfo(string developmentRegion, IReadOnlyList<LocalizedAppInfoLocale> locales)
        {
            DevelopmentRegion = developmentRegion;
            Locales = locales;
        }
    }

    [VisibleToOtherModules]
    internal readonly struct LocalizedAppInfoLocale
    {
        // The locale code as Apple writes it, so it can name an .lproj folder directly.
        public string Code { get; }

        // Property list key to localized value, for example CFBundleDisplayName.
        public IReadOnlyDictionary<string, string> Values { get; }

        public LocalizedAppInfoLocale(string code, IReadOnlyDictionary<string, string> values)
        {
            Code = code;
            Values = values;
        }
    }

    [VisibleToOtherModules]
    internal interface ILocalizedAppInfoWriter
    {
        // Several Apple platform supports can be installed at once, each with its own writer, so a writer says which
        // targets it handles rather than leaving the choice to whichever type is discovered first.
        bool CanWrite(BuildTarget target);

        // Writes the payload into an Xcode project Unity has already generated. Returns false when the project
        // cannot be found, which happens on macOS unless the build creates an Xcode project.
        bool Write(string builtProjectPath, BuildTarget target, LocalizedAppInfo appInfo);
    }
}
