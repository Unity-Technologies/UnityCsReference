// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Center.Editor
{
    using SectionCategoryToSectionIdToSectionType = Dictionary<OnboardingSectionCategory, Dictionary<string, Type>>;

    using SectionCategoryToSectionList = Dictionary<OnboardingSectionCategory, Type[]>;

    /// <summary>
    /// Stores the available section types in a serializable way, but for comparison purposes only.
    /// Only the assembly qualified names are serialized in a sorted array.
    /// </summary>
    internal class AvailableSectionTypes
    {
        readonly SectionCategoryToSectionList m_SectionMapping;

        public AvailableSectionTypes(SectionCategoryToSectionList sectionTypes)
        {
            m_SectionMapping = sectionTypes;
        }

        public bool TryGetValue(OnboardingSectionCategory category, out Type[] sectionTypes)
        {
            return m_SectionMapping.TryGetValue(category, out sectionTypes);
        }
    }

    [Obsolete("To be removed when IOnboardingSection is removed.")]
    internal static class SectionsFinder
    {
        public static AvailableSectionTypes FindSectionTypes()
        {
            var dico = GetSectionCategoryToSectionIdToSectionType();
            return SortSectionsByOrder(dico);
        }

        static AvailableSectionTypes SortSectionsByOrder(SectionCategoryToSectionIdToSectionType dico)
        {
            var result = new SectionCategoryToSectionList();
            foreach (var (category, idToTypeDictionary) in dico)
            {
                var typeAttributeList = GetOnboardingTypeAttributeList(idToTypeDictionary);
                typeAttributeList.Sort((typeA, typeB) => typeA.Attribute.Order.CompareTo(typeB.Attribute.Order));
                result[category] = GetOnboardingTypeArray(typeAttributeList);
            }

            return new AvailableSectionTypes(result);
        }

        static Type[] GetOnboardingTypeArray(List<(Type Type, OnboardingSectionAttribute Attribute)> typeAttributeList)
        {
            var typeArray = new Type[typeAttributeList.Count];
            for (var i = 0; i < typeAttributeList.Count; i++)
            {
                typeArray[i] = typeAttributeList[i].Type;
            }

            return typeArray;
        }

        static List<(Type Type, OnboardingSectionAttribute Attribute)> GetOnboardingTypeAttributeList(Dictionary<string, Type> idToTypeDictionary)
        {
            var typeAttributeList = new List<(Type Type, OnboardingSectionAttribute Attribute)>(idToTypeDictionary.Count);

            foreach (var (_, type) in idToTypeDictionary)
            {
                var attribute = type.GetCustomAttribute<OnboardingSectionAttribute>();
                if (attribute != null)
                {
                    typeAttributeList.Add((type, attribute));
                }
            }

            return typeAttributeList;
        }

        static SectionCategoryToSectionIdToSectionType GetSectionCategoryToSectionIdToSectionType()
        {
            var dico = new Dictionary<OnboardingSectionCategory, Dictionary<string, Type>>();
            foreach (var sectionType in TypeCache.GetTypesDerivedFrom<IOnboardingSection>())
            {
                if (sectionType.IsAbstract) continue;

                // check if type has default constructor
                if (sectionType.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogWarning($"Onboarding section type {sectionType} does not have a default constructor and will be ignored.");
                    continue;
                }

                var sectionAttribute = sectionType.GetCustomAttribute<OnboardingSectionAttribute>();
                if(sectionAttribute == null)
                    continue;

                if (!IsDisplayConditionFulfilledForSection(sectionAttribute))
                        continue;

                if (!dico.ContainsKey(sectionAttribute.Category))
                    dico[sectionAttribute.Category] = new Dictionary<string, Type>();

                if (dico[sectionAttribute.Category].TryGetValue(sectionAttribute.Id, out var existing))
                {
                    var existingAttr = existing.GetCustomAttribute<OnboardingSectionAttribute>();

                    if (existingAttr!= null && existingAttr.Id == sectionAttribute.Id
                        && sectionAttribute.Priority > existingAttr.Priority)
                    {
                        dico[sectionAttribute.Category][sectionAttribute.Id] = sectionType;
                    }
                }
                else
                {
                    dico[sectionAttribute.Category][sectionAttribute.Id] = sectionType;
                }
            }

            return dico;
        }

        static bool IsDisplayConditionFulfilledForSection(OnboardingSectionAttribute attribute)
        {
            return attribute.DisplayCondition switch
            {
                DisplayCondition.None => true,
                DisplayCondition.PackageInstalled when string.IsNullOrEmpty(attribute.TargetPackageId)
                    => throw new ArgumentException("PackageInstalled condition requires a target package id"),
                DisplayCondition.PackageInstalled
                    => UnityEditor.PackageManager.PackageInfo.FindForPackageName(attribute.TargetPackageId) != null,
                DisplayCondition.NoPackageInstalled => true,
                _ => throw new NotImplementedException($"Unknown display condition: {attribute.DisplayCondition}")
            };
        }
    }
}
