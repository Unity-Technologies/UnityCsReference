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

                        OnGameObjectChanged(TryGetGameObject(e.entityId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectParent:
                    {
                        stream.GetChangeGameObjectParentEvent(i, out var e);

                        if (e.previousParentEntityId != EntityId.None)
                            OnGameObjectChanged(TryGetGameObject(e.previousParentEntityId));

                        if (e.newParentEntityId != EntityId.None)
                            OnGameObjectChanged(TryGetGameObject(e.newParentEntityId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var e);

                        OnGameObjectChanged(TryGetGameObject(e.entityId));
                    }
                    break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out var e);

                        OnGameObjectChanged(TryGetGameObject(e.entityId));
                    }
                    break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                    {
                        stream.GetUpdatePrefabInstancesEvent(i, out var e);

                        for (int index = 0; index < e.entityIds.Length; index++)
                            OnGameObjectChanged(TryGetGameObject(e.entityIds[index]));
                    }
                    break;

                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    {
                        stream.GetCreateGameObjectHierarchyEvent(i, out var e);

                        GameObject go = TryGetGameObject(e.entityId);
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

                        GameObject goParent = TryGetGameObject(e.parentEntityId);
                        if (goParent != null)
                            OnGameObjectChanged(goParent);
                    }
                    break;
                }
            }
        }

        private static GameObject TryGetGameObject(EntityId entityId)
        {
            Component comp = EditorUtility.EntityIdToObject(entityId) as Component;
            if (comp)
                return comp.gameObject;

            return EditorUtility.EntityIdToObject(entityId) as GameObject;
        }

        private static void OnGameObjectChanged(GameObject go)
        {
            if (!go)
                return;

            PrefabUtility.ClearPrefabInstanceNonDefaultOverridesCache(go);
            PrefabUtility.ClearPrefabInstanceUnusedOverridesCache(go);
        }
    }
}
