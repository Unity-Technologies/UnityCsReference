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
    /// Editor utilities for creating and converting <see cref="LoadableObjectId"/> values when authoring content.
    /// </summary>
    /// <remarks>
    /// <see cref="LoadableObjectId"/> is the low-level reference type used by `Loadable{T}` for on-demand object loading.
    /// These methods convert between live <see cref="Object"/> instances and serialized loadable object IDs for content directory builds.
    /// </remarks>
    /// <example>
    /// <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/LoadableObjectIdEditorUtility_Example.cs"/>
    /// </example>
    [NativeHeader("Editor/Src/Utility/LoadableObjectIdEditorUtility.bindings.h")]
    [VisibleToOtherModules]
    public static class LoadableObjectIdEditorUtility
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
        /// This method will return <c>false</c> if the <see cref="LoadableObjectId"/> contains a runtime handle.
        /// Runtime handles are only interpretable by the <see cref="ContentLoadManager"/> and cannot be converted to GUID/local file identifier.
        /// </remarks>
        /// <param name="loadableObjectId">The <see cref="LoadableObjectId"/> to deconstruct.</param>
        /// <param name="guid">The GUID of the asset file containing the referenced object.</param>
        /// <param name="localId">The local file identifier of the object within the asset.</param>
        /// <param name="fileType">The file identifier type indicating whether this is a source asset, primary artifact, or non-asset reference.</param>
        /// <returns>
        /// <c>true</c> if the LoadableObjectId represents an asset reference and all components were successfully retrieved;
        /// <c>false</c> if the LoadableObjectId is a runtime handle.
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
        /// Retrieve the <see cref="Object"/> referenced by a LoadableObjectId.
        /// </summary>
        /// <remarks>
        /// This method returns the object in the Editor source asset, not the object in the build output.
        /// </remarks>
        /// <param name="loadableObjectId">The LoadableObjectId to convert.</param>
        /// <returns>
        /// The Unity <see cref="Object"/> referenced by the <see cref="LoadableObjectId"/>, or null if the reference is invalid.
        /// </returns>
        public static Object LoadableObjectIdToObject(LoadableObjectId loadableObjectId)
        {
            return LoadableObjectIdToObjectInternal(loadableObjectId);
        }

        /// <summary>
        /// Creates a <see cref="LoadableObjectId"/> from a <see cref="Object"/> that can be used for on-demand loading.
        /// </summary>
        /// <param name="obj">
        /// A Unity <see cref="Object"/> that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be referenced by
        /// <see cref="LoadableObjectId"/>.
        /// </param>
        /// <returns>
        /// A LoadableObjectId that points to the specified object, or an empty LoadableObjectId if obj is null
        /// or if it does not belong to a persisted asset.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the object is non-persistent (cannot be serialized to disk).
        /// </exception>
        public static LoadableObjectId CreateLoadableObjectId(Object obj)
        {
            return ObjectToLoadableObjectIdInternal(obj);
        }

        /// <summary>
        /// Creates a <see cref="LoadableObjectId"/> from an <see cref="EntityId"/>.
        /// </summary>
        /// <param name="entityId">
        /// The <see cref="EntityId"/> of a Unity <see cref="Object"/> that has been serialized to disk as part of an Asset. Objects inside Scenes cannot be referenced by <see cref="LoadableObjectId"/>.
        /// </param>
        /// <returns>A <see cref="LoadableObjectId"/> that points to the specified object.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the EntityId is invalid or if the object is non-persistent.
        /// </exception>
        public static LoadableObjectId CreateLoadableObjectId(EntityId entityId)
        {
            return EntityIdToLoadableObjectIdInternal(entityId);
        }

        /// <summary>
        /// Creates a LoadableObjectId from its component parts.
        /// </summary>
        /// <remarks>
        /// This method is the inverse of <see cref="TryDeconstructLoadableObjectId"/>: it reconstructs a
        /// <see cref="LoadableObjectId"/> from the GUID, file identifier type, and local file identifier.
        /// Use <see cref="CreateLoadableObjectId(Object)"/> or <see cref="CreateLoadableObjectId(EntityId)"/> when
        /// constructing a reference from a live <see cref="Object"/>; use this overload only when the
        /// component parts are already known (for example, when persisting and restoring a reference in tooling).
        /// </remarks>
        /// <param name="guid">The GUID of the asset file containing the object.</param>
        /// <param name="fileType">The <see cref="FileIdentifierType"/> indicating whether this is a source asset, primary artifact, or non-asset reference.</param>
        /// <param name="localId">The local file identifier of the object within the asset.</param>
        /// <returns>A <see cref="LoadableObjectId"/> constructed from the specified components.</returns>
        /// <seealso cref="TryDeconstructLoadableObjectId"/>
        /// <seealso cref="GlobalObjectId"/>
        public static LoadableObjectId CreateLoadableObjectId(GUID guid, FileIdentifierType fileType, long localId)
        {
            var loadableObjectId = new LoadableObjectId();
            loadableObjectId.m_GUID = guid;
            loadableObjectId.m_FileIdentifierType = fileType;
            loadableObjectId.m_LocalIdentifierInFile = localId;
            loadableObjectId.m_ObjectIdHash = new Hash128();
            return loadableObjectId;
        }

        /// <summary>
        /// Draws an IMGUI loadable object field (with striped background).
        /// When <paramref name="property"/> is non-null the current object is resolved from it;
        /// otherwise the field draws as empty (null value).
        /// </summary>
        /// <param name="position">Rect to draw the field in.</param>
        /// <param name="property">SerializedProperty of type LoadableObjectId, or null when no backing property exists (e.g. null managed reference).</param>
        /// <param name="label">Label for the field.</param>
        /// <param name="objectType">Type of Unity Object that can be assigned (e.g. Texture2D, or UnityEngine.Object for any).</param>
        /// <returns>The object selected by the user, or null.</returns>
        [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
        internal static Object DrawLoadableObjectIdField(Rect position, SerializedProperty property, GUIContent label, Type objectType)
        {
            var currentObj = property != null
                ? LoadableObjectIdToObject(property.loadableObjectIdValue)
                : null;

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            int id = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);
            return EditorGUI.DoLoadableObjectField(fieldRect, fieldRect, id, currentObj, null, objectType, property, LoadableObjectIdFieldValidator, true);
        }

        /// <summary>
        /// Validates a user-selected object and writes the resulting LoadableObjectId
        /// back to the given <paramref name="property"/>. Logs a warning if the object
        /// cannot be converted.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
        internal static void ApplyLoadableObjectIdChange(SerializedProperty property, Object newObj)
        {
            try
            {
                var newRef = CreateLoadableObjectId(newObj);
                if (newObj != null && !newRef.IsValid)
                    Debug.LogWarning(L10n.Tr("The selected object cannot be used as a LoadableObjectId."), newObj);
                else
                    property.loadableObjectIdValue = newRef;
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning(string.Format(L10n.Tr("The selected object cannot be used as a LoadableObjectId: {0}"), e.Message), newObj);
            }
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

        [FreeFunction("LoadableObjectIdUtility::EntityIdToLoadableObjectId", ThrowsException = true)]
        private static extern LoadableObjectId EntityIdToLoadableObjectIdInternal(EntityId instanceID);
    }
}
