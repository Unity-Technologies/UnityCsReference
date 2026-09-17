// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Editor.Analytics;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "The base EditorWindow constructor schedules a delayed call; this window is recreated on code reload")]
    class MultiplayerCenterWindow : EditorWindow, IDataSourceViewHashProvider
    {
        const string k_WindowTemplatePath = "Multiplayer/MultiplayerCenter/UI/MultiplayerCenterWindow.uxml";
        const string k_CommonSkinPath = "Multiplayer/MultiplayerCenter/UI/common.uss";
        const string k_GameGenresPath = "Multiplayer/MultiplayerCenter/GameGenre.asset";
        const string k_CategoriesPath = "Multiplayer/MultiplayerCenter/Categories.asset";

        VisualTreeAsset m_WindowTemplate;
        GameGenreList m_GameGenres;
        CategoriesDescription m_CategoriesDescription;

        /// <summary>
        /// Loads a resource from the editor's default resources on first access and caches it.
        /// </summary>
        internal VisualTreeAsset WindowTemplate => Resolve(ref m_WindowTemplate, k_WindowTemplatePath);

        /// <inheritdoc cref="WindowTemplate" />
        /// <remarks>
        /// <c>[CreateProperty]</c> so the UXML binds through this accessor rather than the
        /// backing field: a <c>data-source-path</c> naming <c>m_GameGenres</c> reads the field
        /// directly, which is still null until something touches the property.
        /// </remarks>
        [CreateProperty]
        internal GameGenreList GameGenres => Resolve(ref m_GameGenres, k_GameGenresPath);

        /// <inheritdoc cref="WindowTemplate" />
        /// <remarks>
        /// <c>[CreateProperty]</c> so the UXML binds through this accessor rather than the
        /// backing field, same as <see cref="GameGenres"/>.
        /// </remarks>
        [CreateProperty]
        internal CategoriesDescription Categories => Resolve(ref m_CategoriesDescription, k_CategoriesPath);

        static T Resolve<T>(ref T cached, string resourceName) where T : UnityEngine.Object
        {
            if (cached == null)
                cached = EditorGUIUtility.LoadRequired(resourceName) as T;
            return cached;
        }

        /// <summary>
        /// Adds <paramref name="commonStyleSheetPath"/> along with its <c>_dark</c> or <c>_light</c>
        /// variant, matching the editor's current skin, to <paramref name="element"/>.
        /// </summary>
        static void AddThemedStyleSheets(VisualElement element, string commonStyleSheetPath)
        {
            var extension = Path.GetExtension(commonStyleSheetPath);
            var baseName = commonStyleSheetPath.Substring(0, commonStyleSheetPath.Length - extension.Length);
            var variantSuffix = EditorGUIUtility.isProSkin ? "_dark" : "_light";
            element.styleSheets.Add(EditorGUIUtility.LoadRequired(baseName + variantSuffix + extension) as StyleSheet);
            element.styleSheets.Add(EditorGUIUtility.LoadRequired(commonStyleSheetPath) as StyleSheet);
        }

        /// <summary>
        /// Menu item declaration to open the window.
        /// </summary>
        [MenuItem("Window/Multiplayer/Multiplayer Center")]
        static void OpenWindow()
        {
            var window = GetWindow<MultiplayerCenterWindow>(false, "Multiplayer Center", true);
            window.minSize = new Vector2(600, 400);
            WindowChangedEvent.Send(new WindowChangedData(WindowTransition.Opened));
        }

        /// <summary>
        /// This is only for the window to survive an assembly reload
        /// and not reset the current m_DisplayQuestion state in its OnEnable.
        /// When users have already answered the question and would go back to the question page,
        /// if an assembly reload happens at this point, OnEnable will be called
        /// and we would reset the m_DisplayQuestion value, effectively leaving that panel.
        /// </summary>
        /// <remarks>
        /// This value is not saved outside the window instance context so that closing and
        /// reopening the window after having answered the question will bring users back
        /// to the quickstart content.
        /// </remarks>
        bool m_SetupDone;

        /// <summary>
        /// Which genre is currently selected based on the index of the <see cref="GameGenreList"/>.
        /// </summary>
        /// <remarks>
        /// This information is directly saved into the ProjectSettings.
        /// </remarks>
        int SelectedGenre
        {
            get => MultiplayerCenterSettings.instance.SelectedGenre;
            set
            {
                MultiplayerCenterSettings.instance.SelectedGenre = value;
                MultiplayerCenterSettings.instance.Save();
            }
        }

        /// <summary>
        /// Global state of the UI.
        /// </summary>
        /// <remarks>
        /// This is bound to the <see cref="QuestionModeToggle.QuestionMode"/> property in the UXML.
        /// </remarks>
        [CreateProperty] bool m_DisplayQuestion = true;

        /// <summary>
        /// Title label of the banner that changes based on the question and its answer.
        /// </summary>
        /// <remarks>
        /// This is bound to the "title" Label.text property in the UXML.
        /// </remarks>
        [CreateProperty]
        string TitleLabel => m_DisplayQuestion
            ? L10n.Tr("What kind of multiplayer game do you want to build?", null)
            : GameGenres.Descriptions[SelectedGenre].Name;

        /// <summary>
        /// Selected GameGenre in the ListView when <see cref="m_DisplayQuestion" /> is true.
        /// </summary>
        /// <remarks>
        /// This is bound to the "question-list" ListView.selectedIndex property in the UXML.
        /// </remarks>
        [CreateProperty] int m_ListViewSelectedGameGenre;

        /// <summary>
        /// Description of the selected GameGenre.
        /// </summary>
        /// <remarks>
        /// This is bound to the "question-description" VisualElement.dataSource property in the UXML.
        /// </remarks>
        [CreateProperty]
        GameGenreDescription SelectedGameGenre => GameGenres.Descriptions[m_ListViewSelectedGameGenre];

        /// <summary>
        /// Currently selected category.
        /// </summary>
        /// <remarks>
        /// This information is directly saved into the EditorPrefs because it is local to each user.
        /// It could be moved to be saved per-user-per-project in the future. <br />
        /// This is bound to the "categories" ListView.selectedIndex property in the UXML.
        /// </remarks>
        [CreateProperty]
        int SelectedCategory
        {
            get => m_SelectedCategory;
            set
            {
                if (value != m_SelectedCategory)
                {
                    m_SelectedCategory = value;
                    EditorPrefs.SetInt("com.unity.multiplayer.center.selectedCategory", value);
                }
            }
        }

        int m_SelectedCategory;

        /// <summary>
        /// Currently selected category type.
        /// </summary>
        /// <remarks>
        /// This is bound to the "category-content" <see cref="CategoriesContainer.DisplayedCategory"/> property in the UXML.
        /// </remarks>
        [CreateProperty]
        OnboardingSectionCategory SelectedCategoryType
        {
            get
            {
                var categories = Categories.FilteredCategories;
                if (SelectedCategory < 0 || SelectedCategory >= categories.Count)
                    return default;
                return categories[SelectedCategory].CategoryType;
            }
        }

        void OnDestroy()
        {
            WindowChangedEvent.Send(new WindowChangedData(WindowTransition.Closed));
        }

        void OnEnable()
        {

            if (!m_SetupDone)
            {
                m_DisplayQuestion = SelectedGenre == -1;
                m_ListViewSelectedGameGenre = SelectedGenre < 0 ? 0 : SelectedGenre;
                m_SelectedCategory = EditorPrefs.GetInt("com.unity.multiplayer.center.selectedCategory", 0);
                m_SetupDone = true;
            }
        }

        /// <summary>
        /// Called by Unity automatically when the window is opened or the UI needs a full rebuild.
        /// </summary>
        void CreateGUI()
        {
            WindowTemplate.CloneTree(rootVisualElement);
            rootVisualElement.viewDataKey = GetType().Name;
            AddThemedStyleSheets(rootVisualElement, k_CommonSkinPath);
            rootVisualElement.dataSource = this;

            var questionList = rootVisualElement.Q<ListView>("question-list");
            questionList.selectionChanged += OnQuestionListSelectionChanged;

            var answerButton = rootVisualElement.Q<Button>("answer-button");
            var backButton = rootVisualElement.Q<Button>("back-button");

            backButton.clicked += () =>
            {
                m_DisplayQuestion = true;
                SelectedGenre = -1;
            };

            answerButton.clicked += () =>
            {
                // Only save the genre and reset the category if it has changed.
                if (SelectedGenre != m_ListViewSelectedGameGenre)
                {
                    SelectedCategory = 0;
                    SelectedGenre = m_ListViewSelectedGameGenre;
                }

                m_DisplayQuestion = false;

                var genreName = SelectedGameGenre.Name;
                GenreSelectedEvent.Send(new GenreSelectedData { gameGenre = genreName });
            };
        }

        void OnQuestionListSelectionChanged(IEnumerable<object> objects)
        {
            var questionDescription = rootVisualElement.Q<VisualElement>("question-description");

            using var enumerator = objects.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            var genre = enumerator.Current as GameGenreDescription?;
            var header = questionDescription.Q<VisualElement>("genre-header-container");
            header.Clear();

            var genreHeader = genre?.Header.Instantiate();
            genreHeader?.SetBinding(nameof(genreHeader.dataSource), new DataBinding() { dataSource = genre, });

            header.Add(genreHeader);
        }


        /// <summary>
        /// This is testing on the 3 fields with [CreateProperty] that have an impact on the UI display.
        /// </summary>
        /// <returns>A hash of the <see cref="m_DisplayQuestion"/>,
        /// <see cref="m_ListViewSelectedGameGenre"/>,
        /// and <see cref="SelectedCategory"/> values.</returns>
        public long GetViewHashCode()
        {
            var hash = Hash128.Compute(m_DisplayQuestion.GetHashCode());
            hash.Append(m_ListViewSelectedGameGenre);
            hash.Append(SelectedCategory);
            return hash.GetHashCode();
        }
    }
}
