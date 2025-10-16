// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Dev project
[assembly: InternalsVisibleTo("GraphToolkitTestProject")]

// UGTK
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Internal.Editor")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Editor")]

// UGTK Tests
[assembly: InternalsVisibleTo("Unity.Modules.GraphToolkit.CommandStateObserver.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Modules.GraphToolkit.Tests.Common")]
[assembly: InternalsVisibleTo("Unity.Modules.GraphToolkit.Internal.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Editor.Tests.Performance")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Editor.Tests.UI")]

// Samples (Todo: Should remove these when the public samples use the public api)
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.VisualNovelDirector.Editor")]

// Unity users
[assembly: InternalsVisibleTo("Unity.Motion.Editor")]
[assembly: InternalsVisibleTo("Unity.Motion.Editor.Tests")]

// Test Samples
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.BlackboardSample")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.ContextSample")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.ImportedGraphEditor")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.ItemLibrary")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.RecipesEditor")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.SimpleMathBook")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.StateMachine")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.TestSample")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.VerticalFlow")]
[assembly: InternalsVisibleTo("Unity.GraphToolkit.Samples.SampleSupport")]
