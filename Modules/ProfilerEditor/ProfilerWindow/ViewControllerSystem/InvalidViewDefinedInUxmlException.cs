// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor.UI
{
    class InvalidViewDefinedInUxmlException : Exception
    {
        const string k_Message = "Unable to create view from Uxml. Uxml must contain at least one child element.";
        public InvalidViewDefinedInUxmlException() : base(k_Message) { }
    }
}
