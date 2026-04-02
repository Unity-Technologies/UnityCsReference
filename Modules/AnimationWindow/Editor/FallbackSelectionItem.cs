// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    class FallbackSelectionItem : IAnimationWindowSelectionItem
    {
        GameObject m_GameObject;
        IAnimationWindowController m_Controller;

        public bool canChangeClip => false;
        public bool canAddCurves => false;
        public bool canCreateClips => false;
        public bool canSyncSceneSelection => false;

        public int GetRefreshHash() => 0;

        public FallbackSelectionItem()
        {
            m_Controller = new DefaultAnimationWindowController
            {
                frameRate = 30f
            };
        }

        public void Dispose()
        {
        }

        public void Synchronize()
        {
        }

        public IAnimationWindowClip[] GetClips() => Array.Empty<IAnimationWindowClip>();

        public IAnimationWindowClip CreateNewClip() => null;

        public bool InitializeSelection() =>
            MecanimUtilities.InitializeGameObjectForAnimation(gameObject);

        public GameObject gameObject
        {
            get => m_GameObject;
            set => m_GameObject = value;
        }

        public GameObject rootGameObject => null;

        public IAnimationWindowClip clip
        {
            get => null;
            set {}
        }

        public IAnimationWindowController controller => m_Controller;
        public Component animationPlayer => null;
        public bool disabled => true;
        public bool isReadOnly => true;

        public bool IsCompatibleWith(UnityEngine.Object _) => false;

        public EditorCurveBinding[] GetAnimatableBindings(GameObject _)
        {
            return Array.Empty<EditorCurveBinding>();
        }

        public EditorCurveBinding[] GetAnimatableBindings()
        {
            return Array.Empty<EditorCurveBinding>();
        }

        public Type GetValueType(EditorCurveBinding _)
        {
            return null;
        }

        public void OnPlayModeStateChanged(PlayModeStateChange state)
        {
        }

        public bool isImported => false;
        public bool hasUnsavedChanges => false;
        public void SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void DiscardChanges()
        {
            throw new NotImplementedException();
        }
    }
}
