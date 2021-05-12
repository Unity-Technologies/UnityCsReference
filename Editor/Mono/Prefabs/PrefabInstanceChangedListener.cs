// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    class PrefabInstanceChangedListener
    {
        static PrefabInstanceChangedListener()
        {
            ObjectChangeEvents.changesPublished += OnObjectChanged;
        }

        private static void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var eventType = stream.GetEventType(i);

                switch (eventType)
                {
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                    {
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var e);

                        OnGameObjectChanged(TryGetGameObject(e.instanceId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectParent:
                    {
                        stream.GetChangeGameObjectParentEvent(i, out var e);

                        if (e.previousParentInstanceId != 0)
                            OnGameObjectChanged(TryGetGameObject(e.previousParentInstanceId));

                        if (e.newParentInstanceId != 0)
                            OnGameObjectChanged(TryGetGameObject(e.newParentInstanceId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var e);

                        OnGameObjectChanged(TryGetGameObject(e.instanceId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var e);

                        OnGameObjectChanged(TryGetGameObject(e.instanceId));
                    }
                    break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                    {
                        stream.GetUpdatePrefabInstancesEvent(i, out var e);

                        for (int index = 0; index < e.instanceIds.Length; index++)
                            OnGameObjectChanged(TryGetGameObject(e.instanceIds[index]));
                    }
                    break;

                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    {
                        stream.GetCreateGameObjectHierarchyEvent(i, out var e);

                        GameObject go = TryGetGameObject(e.instanceId);
                        if (go != null && go.transform.parent)
                        {
                            go = go.transform.parent.gameObject;

                            OnGameObjectChanged(go);
                        }
                    }
                    break;

                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                    {
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var e);

                        GameObject goParent = TryGetGameObject(e.parentInstanceId);
                        if (goParent != null)
                            OnGameObjectChanged(goParent);
                    }
                    break;
                }
            }
        }

        private static GameObject TryGetGameObject(int instanceId)
        {
            Component comp = EditorUtility.InstanceIDToObject(instanceId) as Component;
            if (comp)
                return comp.gameObject;

            return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        }

        private static void OnGameObjectChanged(GameObject go)
        {
            if (!go)
                return;

            PrefabUtility.ClearPrefabInstanceNonDefaultOverridesCache(go);
        }
    }
}
