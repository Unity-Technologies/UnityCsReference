// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.VectorGraphicsModule")]
    internal partial class SpriteUtilityWindow
    {
        protected class Styles
        {
            public static readonly GUIContent openSpriteEditor = L10n.TextContent("Open Sprite Editor", null, null, null);
            public static readonly GUIContent install2DPackage = L10n.TextContent("Install 2D Sprite Package", null, null, null);
            public static readonly GUIContent failedToInstall2DPackageTitle = L10n.TextContent("Installation Failed", null, null, null);
            public static readonly GUIContent failedToInstall2DPackageContent = L10n.TextContent("Failed to install package com.unity.2d.sprite.\nErrorCode: {0}\nMessage: {1}", null, null, null);
            public static readonly GUIContent install2DPackageReason = L10n.TextContent("The Sprite Editor window is not available because the 2D Sprite package is not installed. Click on the 'Install 2D Sprite Package' button to install the package to edit Sprites in Sprite Editor window.", null, null, null);
            public static readonly GUIContent okText = L10n.TextContent("OK", null, null, null);
        }

        [AutoStaticsCleanupOnCodeReload]
        static LaunchSpriteEditorWindowAfterDomainReload s_LaunchSpriteEditorWindowAfterDomainReload;

        internal static bool DoOpenSpriteEditorWindowUI(bool enableOpenSpriteEditorButton, bool enableInstallButton = true)
        {
            var buttonText = showSpriteEditorWindow == null ? Styles.install2DPackage : Styles.openSpriteEditor;
            GUILayout.BeginVertical();
            var clicked = false;
            using (new EditorGUI.DisabledScope((showSpriteEditorWindow != null && !enableOpenSpriteEditorButton) ||
                                                (showSpriteEditorWindow == null && !enableInstallButton)))
            {
                clicked = GUILayout.Button(buttonText);
            }

            if (showSpriteEditorWindow == null)
                EditorGUILayout.HelpBox(Styles.install2DPackageReason.text, MessageType.Info, true);
            GUILayout.EndVertical();
            return clicked;
        }

        [Obsolete("Use SpriteUtility.SetShowSpriteEditorWindowWithObject instead")]
        internal static void SetShowSpriteEditorWindow(Func<bool> spriteEditorWindow)
        {
            if (spriteEditorWindow != null)
                showSpriteEditorWindow = (x) => spriteEditorWindow();
        }

        internal static void SetShowSpriteEditorWindowWithObject(Func<UnityEngine.Object, bool> spriteEditorWindow)
        {
            if (spriteEditorWindow != null)
                showSpriteEditorWindow = spriteEditorWindow;
        }

        internal static void SetApplySpriteEditorWindow(Action action)
        {
            if (action != null)
                applySpriteEditorWindow = action;
            else
                applySpriteEditorWindow = () => {};
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.VectorGraphicsModule")]
        internal static bool ShowSpriteEditorWindow(UnityEngine.Object obj = null)
        {
            if (showSpriteEditorWindow == null)
            {
                var installSuccess = InstallSpritePackage();
                if (installSuccess)
                {
                    s_LaunchSpriteEditorWindowAfterDomainReload = ScriptableObject.CreateInstance<LaunchSpriteEditorWindowAfterDomainReload>();
                    s_LaunchSpriteEditorWindowAfterDomainReload.selectedObject = obj;
                }
                return installSuccess;
            }

            return showSpriteEditorWindow(obj != null ? obj : Selection.activeObject);
        }


        [AutoStaticsCleanupOnCodeReload]
        static Func<UnityEngine.Object, bool> showSpriteEditorWindow = null;

        static bool InstallSpritePackage()
        {
            if(s_LaunchSpriteEditorWindowAfterDomainReload != null)
                ScriptableObject.DestroyImmediate(s_LaunchSpriteEditorWindowAfterDomainReload);

            var addRequest = PackageManager.Client.Add("com.unity.2d.sprite");
            while (!addRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);

            if (addRequest.Status == PackageManager.StatusCode.Failure)
            {
                var message = String.Format(Styles.failedToInstall2DPackageContent.text, addRequest.Error.errorCode, addRequest.Error.message);
                EditorUtility.DisplayDialog(Styles.failedToInstall2DPackageTitle.text, message, Styles.okText.text);
            }

            return addRequest.Status == PackageManager.StatusCode.Success;
        }

        internal static void ApplySpriteEditorWindow()
        {
            applySpriteEditorWindow();
        }

        [AutoStaticsCleanupOnCodeReload]
        static Action applySpriteEditorWindow = () => {};
    } // class

    internal class LaunchSpriteEditorWindowAfterDomainReload : ScriptableObject
    {
        [SerializeField]
        UnityEngine.Object m_SelectedObject;

        public UnityEngine.Object selectedObject
        {
            get => m_SelectedObject;
            set => m_SelectedObject = value;
        }

        public LaunchSpriteEditorWindowAfterDomainReload()
        {
            AssemblyReloadEvents.afterAssemblyReload += WaitForEditorApplicaionUpdate;
        }

        void WaitForEditorApplicaionUpdate()
        {
            AssemblyReloadEvents.afterAssemblyReload -= WaitForEditorApplicaionUpdate;
            // Do this to ensure asset database has finish importing the assets needed by Sprite Editor Window
            EditorApplication.update += OpenSpriteEditor;
        }

        void OpenSpriteEditor()
        {
            EditorApplication.update -= OpenSpriteEditor;
            try
            {
                SpriteUtilityWindow.ShowSpriteEditorWindow(selectedObject);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                DestroyImmediate(this);
            }
        }
    }
}
