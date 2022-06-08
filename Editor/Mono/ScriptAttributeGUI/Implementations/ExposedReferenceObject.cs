// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class ExposedReferenceObject
    {
        internal string exposedPropertyNameString { get; set; }
        internal Object currentReferenceValue { get; set; }
        internal SerializedProperty exposedPropertyDefault { get; set; }
        internal SerializedProperty exposedPropertyName { get; set; }
        internal BaseExposedPropertyDrawer.ExposedPropertyMode propertyMode { get; set; }
        internal IExposedPropertyTable exposedPropertyTable { get; set; }

        internal BaseExposedPropertyDrawer.OverrideState currentOverrideState
        {
            get => m_CurrentOverrideState;
            set => m_CurrentOverrideState = value;
        }

        BaseExposedPropertyDrawer.OverrideState m_CurrentOverrideState;

        internal ExposedReferenceObject(SerializedProperty property)
        {
            exposedPropertyDefault = property.FindPropertyRelative("defaultValue");
            exposedPropertyName = property.FindPropertyRelative("exposedName");
            exposedPropertyNameString = exposedPropertyName.stringValue;
            propertyMode = BaseExposedPropertyDrawer.GetExposedPropertyMode(exposedPropertyNameString);
            exposedPropertyTable = GetExposedPropertyTable(property);
            currentOverrideState = BaseExposedPropertyDrawer.OverrideState.DefaultValue;
            currentReferenceValue = Resolve(out m_CurrentOverrideState);
        }

        IExposedPropertyTable GetExposedPropertyTable(SerializedProperty property)
        {
            var t = property.serializedObject.context;
            return t as IExposedPropertyTable;
        }

        internal Object Resolve(out BaseExposedPropertyDrawer.OverrideState currentOverrideState)
        {
            PropertyName exposedPropertyName = new PropertyName(exposedPropertyNameString);
            Object defaultValue = exposedPropertyDefault.objectReferenceValue;
            Object objReference = null;
            var propertyIsNamed = !PropertyName.IsNullOrEmpty(exposedPropertyName);
            currentOverrideState = BaseExposedPropertyDrawer.OverrideState.DefaultValue;

            if (exposedPropertyTable != null)
            {
                objReference =
                    exposedPropertyTable.GetReferenceValue(exposedPropertyName, out var propertyFoundInTable);

                if (propertyFoundInTable)
                    currentOverrideState = BaseExposedPropertyDrawer.OverrideState.Overridden;
                else if (propertyIsNamed)
                    currentOverrideState = BaseExposedPropertyDrawer.OverrideState.MissingOverride;
            }

            return currentOverrideState == BaseExposedPropertyDrawer.OverrideState.Overridden ? objReference : defaultValue;
        }
    }
}
