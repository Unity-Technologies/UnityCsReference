// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.PropertiesModule")]

[assembly: InternalsVisibleTo("Unity.Properties.CodeGen.IntegrationTests")]
[assembly: InternalsVisibleTo("PropertyBags.GenerationTests")]
[assembly: InternalsVisibleTo("Unity.Properties.Reflection.Tests")]
[assembly: InternalsVisibleTo("Unity.Properties.Tests")]

[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")] // ConversionRegistry
[assembly: InternalsVisibleTo("UnityEditor.UIElementsModule")] // PropertyBag.AddJobToWaitQueue

[assembly: InternalsVisibleTo("Unity.UIElements.RuntimeTests")] // ReflectedPropertyBag<TContainer>
