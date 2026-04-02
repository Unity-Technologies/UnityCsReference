// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    /// <summary>
    /// Utility class for creating and converting LoadableReference objects in the Editor.
    /// LoadableReference is the low-level reference type used by Loadable for on-demand asset loading.
    /// This utility provides methods to convert between Unity Objects and LoadableReferences.
    /// </summary>
    /// <seealso cref="LoadableReference"/>
    [NativeHeader("Editor/Src/Utility/LoadableReferenceEditorUtility.bindings.h")]
    [VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal static class LoadableReferenceEditorUtility
    {
        /// <summary>
        /// Deconstructs a LoadableReference into its component parts: GUID, local file identifier, and file identifier type.
        /// </summary>
        /// <remarks>
        /// This method will return false if the LoadableReference contains a runtime handle.
        /// Runtime handles are only interpretable by the ContentLoadManager and cannot be converted to GUID/local file identifier.
        /// This method provides the complete inverse of <see cref="CreateLoadableReference(GUID, FileIdentifierType, long)"/>,
        /// returning all three components that were used to construct the LoadableReference.
        /// </remarks>
        /// <param name="loadableRef">The LoadableReference to deconstruct.</param>
        /// <param name="guid">The GUID of the asset file containing the referenced object.</param>
        /// <param name="localId">The local file identifier of the object within the asset.</param>
        /// <param name="fileType">The file identifier type indicating whether this is a source asset, primary artifact, or non-asset reference.</param>
        /// <returns>
        /// True if the LoadableReference represents an asset reference and all components were successfully retrieved;
        /// false if the LoadableReference is a runtime handle.
        /// </returns>
        /// <seealso cref="CreateLoadableReference(GUID, FileIdentifierType, long)"/>
        /// <seealso cref="AssetDatabase.TryGetGUIDAndLocalFileIdentifier"/>
        public static bool TryDeconstructLoadableReference(LoadableReference loadableRef, out GUID guid, out long localId, out FileIdentifierType fileType)
        {
            // If this is a runtime handle (has ObjectIdHash), we cannot extract GUID/lfid/type
            if (loadableRef.m_ObjectIdHash.isValid)
            {
                guid = new GUID();
                localId = 0;
                fileType = FileIdentifierType.NonAsset;
                return false;
            }

            guid = loadableRef.m_GUID;
            localId = loadableRef.m_LocalIdentifierInFile;
            fileType = loadableRef.m_FileIdentifierType;
            return true;
        }

        /// <summary>
        /// Retrieve the Object referenced by a LoadableReference.
        /// </summary>
        /// <remarks>
        /// This method returns the object in the Editor source asset, not the object in the build output.
        /// </remarks>
        /// <param name="loadableRef">The LoadableReference to convert.</param>
        /// <returns>
        /// The Unity Object referenced by the LoadableReference, or null if the reference is invalid.
        /// </returns>
        public static Object LoadableReferenceToObject(LoadableReference loadableRef)
        {
            return LoadableReferenceToObjectInternal(loadableRef);
        }

        /// <summary>
        /// Converts a Unity Object to a LoadableReference that can be used for on-demand loading.
        /// </summary>
        /// <param name="obj">
        /// A Unity Object that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be referenced by
        /// LoadableReference.
        /// </param>
        /// <returns>
        /// A LoadableReference that points to the specified object, or an empty LoadableReference if obj is null
        /// or if it does not belong to a persisted asset.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the object is non-persistent (cannot be serialized to disk).
        /// </exception>
        public static LoadableReference ObjectToLoadableReference(Object obj)
        {
            return ObjectToLoadableReferenceInternal(obj);
        }

        /// <summary>
        /// Creates a LoadableReference from an EntityId.
        /// </summary>
        /// <param name="entityID">
        /// The EntityId of a Unity Object that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be
        /// referenced by LoadableReference.
        /// </param>
        /// <returns>A LoadableReference that points to the object with the specified EntityId.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the EntityId is invalid or if the object is non-persistent.
        /// </exception>
        public static LoadableReference CreateLoadableReference(EntityId entityID)
        {
            return EntityIDToLoadableReferenceInternal(entityID);
        }

        /// <summary>
        /// Creates a LoadableReference from its component parts.
        /// </summary>
        /// <param name="guid">The GUID of the asset file containing the object.</param>
        /// <param name="type">The identifier of the reference type.</param>
        /// <param name="fileID">The local file ID of the object within the asset.</param>
        /// <returns>A LoadableReference constructed from the specified components.</returns>
        /// <seealso cref="GlobalObjectId"/>
        public static LoadableReference CreateLoadableReference(GUID guid, FileIdentifierType type, long fileID)
        {
            var loadableRef = new LoadableReference();
            loadableRef.m_GUID = guid;
            loadableRef.m_FileIdentifierType = type;
            loadableRef.m_LocalIdentifierInFile = fileID;
            loadableRef.m_ObjectIdHash = new Hash128();
            return loadableRef;
        }

        /// <summary>
        /// Draws an IMGUI field for a LoadableReference property. Used by LoadableReferenceDrawer and LoadableDrawer for nested m_LoadableRef.
        /// </summary>
        /// <param name="position">Rect to draw the field in.</param>
        /// <param name="property">SerializedProperty of type LoadableReference.</param>
        /// <param name="label">Label for the field.</param>
        /// <param name="objectType">Type of Unity Object that can be assigned (e.g. Texture2D, or UnityEngine.Object for any).</param>
        [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
        internal static void DrawLoadableReferenceField(Rect position, SerializedProperty property, GUIContent label, Type objectType)
        {
            EditorGUI.BeginProperty(position, label, property);
            var loadableRef = property.loadableReferenceValue;
            var currentObj = LoadableReferenceToObject(loadableRef);

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            int id = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.DoLoadableObjectField(fieldRect, fieldRect, id, currentObj, null, objectType, property, null, false);

            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    var newRef = ObjectToLoadableReference(newObj);
                    if (newObj != null && !newRef.isValid)
                        Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableReference."));
                    else
                        property.loadableReferenceValue = newRef;
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableReference: {0}"), e.Message));
                }
            }
            EditorGUI.EndProperty();
        }

        [FreeFunction("LoadableReferenceUtility::LoadableReferenceToObject", ThrowsException = true)]
        private static extern Object LoadableReferenceToObjectInternal(LoadableReference loadableRef);

        [FreeFunction("LoadableReferenceUtility::ObjectToLoadableReference", ThrowsException = true)]
        private static extern LoadableReference ObjectToLoadableReferenceInternal(Object obj);

        [FreeFunction("LoadableReferenceUtility::EntityIDToLoadableReference", ThrowsException = true)]
        private static extern LoadableReference EntityIDToLoadableReferenceInternal(EntityId instanceID);
    }
}
