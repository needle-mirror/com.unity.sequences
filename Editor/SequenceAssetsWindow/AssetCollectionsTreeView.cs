using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    partial class AssetCollectionsTreeView : SequencesTreeView
    {
        static class Styles
        {
            internal static readonly Dictionary<string, string> itemIconClassNames = new Dictionary<string, string>()
            {
                { "Character", itemIconClassName + "-character" },
                { "Fx", itemIconClassName + "-fx" },
                { "Lighting", itemIconClassName + "-lighting" },
                { "Photography", itemIconClassName + "-photography" },
                { "Prop", itemIconClassName + "-prop" },
                { "Set", itemIconClassName + "-set" },
                { "Audio", itemIconClassName + "-audio" }
            };

            internal static readonly string prefabItemIconClassName = itemIconClassName + "-prefab";
            internal static readonly string prefabVariantItemIconClassName = itemIconClassName + "-prefab-variant";
        }

        class AssetCollectionTreeViewItem
        {
            internal enum Type
            {
                Header,
                Item
            }

            internal Type treeViewItemType { get; } = Type.Item;
            internal string collectionName;
            internal GameObject asset;

            internal AssetCollectionTreeViewItem(Type type, string name, GameObject prefab = null)
            {
                treeViewItemType = type;
                collectionName = name;
                asset = prefab;
            }

            internal string GetItemText()
            {
                if (treeViewItemType == Type.Header)
                    return collectionName;

                if (asset == null)
                    return "NewSequenceAsset";

                return asset.name;
            }
        }

        // Keep an indexer to assign unique ID to new TreeViewItem.
        // ID starts at 1 as the root item's ID is 0.
        [SerializeField]
        int m_IndexGenerator = 1;

        public AssetCollectionsTreeView() : base()
        {
            // TODO: handle multi-selection.
            selectionType = SelectionType.Single;

            var rootItems = GenerateDataTree();
            SetRootItems(rootItems);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            SequenceAssetIndexer.sequenceAssetImported += OnSequenceAssetImported;
            SequenceAssetIndexer.sequenceAssetDeleted += OnSequenceAssetDeleted;
            SequenceAssetIndexer.sequenceAssetUpdated += OnSequenceAssetUpdated;
#if UNITY_2022_2_OR_NEWER
            selectionChanged += OnTreeViewSelectionChanged;
#else
            onSelectionChange += OnTreeViewSelectionChanged;
#endif
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            SequenceAssetIndexer.sequenceAssetImported -= OnSequenceAssetImported;
            SequenceAssetIndexer.sequenceAssetDeleted -= OnSequenceAssetDeleted;
            SequenceAssetIndexer.sequenceAssetUpdated -= OnSequenceAssetUpdated;

#if UNITY_2022_2_OR_NEWER
            selectionChanged -= OnTreeViewSelectionChanged;
#else
            onSelectionChange -= OnTreeViewSelectionChanged;
#endif
        }

        protected override void InitClassListAtIndex(VisualElement ve, int index)
        {
            var icon = GetIconElement(ve);
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);
            string classToEnable;

            if (data != null && data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                classToEnable = Styles.itemIconClassNames[data.collectionName];
            else if (data != null && data.treeViewItemType == AssetCollectionTreeViewItem.Type.Item)
            {
                if (SequenceAssetUtility.IsSource(data.asset))
                    classToEnable = Styles.prefabItemIconClassName;
                else
                    classToEnable = Styles.prefabVariantItemIconClassName;
            }
            else
            {
                var parent = GetItemDataForId<AssetCollectionTreeViewItem>(GetParentIdForIndex(index));
                classToEnable = (parent.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    ? Styles.prefabItemIconClassName
                    : Styles.prefabVariantItemIconClassName;
            }

            icon.EnableInClassList(classToEnable, true);
        }

        protected override void ResetClassListAtIndex(VisualElement ve, int index)
        {
            var icon = GetIconElement(ve);
            foreach (var itemClass in Styles.itemIconClassNames)
                icon.EnableInClassList(itemClass.Value, false);

            icon.EnableInClassList(Styles.prefabItemIconClassName, false);
            icon.EnableInClassList(Styles.prefabVariantItemIconClassName, false);
        }

        protected override void DoubleClicked(int index)
        {
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);
            if (data.treeViewItemType == AssetCollectionTreeViewItem.Type.Item)
                AssetDatabase.OpenAsset(data.asset);
        }

        protected override void DuplicateSelectedItems()
        {
            var selectedVariantsData = selectedIndices
                .Select(GetItemDataForIndex<AssetCollectionTreeViewItem>)
                .Where(data => data is { treeViewItemType: AssetCollectionTreeViewItem.Type.Item } &&
                    PrefabUtility.IsPartOfVariantPrefab(data.asset))
                .ToArray();

            foreach (var data in selectedVariantsData)
            {
                SequenceAssetUtility.DuplicateVariant(data.asset);
            }
        }

        protected override void ContextClicked(DropdownMenu menu)
        {
            var indices = selectedIndices.ToArray();
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(indices[0]);

            if (data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                PopulateContextMenuForAssetCollection(menu, indices[0]);
            else
            {
                if (SequenceAssetUtility.IsSource(data.asset))
                    PopulateContextMenuForSequenceAsset(menu, indices[0]);
                else
                    PopulateContextMenuForSequenceAssetVariant(menu, indices[0]);
            }
        }

        protected override void DeleteSelectedItems()
        {
            int[] indices = selectedIndices.ToArray();

            for (int i = indices.Length - 1; i >= 0; --i)
            {
                var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(indices[i]);
                if (data == null || data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    continue;

                if (!UserVerifications.ValidateSequenceAssetDeletion(data.asset))
                    return;

                var parent = GetItemDataForId<AssetCollectionTreeViewItem>(GetParentIdForIndex(indices[i]));
                if (parent.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    SequenceAssetUtility.DeleteSourceAsset(data.asset);
                else
                    SequenceAssetUtility.DeleteVariantAsset(data.asset);
            }
        }

        protected override string GetItemTextForIndex(int index)
        {
            var itemData = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);
            if (itemData != null)
                return itemData.GetItemText();

            var parentId = GetParentIdForIndex(index);
            var parent = GetItemDataForId<AssetCollectionTreeViewItem>(parentId);

            if (parent.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                return SequenceAssetUtility.GetDefaultSequenceAssetName(parent.collectionName);

            return SequenceAssetUtility.GetVariantName(parent.asset);
        }

        protected override void RenameEnded(int id, bool canceled = false)
        {
            var root = GetRootElementForId(id);
            var label = root.Q<RenameableLabel>();
            var newName = label.text;
            var assetTreeView = GetItemDataForId<AssetCollectionTreeViewItem>(id);

            canceled |= assetTreeView != null && string.IsNullOrWhiteSpace(newName);
            if (canceled)
            {
                if (assetTreeView == null)
                    TryRemoveItem(id);

                var index = viewController.GetIndexForId(id);
                RefreshItem(index);
                return;
            }

            newName = SequencesAssetDatabase.SanitizeFileName(newName);
            label.text = newName;

            // An asset creation is requested from the user.
            if (assetTreeView == null)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    var index = viewController.GetIndexForId(id);
                    newName = GetItemTextForIndex(index);
                }

                var parentId = viewController.GetParentId(id);

                // Remove the item used as a placeholder. OnSequenceAssetImported will be called once the
                // API is done creating the asset. That's the moment where we'll create the definitive item.
                viewController.TryRemoveItem(id, false);

                var parentItem = GetItemDataForId<AssetCollectionTreeViewItem>(parentId);
                if (parentItem.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    SequenceAssetUtility.CreateSource(newName, parentItem.collectionName);
                else
                {
                    SequenceAssetUtility.CreateVariant(parentItem.asset, newName);
                }
            }
            else
                SequenceAssetUtility.Rename(assetTreeView.asset, assetTreeView.asset.name, newName);
        }

        protected override bool CanRename(int index)
        {
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);
            if (data != null)
                return data.treeViewItemType == AssetCollectionTreeViewItem.Type.Item;

            // Index can lead to an invalid TreeViewDataItem when they are placeholders.
            // Ex: placeholder for a new asset until the user validates the asset name.
            return base.CanRename(index);
        }

        List<TreeViewItemData<AssetCollectionTreeViewItem>> GenerateDataTree()
        {
            var rootItems = new List<TreeViewItemData<AssetCollectionTreeViewItem>>();

            foreach (var collection in CollectionType.instance.types)
            {
                rootItems.Add(GenerateDataItem(collection));
            }

            return rootItems;
        }

        TreeViewItemData<AssetCollectionTreeViewItem> GenerateDataItem(string collection)
        {
            var children = new List<TreeViewItemData<AssetCollectionTreeViewItem>>();
            foreach (var sourcePrefab in SequenceAssetUtility.FindAllSources(collection))
            {
                children.Add(GenerateDataItem(collection, sourcePrefab));
            }

            var itemData = new AssetCollectionTreeViewItem(AssetCollectionTreeViewItem.Type.Header, collection);
            var item = new TreeViewItemData<AssetCollectionTreeViewItem>(
                GetNextId(),
                itemData,
                children);

            return item;
        }

        TreeViewItemData<AssetCollectionTreeViewItem> GenerateDataItem(string collection, GameObject prefab)
        {
            var itemData = new AssetCollectionTreeViewItem(AssetCollectionTreeViewItem.Type.Item, collection, prefab);

            var variants = new List<TreeViewItemData<AssetCollectionTreeViewItem>>();
            foreach (var variant in SequenceAssetUtility.GetVariants(prefab))
            {
                var childItemData = new AssetCollectionTreeViewItem(AssetCollectionTreeViewItem.Type.Item, collection, variant);
                var childItem = new TreeViewItemData<AssetCollectionTreeViewItem>(GetNextId(), childItemData);
                variants.Add(childItem);
            }

            var item = new TreeViewItemData<AssetCollectionTreeViewItem>(GetNextId(), itemData, variants);
            return item;
        }

        int GetNextId()
        {
            return m_IndexGenerator++;
        }

        /// <summary>
        /// Listens for new import of Sequence Assets from the AssetDatabase.
        /// </summary>
        /// <param name="gameObject"></param>
        void OnSequenceAssetImported(GameObject gameObject)
        {
            if (GetIdFor(gameObject) > -1)
                return;

            string type = SequenceAssetUtility.GetType(gameObject);

            // Duplicated Prefab or Prefab variant created from the Project View.
            if (SequenceAssetUtility.IsSource(gameObject))
            {
                int parentId = GetIdFor(type);
                var itemData = new AssetCollectionTreeViewItem(AssetCollectionTreeViewItem.Type.Item, type, gameObject);
                AddItem(new TreeViewItemData<AssetCollectionTreeViewItem>(GetNextId(), itemData), parentId);
            }
            else if (SequenceAssetUtility.IsVariant(gameObject))
            {
                GameObject baseObject = SequenceAssetUtility.GetSource(gameObject);
                int parentId = GetIdFor(baseObject);
                var itemData = new AssetCollectionTreeViewItem(AssetCollectionTreeViewItem.Type.Item, type, gameObject);
                AddItem(new TreeViewItemData<AssetCollectionTreeViewItem>(GetNextId(), itemData), parentId);
            }
        }

        /// <summary>
        /// Listens for deletion of Sequence Assets in the AssetDatabse.
        /// It detaches TreeView items affected by this deletion.
        /// </summary>
        void OnSequenceAssetDeleted()
        {
            // Deletion from the asset database.
            // Go over all the items and delete the ones with an invalid prefab reference.
            var ids = viewController.GetAllItemIds().ToArray();
            foreach (var id in ids)
            {
                var data = GetItemDataForId<AssetCollectionTreeViewItem>(id);

                if (data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    continue;

                if (data.asset == null)
                    viewController.TryRemoveItem(id, false);
            }
            viewController.RebuildTree();
            RefreshItems();
        }

        void OnSequenceAssetUpdated(GameObject sequenceAsset)
        {
            foreach (var id in viewController.GetAllItemIds())
            {
                var data = GetItemDataForId<AssetCollectionTreeViewItem>(id);
                if (data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    continue;

                if (data.asset == sequenceAsset)
                {
                    RefreshItem(viewController.GetIndexForId(id));
                    break;
                }
            }
        }

        void OnTreeViewSelectionChanged(IEnumerable<object> objs)
        {
            foreach (AssetCollectionTreeViewItem item in objs)
            {
                if (item == null || item.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    continue;

                SelectionUtility.SetSelection(item.asset);

                // Does not support multi-selection at the moment.
                break;
            }
        }

        int GetIdFor(string assetCollection)
        {
            foreach (var rootId in viewController.GetRootItemIds())
            {
                var data = GetItemDataForId<AssetCollectionTreeViewItem>(rootId);

                if (data.collectionName == assetCollection)
                    return rootId;
            }

            return -1;
        }

        int GetIdFor(GameObject asset)
        {
            foreach (var id in viewController.GetAllItemIds())
            {
                var data = GetItemDataForId<AssetCollectionTreeViewItem>(id);
                if (data.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                    continue;

                if (data.asset == asset)
                    return id;
            }

            return -1;
        }

        internal void BeginSequenceAssetCreation(string assetCollection)
        {
            int id = GetIdFor(assetCollection);
            BeginItemCreation<AssetCollectionTreeViewItem>(id);
        }
    }
}
