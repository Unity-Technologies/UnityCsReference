// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Widgets;

namespace Unity.Timeline.Foundation.View.Internals
{
    static class UIResources
    {
        const string k_AssemblyPath = "TimelineFoundation/View/";
        const string k_TemplatePath = k_AssemblyPath + "templates/";
        const string k_StylesheetPath = k_AssemblyPath + "stylesheets/";

        public static readonly TemplateResourceFactory TemplateFactory = new(k_TemplatePath);
        public static readonly StylesheetResourceFactory StylesheetFactory = new(k_StylesheetPath);

        public static readonly StylesheetResource OverlayStylesheet = StylesheetFactory.Get("Overlays");
        public static readonly StylesheetResource TrackStylesheet = StylesheetFactory.Get("Track");
    }
}
