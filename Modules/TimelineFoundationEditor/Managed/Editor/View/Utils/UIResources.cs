// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using Unity.Timeline.Foundation.Widgets;

namespace Unity.Timeline.Foundation.View.Internals
{
    static class UIResources
    {
        const string k_AssemblyPath = "TimelineFoundation/View/";
        const string k_TemplatePath = k_AssemblyPath + "templates/";
        const string k_StylesheetPath = k_AssemblyPath + "stylesheets/";

        [NoAutoStaticsCleanup] // Immutable resource-path factory; holds only a fixed directory string, safe to persist across reload.
        public static readonly TemplateResourceFactory TemplateFactory = new(k_TemplatePath);
        [NoAutoStaticsCleanup] // Immutable resource-path factory; holds only a fixed directory string, safe to persist across reload.
        public static readonly StylesheetResourceFactory StylesheetFactory = new(k_StylesheetPath);

        [NoAutoStaticsCleanup] // Immutable USS stylesheet-path descriptor; holds only fixed path strings, safe to persist across reload.
        public static readonly StylesheetResource OverlayStylesheet = StylesheetFactory.Get("Overlays");
        [NoAutoStaticsCleanup] // Immutable USS stylesheet-path descriptor; holds only fixed path strings, safe to persist across reload.
        public static readonly StylesheetResource TrackStylesheet = StylesheetFactory.Get("Track");
    }
}
