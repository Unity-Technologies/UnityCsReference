// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.Loading;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    /// <summary>
    /// Utility class for creating and converting LoadableObjectId objects in the Editor.
    /// LoadableObjectId is the low-level reference type used by Loadable for on-demand asset loading.
    /// This utility provides methods to convert between Unity Objects and LoadableObjectIds.
    /// </summary>
    /// <seealso cref="LoadableObjectId"/>
    [NativeHeader("Editor/Src/Utility/LoadableObjectIdEditorUtility.bindings.h")]
    [VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal static class LoadableObjectIdEditorUtility
    {
        static readonly string kLoadableObjectIdTooltip = L10n.Tr("References an object that will be included in the Content Directory build and can be loaded asynchronously on demand.");
        internal static string GetLoadableObjectIdTooltip()
        {
            return kLoadableObjectIdTooltip;
        }

        /// <summary>
        /// Deconstructs a LoadableObjectId into its component parts: GUID, local file identifier, and file identifier type.
        /// </summary>
        /// <remarks>
        /// This method will return false if the LoadableObjectId contains a runtime handle.
        /// Runtime handles are only interpretable by the ContentLoadManager and cannot be converted to GUID/local file identifier.
        /// This method provides the complete inverse of <see cref="CreateLoadableObjectId(GUID, FileIdentifierType, long)"/>,
        /// returning all three components that were used to construct the LoadableObjectId.
        /// </remarks>
        /// <param name="loadableObjectId">The LoadableObjectId to deconstruct.</param>
        /// <param name="guid">The GUID of the asset file containing the referenced object.</param>
        /// <param name="localId">The local file identifier of the object within the asset.</param>
        /// <param name="fileType">The file identifier type indicating whether this is a source asset, primary artifact, or non-asset reference.</param>
        /// <returns>
        /// True if the LoadableObjectId represents an asset reference and all components were successfully retrieved;
        /// false if the LoadableObjectId is a runtime handle.
        /// </returns>
        /// <seealso cref="CreateLoadableObjectId(GUID, FileIdentifierType, long)"/>
        /// <seealso cref="AssetDatabase.TryGetGUIDAndLocalFileIdentifier"/>
        public static bool TryDeconstructLoadableObjectId(LoadableObjectId loadableObjectId, out GUID guid, out long localId, out FileIdentifierType fileType)
        {
            // If this is a runtime handle (has ObjectIdHash), we cannot extract GUID/lfid/type
            if (loadableObjectId.m_ObjectIdHash.isValid)
            {
                guid = new GUID();
                localId = 0;
                fileType = FileIdentifierType.NonAsset;
                return false;
            }

            guid = loadableObjectId.m_GUID;
            localId = loadableObjectId.m_LocalIdentifierInFile;
            fileType = loadableObjectId.m_FileIdentifierType;
            return true;
        }

        /// <summary>
        /// Retrieve the Object referenced by a LoadableObjectId.
        /// </summary>
        /// <remarks>
        /// This method returns the object in the Editor source asset, not the object in the build output.
        /// </remarks>
        /// <param name="loadableObjectId">The LoadableObjectId to convert.</param>
        /// <returns>
        /// The Unity Object referenced by the LoadableObjectId, or null if the reference is invalid.
        /// </returns>
        public static Object LoadableObjectIdToObject(LoadableObjectId loadableObjectId)
        {
            return LoadableObjectIdToObjectInternal(loadableObjectId);
        }

        /// <summary>
        /// Converts a Unity Object to a LoadableObjectId that can be used for on-demand loading.
        /// </summary>
        /// <param name="obj">
        /// A Unity Object that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be referenced by
        /// LoadableObjectId.
        /// </param>
        /// <returns>
        /// A LoadableObjectId that points to the specified object, or an empty LoadableObjectId if obj is null
        /// or if it does not belong to a persisted asset.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the object is non-persistent (cannot be serialized to disk).
        /// </exception>
        public static LoadableObjectId ObjectToLoadableObjectId(Object obj)
        {
            return ObjectToLoadableObjectIdInternal(obj);
        }

        /// <summary>
        /// Creates a LoadableObjectId from an EntityId.
        /// </summary>
        /// <param name="entityID">
        /// The EntityId of a Unity Object that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be
        /// referenced by LoadableObjectId.
        /// </param>
        /// <returns>A LoadableObjectId that points to the object with the specified EntityId.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the EntityId is invalid or if the object is non-persistent.
        /// </exception>
        public static LoadableObjectId CreateLoadableObjectId(EntityId entityID)
        {
            return EntityIDToLoadableObjectIdInternal(entityID);
        }

        /// <summary>
        /// Creates a LoadableObjectId from its component parts.
        /// </summary>
        /// <param name="guid">The GUID of the asset file containing the object.</param>
        /// <param name="type">The identifier of the reference type.</param>
        /// <param name="fileID">The local file ID of the object within the asset.</param>
        /// <returns>A LoadableObjectId constructed from the specified components.</returns>
        /// <seealso cref="GlobalObjectId"/>
        public static LoadableObjectId CreateLoadableObjectId(GUID guid, FileIdentifierType type, long fileID)
        {
            var loadableObjectId = new LoadableObjectId();
            loadableObjectId.m_GUID = guid;
            loadableObjectId.m_FileIdentifierType = type;
            loadableObjectId.m_LocalIdentifierInFile = fileID;
            loadableObjectId.m_ObjectIdHash = new Hash128();
            return loadableObjectId;
        }

        /// <summary>
        /// Draws an IMGUI field for a LoadableObjectId property. Used by LoadableObjectIdDrawer and LoadableDrawer for nested m_LoadableObjectId.
        /// </summary>
        /// <param name="position">Rect to draw the field in.</param>
        /// <param name="property">SerializedProperty of type LoadableObjectId.</param>
        /// <param name="label">Label for the field.</param>
        /// <param name="objectType">Type of Unity Object that can be assigned (e.g. Texture2D, or UnityEngine.Object for any).</param>
        [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
        internal static void DrawLoadableObjectIdField(Rect position, SerializedProperty property, GUIContent label, Type objectType)
        {
            EditorGUI.BeginProperty(position, label, property);
            var loadableRef = property.loadableObjectIdValue;
            var currentObj = LoadableObjectIdToObject(loadableRef);

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            int id = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.DoLoadableObjectField(fieldRect, fieldRect, id, currentObj, null, objectType, property, LoadableObjectIdFieldValidator, true);
            
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    var newRef = ObjectToLoadableObjectId(newObj);
                    if (newObj != null && !newRef.isValid)
                        Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableObjectId."));
                    else
                        property.loadableObjectIdValue = newRef;
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}"), e.Message));
                }
            }
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Allows all <see cref="Object"/> types except for <see cref="SceneAsset"/>.
        /// </summary>
        internal static Object LoadableObjectIdFieldValidator(Object[] references, Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            Object validated = EditorGUI.ValidateObjectFieldAssignment(references, objType, property, options);
            if (validated != null && validated is SceneAsset)
                return null;
            return validated;
        }

        [FreeFunction("LoadableObjectIdUtility::LoadableObjectIdToObject", ThrowsException = true)]
        private static extern Object LoadableObjectIdToObjectInternal(LoadableObjectId loadableObjectId);

        [FreeFunction("LoadableObjectIdUtility::ObjectToLoadableObjectId", ThrowsException = true)]
        private static extern LoadableObjectId ObjectToLoadableObjectIdInternal(Object obj);

        [FreeFunction("LoadableObjectIdUtility::EntityIDToLoadableObjectId", ThrowsException = true)]
        private static extern LoadableObjectId EntityIDToLoadableObjectIdInternal(EntityId instanceID);
    }
}
