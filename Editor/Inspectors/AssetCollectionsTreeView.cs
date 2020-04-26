using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    internal class AssetCollectionsTreeView : TreeView
    {
        VisualElement m_VisualElementContainer;
        List<TreeViewItem> m_Items;

        TreeViewItem m_RootTreeViewItem;

        // Keep an indexer to assign unique ID to new TreeViewItem.
        int m_IndexGenerator;

        public AssetCollectionsTreeView(TreeViewState state, VisualElement container)
            : base(state)
        {
            m_VisualElementContainer = container;
            m_Items = new List<TreeViewItem>();

            m_IndexGenerator = 0;
            m_RootTreeViewItem = new TreeViewItem { id = GetNextId(), depth = -1, displayName = "Root" };
            SetupParentsAndChildrenFromDepths(m_RootTreeViewItem, m_Items);
            Reload();

            getNewSelectionOverride = OnNewSelection;

            SequenceAssetIndexer.indexerChanged += RefreshData;
        }

        void GenerateTreeFromData(GameObject[] sequenceAssets)
        {
            foreach (var userType in CollectionType.instance.types)
                GenerateAssetCollectionTreeView(userType, sequenceAssets);
        }

        void GenerateAssetCollectionTreeView(string collectionType, GameObject[] assets)
        {
            CollectionTypeTreeViewItem collectionTypeTreeViewItem = CreateAssetCollectionTreeViewItem(collectionType);

            var content = (assets != null && assets.Length > 0) ? Array.FindAll(
                assets,
                a => SequenceAssetUtility.GetType(a) == collectionType) : null;

            if (content == null || content.Length == 0)
                return;

            foreach (var sequenceAsset in content)
                GenerateSequenceAssetTreeView(sequenceAsset, collectionTypeTreeViewItem);
        }

        void GenerateSequenceAssetTreeView(GameObject asset, CollectionTypeTreeViewItem parent)
        {
            var sequenceAssetTreeViewItem = CreateSequenceAssetTreeViewItem(asset, parent);

            foreach (var variant in SequenceAssetUtility.GetVariants(asset))
                CreateSequenceAssetVariantTreeViewItem(variant, sequenceAssetTreeViewItem);
        }

        public void OnGUI()
        {
            Event evt = Event.current;

            if (HasFocus() && HasSelection())
            {
                if (evt.isKey && evt.type == EventType.KeyUp && evt.keyCode == KeyCode.Delete)
                {
                    RemoveSelection(GetSelection());
                }
            }

            OnGUI(m_VisualElementContainer.contentRect);
        }

        internal void RefreshData()
        {
            m_Items.Clear();
            SetupParentsAndChildrenFromDepths(m_RootTreeViewItem, m_Items);
            Reload();

            GameObject[] sequenceAssets = SequenceAssetUtility.FindAllSources().ToArray();
            GenerateTreeFromData(sequenceAssets);

            Reload();
            ExpandAll();
        }

        internal void CreateSequenceAssetInContext(string collectionType)
        {
            var parent = m_Items.Find(x => (x is CollectionTypeTreeViewItem) && (x as CollectionTypeTreeViewItem).collectionType == collectionType);
            if (parent == null)
            {
                parent = CreateAssetCollectionTreeViewItem(collectionType);
            }

            if (parent != null)
            {
                int newId = GetNextId();
                var newItem = new SequenceAssetTreeViewItem { id = newId, depth = parent.depth + 1, displayName = $"{collectionType}Asset"};
                newItem.owner = this;
                m_Items.Add(newItem);
                parent.AddChild(newItem);

                Reload();

                SetSelection(new int[] { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                BeginRename(newItem);
            }
        }

        internal void CreateSequenceAssetVariantInContext(GameObject source)
        {
            SequenceAssetUtility.CreateVariant(source);
            RefreshData();
        }

        CollectionTypeTreeViewItem CreateAssetCollectionTreeViewItem(string name)
        {
            int newId = GetNextId();

            CollectionTypeTreeViewItem newItem = new CollectionTypeTreeViewItem() { id = newId, depth = 0, displayName = name };
            newItem.SetCollectionType(name);
            newItem.owner = this;
            m_Items.Add(newItem);
            rootItem.AddChild(newItem);

            return newItem;
        }

        SequenceAssetTreeViewItem CreateSequenceAssetTreeViewItem(GameObject asset, CollectionTypeTreeViewItem parent)
        {
            int newId = GetNextId();

            SequenceAssetTreeViewItem newItem = new SequenceAssetTreeViewItem() { id = newId, depth = 1, displayName = asset.name };
            newItem.SetSequenceAsset(asset);
            newItem.owner = this;
            m_Items.Add(newItem);
            parent.AddChild(newItem);

            return newItem;
        }

        void CreateSequenceAssetVariantTreeViewItem(GameObject prefab, SequenceAssetTreeViewItem parent)
        {
            int newId = GetNextId();

            SequenceAssetVariantTreeViewItem newItem = new SequenceAssetVariantTreeViewItem() { id = newId, depth = 1, displayName = prefab.name };
            newItem.SetSequenceAssetVariant(prefab);
            newItem.owner = this;
            m_Items.Add(newItem);
            parent.AddChild(newItem);
        }

        protected override void ContextClickedItem(int id)
        {
            GetItem(id).ContextClicked();
        }

        List<int> OnNewSelection(TreeViewItem clickedItem, bool keepMultiSelection, bool useActionKeyAsShift)
        {
            var item = clickedItem as TreeViewItemBase;
            if (item != null)
            {
                item.Selected();
                return new List<int>() { clickedItem.id };
            }
            return null;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            if (item is CollectionTypeTreeViewItem)
                return false;

            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = GetItem(args.itemID);

            if (args.acceptedRename)
            {
                if (item.state == TreeViewItemBase.State.Creation)
                {
                    if (item.ValidateCreation(args.newName))
                    {
                        RefreshData();
                    }
                    else
                        RemoveSelection(new int[] { args.itemID });
                }
                else if (item.state == TreeViewItemBase.State.Ok)
                {
                    item.Rename(args.newName);
                    // TODO: only rebuild the children as opposed to the full tree view
                    RefreshData();
                }
            }
            else
            {
                if (item.state == TreeViewItemBase.State.Creation)
                    RemoveSelection(new int[] { args.itemID });
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        void RemoveSelection(IList<int> ids)
        {
            foreach (int id in ids)
            {
                var item = GetItem(id);
                // item can be null if the tree view is in focus but no item is selected
                if (item is object)
                    item.Delete();
            }
            RefreshData();
        }

        TreeViewItemBase GetItem(int id)
        {
            return m_Items.Find(x => x.id == id) as TreeViewItemBase;
        }

        int GetNextId()
        {
            return m_IndexGenerator++;
        }

        protected override TreeViewItem BuildRoot()
        {
            SetupDepthsFromParentsAndChildren(m_RootTreeViewItem);
            return m_RootTreeViewItem;
        }

        protected override void DoubleClickedItem(int id)
        {
            TreeViewItemBase item = GetItem(id);
            item.DoubleClicked();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var collectionTypeItem = args.item as CollectionTypeTreeViewItem;

            /// When collectionTypeItem is null, it means the treeview is diplaying a Prefab or a Prefab Variant.
            /// In that case, we fallback to the default drawing method.
            if (args.isRenaming || collectionTypeItem == null)
            {
                base.RowGUI(args);
                return;
            }

            GUIContent gui = new GUIContent(args.label, (args.selected) ? collectionTypeItem.iconSelected : collectionTypeItem.icon);
            var indentLevel = EditorGUI.indentLevel;

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label);
            itemStyle.normal.textColor = GUI.skin.label.normal.textColor;

            EditorGUI.indentLevel = args.item.depth + 1;
            EditorGUI.LabelField(args.rowRect, gui, itemStyle);
            EditorGUI.indentLevel = indentLevel;
        }
    }
}
