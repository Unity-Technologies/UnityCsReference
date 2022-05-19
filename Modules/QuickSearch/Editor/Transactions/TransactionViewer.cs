// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define USE_TRANSACTION_VIEWER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class TransactionViewer : EditorWindow
    {
        const int k_TransactionRowHeight = 16;
        const string k_DateLabelName = "date_label";
        const string k_PathLabelName = "path_label";
        const string k_StateLabelName = "state_label";

        ReadOnlyTransactionManager m_TransactionManager;
        ObjectField m_TransactionAssetField;
        UnityEngine.UIElements.ListView m_TransactionListView;

        public List<Transaction> Transactions { get; } = new List<Transaction>();

        // Add menu named "My Window" to the Window menu
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (TransactionViewer)GetWindow(typeof(TransactionViewer));
            window.Show();
        }

        public static TransactionViewer OpenOnRange(string filePath, TimeRange timeRange, bool listenToChanges)
        {
            var window = (TransactionViewer)GetWindow(typeof(TransactionViewer));
            window.Show();
            window.LoadDatabase(filePath, timeRange, listenToChanges);

            return window;
        }

        void OnEnable()
        {
            m_TransactionManager = new ReadOnlyTransactionManager();

            m_TransactionAssetField = new ObjectField("Transaction Database");
            m_TransactionAssetField.objectType = typeof(DefaultAsset);
            m_TransactionAssetField.RegisterValueChangedCallback(evt =>
            {
                var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
                LoadDatabase(assetPath, TimeRange.All(), true);
            });
            rootVisualElement.Add(m_TransactionAssetField);


            m_TransactionListView  = new UnityEngine.UIElements.ListView();
            m_TransactionListView.itemsSource = Transactions;
            m_TransactionListView.fixedItemHeight = k_TransactionRowHeight;
            m_TransactionListView.makeItem = MakeRowItem;
            m_TransactionListView.bindItem = BindRowItem;
            m_TransactionListView.style.flexGrow = 1.0f;
            rootVisualElement.Add(m_TransactionListView);
        }

        void HandleTransactionsAdded(DateTime newDateTime)
        {
            var timeRange = TimeRange.From(newDateTime, false);
            UpdateListView(m_TransactionListView, timeRange);
        }

        void OnDisable()
        {
            m_TransactionManager.Shutdown();
        }

        void LoadDatabase(string path, TimeRange timeRange, bool listenToChanges)
        {
            m_TransactionManager.Shutdown();
            Transactions.Clear();

            if (string.IsNullOrEmpty(path))
            {
                m_TransactionAssetField.SetValueWithoutNotify(null);
            }
            else
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                m_TransactionAssetField.SetValueWithoutNotify(asset);

                m_TransactionManager.SetFilePath(path);
                m_TransactionManager.Init();

                if (listenToChanges)
                    m_TransactionManager.transactionsAdded += HandleTransactionsAdded;
            }

            UpdateListView(m_TransactionListView, timeRange);
        }

        void UpdateListView(UnityEngine.UIElements.ListView listViewElement, TimeRange timeRange)
        {
            if (m_TransactionManager.Initialized)
            {
                var transactions = m_TransactionManager.Read(timeRange);
                Transactions.AddRange(transactions);
            }

            EditorApplication.delayCall += listViewElement.Rebuild;
        }

        static VisualElement MakeRowItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var dateLabel = new Label();
            dateLabel.name = k_DateLabelName;
            row.Add(dateLabel);

            var pathLabel = new Label();
            pathLabel.name = k_PathLabelName;
            row.Add(pathLabel);

            var stateLabel = new Label();
            stateLabel.name = k_StateLabelName;
            row.Add(stateLabel);

            return row;
        }

        void BindRowItem(VisualElement element, int index)
        {
            if (index < 0 || index >= Transactions.Count)
                return;

            var transaction = Transactions[index];
            var date = DateTime.FromBinary(transaction.timestamp).ToUniversalTime();
            var assetPath = AssetDatabase.GUIDToAssetPath(transaction.guid.ToString());

            var dateLabel = element.Q<Label>(k_DateLabelName);
            dateLabel.text = $"{date:u}";

            var pathLabel = element.Q<Label>(k_PathLabelName);
            pathLabel.text = assetPath;

            var stateLabel = element.Q<Label>(k_StateLabelName);
            stateLabel.text = transaction.GetState().ToString();
        }
    }
}
