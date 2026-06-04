// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;
using System.Text;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A common system to handle COPPA compliance configuration.
    /// </summary>
    internal class CoppaManager
    {
        const string k_CoppaTemplatePath = "UXML/ServicesWindow/Coppa.uxml";
        const string k_CoppaCommonStyleSheetPath = "StyleSheets/ServicesWindow/CoppaCommon.uss";
        const string k_CoppaDarkStyleSheetPath = "StyleSheets/ServicesWindow/CoppaDark.uss";
        const string k_CoppaLightStyleSheetPath = "StyleSheets/ServicesWindow/CoppaLight.uss";
        const string k_CoppaLearnMoreUrl = "https://docs.unity.com/ads/COPPACompliance.html";
        const string k_CoppaComplianceEditorConfigurationExceptionMessage = "Unexpected UnityConnect SetCOPPACompliance behavior. " +
            "Coppa compliance was saved successfully on the web dashboard but not within editor configurations. " +
            "Please try again to ensure you synchronize the state of the web dashboard with your application. " +
            "Or you may alternatively restart the editor to sync up with the web dashboard.";
        public const string CoppaComplianceChangedMessage = "COPPA compliance changed";
        const string k_CoppaUnexpectedSaveRequestBehaviorMessage = "Unexpected save request behavior.";
        const long k_HttpStatusNoContent = 204;
        const string k_Undefined = "Please select";
        const string k_Yes = "Yes";
        const string k_No = "No";
        const string k_CoppaCompliantJsonValue = "compliant";
        const string k_CoppaNotCompliantJsonValue = "not_compliant";

        internal const string coppaContainerName = "CoppaContainer";
        const string k_CoppaFieldName = "CoppaField";
        const string k_CoppaLearnLinkBtnName = "CoppaLearnLinkBtn";

        VisualElement m_CoppaContainer;

        public ChangeCallback changeCallback { private get; set; }
        public ExceptionCallback exceptionCallback { private get; set; }

        public VisualElement coppaContainer => m_CoppaContainer;

        private struct CoppaState
        {
            public bool isCompliant;
        }

        /// <summary>
        /// Configures a new Coppa manager to this within an existing EditorWindow
        /// </summary>
        /// <param name="rootVisualElement">visual element where the coppa content must be added</param>
        public CoppaManager(VisualElement rootVisualElement)
        {
            InitializeCoppaManager(rootVisualElement);
        }

        void InitializeCoppaManager(VisualElement rootVisualElement)
        {
            rootVisualElement.AddStyleSheetPath(k_CoppaCommonStyleSheetPath);
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? k_CoppaDarkStyleSheetPath : k_CoppaLightStyleSheetPath);
            var coppaTemplate = EditorGUIUtility.Load(k_CoppaTemplatePath) as VisualTreeAsset;
            rootVisualElement.Add(coppaTemplate.CloneTree().contentContainer);
            m_CoppaContainer = rootVisualElement.Q(coppaContainerName);
            var coppaField = BuildPopupField(m_CoppaContainer, k_CoppaFieldName);

            //Setup dashboard link
            var learnMoreClickable = new Clickable(() =>
            {
                Application.OpenURL(k_CoppaLearnMoreUrl);
            });
            m_CoppaContainer.Q(k_CoppaLearnLinkBtnName).AddManipulator(learnMoreClickable);

            var originalCoppaValue = UnityConnect.instance.GetProjectInfo().COPPA;
            var coppaChoicesList = new List<String>() { L10n.Tr(k_No), L10n.Tr(k_Yes) };
            if (originalCoppaValue == COPPACompliance.COPPAUndefined.ToCoppaCompliance())
            {
                coppaChoicesList.Insert(0, L10n.Tr(k_Undefined));
            }
            coppaField.choices = coppaChoicesList;
            SetCoppaFieldValue(originalCoppaValue, coppaField);

            coppaField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == GetFieldValueForCompliancy(UnityConnect.instance.GetProjectInfo().COPPA.ToCOPPACompliance()))
                {
                    return;
                }
                ApplyCoppaChange(coppaField);
            });
        }

        void ApplyCoppaChange(PopupField<string> coppaField)
        {
            var originalCoppaValue = UnityConnect.instance.GetProjectInfo().COPPA;
            var payloadValue = GetCompliancyJsonValueFromFieldValue(coppaField);
            var newCompliancyValue = GetCompliancyForFieldValue(coppaField);
            SetEnabledCoppaControls(coppaContainer, false);
            try
            {
                ServicesConfiguration.instance.RequestCurrentProjectCoppaApiUrl(projectCoppaApiUrl =>
                {
                    UnityWebRequest currentSaveRequest = null;
                    try
                    {
                        var payload = $"{{\"coppa\":\"{payloadValue}\"}}";
                        var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                        currentSaveRequest = new UnityWebRequest(projectCoppaApiUrl, UnityWebRequest.kHttpVerbPUT)
                        { uploadHandler = uploadHandler};
                        currentSaveRequest.suppressErrorsToConsole = true;
                        currentSaveRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                        currentSaveRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
                        var operation = currentSaveRequest.SendWebRequest();
                        operation.completed += _ =>
                        {
                            try
                            {
                                HandleSaveResponse(currentSaveRequest, coppaField, originalCoppaValue, newCompliancyValue);
                            }
                            finally
                            {
                                currentSaveRequest.Dispose();
                                SetEnabledCoppaControls(coppaContainer, true);
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        HandleApplyException(ex, originalCoppaValue, coppaField, currentSaveRequest);
                    }
                });
            }
            catch (Exception ex)
            {
                HandleApplyException(ex, originalCoppaValue, coppaField);
            }
        }

        void HandleApplyException(Exception ex, CoppaCompliance originalCoppaValue, PopupField<string> coppaField, UnityWebRequest request = null)
        {
            request?.Dispose();
            SetCoppaFieldValue(originalCoppaValue, coppaField);
            SetEnabledCoppaControls(coppaContainer, true);
            exceptionCallback?.Invoke(originalCoppaValue, ex);
        }

        void HandleSaveResponse(UnityWebRequest request, PopupField<string> coppaField, CoppaCompliance originalCoppaValue, COPPACompliance newCompliancyValue)
        {
            if (request.responseCode == k_HttpStatusNoContent)
            {
                try
                {
                    if (!UnityConnect.instance.SetCOPPACompliance(newCompliancyValue))
                    {
                        EditorAnalytics.SendCoppaComplianceEvent(new CoppaState() { isCompliant = newCompliancyValue == COPPACompliance.COPPACompliant });

                        SetCoppaFieldValue(originalCoppaValue, coppaField);
                        exceptionCallback?.Invoke(originalCoppaValue, new CoppaComplianceEditorConfigurationException(k_CoppaComplianceEditorConfigurationExceptionMessage));
                    }
                    else
                    {
                        originalCoppaValue = newCompliancyValue.ToCoppaCompliance();
                        SetCoppaFieldValue(originalCoppaValue, coppaField);
                        NotificationManager.instance.Publish(Notification.Topic.CoppaCompliance, Notification.Severity.Info,
                            L10n.Tr(CoppaComplianceChangedMessage));
                        changeCallback?.Invoke(originalCoppaValue);
                    }
                }
                catch (Exception ex)
                {
                    SetCoppaFieldValue(originalCoppaValue, coppaField);
                    exceptionCallback?.Invoke(originalCoppaValue, new CoppaComplianceEditorConfigurationException(k_CoppaComplianceEditorConfigurationExceptionMessage, ex));
                }
            }
            else
            {
                SetCoppaFieldValue(originalCoppaValue, coppaField);
                exceptionCallback?.Invoke(originalCoppaValue, new CoppaComplianceWebConfigurationException(L10n.Tr(k_CoppaUnexpectedSaveRequestBehaviorMessage))
                {
                    error = request.error,
                    method = request.method,
                    timeout = request.timeout,
                    url = request.url,
                    responseHeaders = request.GetResponseHeaders(),
                    responseCode = request.responseCode,
                    isHttpError = (request.result == UnityWebRequest.Result.ProtocolError),
                    isNetworkError = (request.result == UnityWebRequest.Result.ConnectionError),
                });
            }
        }

        static void SetEnabledCoppaControls(VisualElement coppaContainer, bool enable)
        {
            coppaContainer.Q(k_CoppaFieldName).SetEnabled(enable);
        }

        static void SetCoppaFieldValue(CoppaCompliance coppaCompliance, PopupField<String> coppaField)
        {
            coppaField.SetValueWithoutNotify(GetFieldValueForCompliancy(coppaCompliance.ToCOPPACompliance()));
        }

        static PopupField<string> BuildPopupField(VisualElement block, string anchorName)
        {
            var anchor = block.Q(anchorName);
            var anchorParent = anchor.parent;
            var anchorIndex = anchorParent.IndexOf(anchor);
            var popupField = new PopupField<string> { name = anchor.name };
            anchorParent.RemoveAt(anchorIndex);
            anchorParent.Insert(anchorIndex, popupField);
            return popupField;
        }

        static string GetFieldValueForCompliancy(COPPACompliance coppaCompliance)
        {
            switch (coppaCompliance)
            {
                case COPPACompliance.COPPACompliant:
                    return L10n.Tr(k_Yes);
                case COPPACompliance.COPPANotCompliant:
                    return L10n.Tr(k_No);
                default:
                    return L10n.Tr(k_Undefined);
            }
        }

        static COPPACompliance GetCompliancyForFieldValue(PopupField<string> coppaField)
        {
            if (coppaField.value == L10n.Tr(k_Yes))
            {
                return COPPACompliance.COPPACompliant;
            }
            if (coppaField.value == L10n.Tr(k_No))
            {
                return COPPACompliance.COPPANotCompliant;
            }
            return COPPACompliance.COPPAUndefined;
        }

        static string GetCompliancyJsonValueFromFieldValue(PopupField<string> coppaField)
        {
            return coppaField.value == L10n.Tr(k_Yes) ? k_CoppaCompliantJsonValue : k_CoppaNotCompliantJsonValue;
        }

        public delegate void ChangeCallback(CoppaCompliance coppaCompliance);

        public delegate void ExceptionCallback(CoppaCompliance coppaCompliance, Exception exception);
    }

    internal class CoppaComplianceEditorConfigurationException : Exception
    {
        public CoppaComplianceEditorConfigurationException(string message) : base(message)
        {
        }

        public CoppaComplianceEditorConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    internal class CoppaComplianceWebConfigurationException : Exception
    {
        public string error { get; set; }
        public string method { get; set; }
        public string url { get; set; }
        public long responseCode { get; set; }
        public bool isHttpError { get; set; }
        public bool isNetworkError { get; set; }
        public Dictionary<string, string> responseHeaders { get; set; }
        public int timeout { get; set; }

        public CoppaComplianceWebConfigurationException(string message) : base(message)
        {
        }
    }
}
