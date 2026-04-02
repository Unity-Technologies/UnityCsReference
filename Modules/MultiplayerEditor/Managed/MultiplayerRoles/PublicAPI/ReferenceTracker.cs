// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Editor
{
    internal struct ReferenceTracker
    {
        private HashSet<EntityId> m_StrippedObjectsInstanceIds; // Objects that should be stripped
        private HashSet<EntityId> m_ReferencesInstanceIds; // Objects referenced by any other not stripped object
        private List<(EntityId, EntityId)> m_ReferencePairs; // Pairs of (reference, referencer) -- where the referencer is not stripped
        private GameObject[] m_GameObjects; // Objects to collect data from
        private MultiplayerRoleFlags m_ActiveMultiplayerRoleMask;
        private List<Component> m_ComponentsCache;

        public ReferenceTracker(MultiplayerRoleFlags activeMultiplayerRoleMask, GameObject[] gameObjects)
        {
            m_ComponentsCache = new List<Component>();
            m_StrippedObjectsInstanceIds = new ();
            m_ReferencesInstanceIds = new ();
            m_ReferencePairs = new ();
            m_GameObjects = gameObjects;
            m_ActiveMultiplayerRoleMask = activeMultiplayerRoleMask;
        }

        public ReferenceTracker(MultiplayerRoleFlags activeMultiplayerRoleMask, Scene scene) : this(activeMultiplayerRoleMask, scene.GetRootGameObjects()) { }

        private void TrackStrippedObject(Object obj)
        {
            m_StrippedObjectsInstanceIds.Add(obj.GetEntityId());
        }

        private void TrackReference(Object referencer, Object reference)
        {
            if (reference == null)
                return;

            var referencerInstanceId = referencer.GetEntityId();
            var referenceInstanceId = reference.GetEntityId();

            m_ReferencesInstanceIds.Add(referenceInstanceId);
            m_ReferencePairs.Add((referenceInstanceId, referencerInstanceId));
        }

        public void Collect()
        {
            foreach (var gameObject in m_GameObjects)
                CollectRecursive(gameObject.transform, false);
        }

        private void CollectRecursive(Transform transform, bool parentStripped)
        {
            transform.gameObject.GetComponents(m_ComponentsCache);

            parentStripped = parentStripped || EditorMultiplayerRolesManager.ShouldStrip(m_ActiveMultiplayerRoleMask, transform.gameObject);

            if (parentStripped)
                TrackStrippedObject(transform.gameObject);

            foreach (var component in m_ComponentsCache)
            {
                if (component == null)
                    continue;

                if (component is MultiplayerRolesData)
                    continue;

                if (parentStripped || EditorMultiplayerRolesManager.ShouldStrip(m_ActiveMultiplayerRoleMask, component))
                {
                    TrackStrippedObject(component);
                    continue;
                }

                using var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        TrackReference(component, property.objectReferenceValue);
                }
            }
        }

        public void WarnBrokenReferencesIfNeeded()
        {
            var brokenReferences = new HashSet<EntityId>(m_ReferencesInstanceIds);
            brokenReferences.IntersectWith(m_StrippedObjectsInstanceIds);
            if (brokenReferences.Count == 0)
                return;

            var message = $"The following objects are stripped for the current multiplayer role ({m_ActiveMultiplayerRoleMask}) but they are referenced by other objects in the scene. This could lead to null reference errors.\n";

            foreach (var referencePair in m_ReferencePairs)
            {
                if (brokenReferences.Contains(referencePair.Item1))
                {
                    var reference = EditorUtility.EntityIdToObject(referencePair.Item1);
                    var referencer = EditorUtility.EntityIdToObject(referencePair.Item2);
                    message += $"- {reference.name} ({reference.GetType().Name}) referenced by {referencer.name} ({referencer.GetType().Name})\n";
                }
            }

            Debug.LogWarning(message);
        }
    }
}
