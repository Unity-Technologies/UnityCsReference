// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    abstract class CameraController
    {
        // Actual API to be defined
        // Passing the associated Camera as a parameter should not be needed (only CameraState should be important). See how we can break that dependency when doing the proper refactoring.
        public abstract void Update(CameraState cameraState, Camera cam);
    }
}
