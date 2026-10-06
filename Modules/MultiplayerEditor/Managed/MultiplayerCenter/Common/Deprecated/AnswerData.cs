// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Center.Common
{
    /// <summary>
    /// Stores what the user answered in the GameSpecs questionnaire. The Preset is not included here.
    /// </summary>
    [Obsolete("Use MultiplayerCenterSettings for MultiplayerCenter project-wide settings")]
    [Serializable]
    internal class AnswerData
    {
        /// <summary>
        /// The list of answers the user has given so far.
        /// </summary>
        public List<AnsweredQuestion> Answers = new();

        /// <summary>
        /// Makes a deep copy of the object.
        /// </summary>
        /// <returns>The clone</returns>
        public AnswerData Clone()
        {
            return JsonUtility.FromJson(JsonUtility.ToJson(this), typeof(AnswerData)) as AnswerData;
        }
    }

    /// <summary>
    /// Answer to a single game spec question.
    /// </summary>
    [Serializable]
    [Obsolete("Use MultiplayerCenterSettings for MultiplayerCenter project-wide settings")]
    internal class AnsweredQuestion
    {
        /// <summary>
        /// The question identifier as defined in the game spec questionnaire.
        /// </summary>
        public string QuestionId;

        /// <summary>
        /// The answers selected by the user (most often, it contains only one element).
        /// </summary>
        public List<string> Answers;
    }
}
