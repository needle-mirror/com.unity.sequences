using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    internal partial class StructureTreeView : TreeView
    {
        static class Styles
        {
            public static readonly Color k_InvalidColorLight = new Color32(142, 10, 10, 255);
            public static readonly Color k_InvalidColorDark = new Color32(255, 120, 120, 255);
        }

        VisualElement m_VisualElementContainer;
        List<TreeViewItem> m_Items;

        TreeViewItem m_RootTreeViewItem;

        // Keep an indexer to assign unique ID to new TreeViewItem.
        int m_IndexGenerator;

        public StructureTreeView(TreeViewState state, VisualElement container)
            : base(state)
        {
            m_VisualElementContainer = container;
            m_Items = new List<TreeViewItem>();

            m_IndexGenerator = 0;
            m_RootTreeViewItem = new TreeViewItem { id = GetNextId(), depth = -1, displayName = "Root" };
            SetupParentsAndChildrenFromDepths(m_RootTreeViewItem, m_Items);
            Reload();

            getNewSelectionOverride = OnNewSelection;
            SelectionUtility.sequenceSelectionChanged += OnSequenceSelectionChanged;
            SequenceUtility.sequenceCreated += OnSequenceUpdate;
            SequenceUtility.sequenceDeleted += OnSequenceDeleted;
            Sequence.sequenceRenamed += OnSequenceRenamed;
        }

        void OnSequenceUpdate(TimelineSequence sequence, MasterSequence masterSequence)
        {
            RefreshData();
        }

        void OnSequenceDeleted()
        {
            RefreshData();
        }

        void OnSequenceSelectionChanged()
        {
            Sequence sequence = SelectionUtility.activeSequenceSelection;
            if (sequence == null)
            {
                SetSelection(new int[] {}, TreeViewSelectionOptions.None);
                return;
            }

            var foundItem = m_Items.Find(item => (item as EditorialElementTreeViewItem).timelineSequence == sequence);
            if (foundItem != null)
                SetSelection(new int[] { foundItem.id }, TreeViewSelectionOptions.RevealAndFrame);
        }

        void OnSequenceRenamed(Sequence sequence)
        {
            var found = m_Items.Find(x => ((EditorialElementTreeViewItem)x).timelineSequence == sequence);
            if (found != null)
                found.displayName = sequence.name;
        }

        void GenerateTreeFromData(IEnumerable<MasterSequence> masterSequences)
        {
            foreach (var masterSequence in masterSequences)
            {
                if (masterSequence.manager != null && masterSequence.manager.count > 0 && masterSequence.rootSequence != null)
                {
                    GenerateMasterSequenceTreeView(masterSequence);
                }
            }
        }

        void GenerateMasterSequenceTreeView(MasterSequence masterSequence)
        {
            var masterSequenceTreeViewItem = CreateNewMasterSequenceFrom(masterSequence);

            if (!masterSequence.rootSequence.hasChildren)
                return;

            foreach (var childSequence in masterSequence.rootSequence.children)
            {
                SequenceTreeViewItem sequenceItem = CreateNewSequenceFrom(childSequence as TimelineSequence, masterSequenceTreeViewItem, masterSequence);
                GenerateSequenceTreeView(childSequence as TimelineSequence, sequenceItem, masterSequence);
            }
        }

        void GenerateSequenceTreeView(TimelineSequence sequence, SequenceTreeViewItem parent, MasterSequence asset)
        {
            if (!sequence.hasChildren)
                return;

            foreach (var child in sequence.children)
            {
                CreateNewSubSequenceFrom((TimelineSequence)child, parent, asset);
            }
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

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.isRenaming)
            {
                base.RowGUI(args);
                return;
            }
            var item = args.item as EditorialElementTreeViewItem;


            GUIContent itemLabel = new GUIContent(args.label, (args.selected) ? item.iconSelected : item.icon);
            var indentLevel = EditorGUI.indentLevel;

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label);

            if (item.GetTargetValidity() != SequenceUtility.SequenceValidity.Valid)
                itemStyle.normal.textColor = EditorGUIUtility.isProSkin ? Styles.k_InvalidColorDark : Styles.k_InvalidColorLight;
            else
                itemStyle.normal.textColor = GUI.skin.label.normal.textColor;

            itemLabel.tooltip = "";
            if (item.GetTargetValidity().HasFlag(SequenceUtility.SequenceValidity.MissingGameObject))
                itemLabel.tooltip += "Missing GameObject";

            if (item.GetTargetValidity().HasFlag(SequenceUtility.SequenceValidity.MissingTimeline))
                itemLabel.tooltip += String.IsNullOrEmpty(itemLabel.tooltip) ? "Missing Timeline" : "\nMissing Timeline";

            if (item.GetTargetValidity().HasFlag(SequenceUtility.SequenceValidity.Orphan))
                itemLabel.tooltip += String.IsNullOrEmpty(itemLabel.tooltip) ? "Missing Timeline in a parent Sequence" : "\nMissing Timeline in a parent Sequence";

            EditorGUI.indentLevel = args.item.depth + 1;
            EditorGUI.LabelField(args.rowRect, itemLabel, itemStyle);
            EditorGUI.indentLevel = indentLevel;
        }

        public void RefreshData()
        {
            m_Items.Clear();
            SetupParentsAndChildrenFromDepths(m_RootTreeViewItem, m_Items);
            Reload();

            IEnumerable<MasterSequence> existingAssets = SequencesAssetDatabase.FindAsset<MasterSequence>();
            GenerateTreeFromData(existingAssets);

            Reload();
            ExpandAll();
        }

        protected override TreeViewItem BuildRoot()
        {
            SetupDepthsFromParentsAndChildren(m_RootTreeViewItem);

            return m_RootTreeViewItem;
        }

        protected override void ContextClickedItem(int id)
        {
            GetItem(id).ContextClicked();
        }

        protected override void DoubleClickedItem(int id)
        {
            GetItem(id).DoubleClicked();
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
            var editorialItem = item as EditorialElementTreeViewItem;
            if (editorialItem != null && editorialItem.GetTargetValidity() != SequenceUtility.SequenceValidity.Valid)
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
                        RefreshData();
                }
                else if (item.state == TreeViewItemBase.State.Ok)
                {
                    item.Rename(args.newName);
                    RefreshData();
                }
            }
            else
            {
                if (item.state == TreeViewItemBase.State.Creation)
                    RefreshData();
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
                if (item != null)
                    item.Delete();
            }
            RefreshData();
        }

        MasterSequenceTreeViewItem CreateNewMasterSequenceFrom(MasterSequence masterSequence)
        {
            int newId = GetNextId();

            MasterSequenceTreeViewItem newItem = new MasterSequenceTreeViewItem { id = newId, depth = 0, displayName = masterSequence.rootSequence.name };
            newItem.owner = this;
            newItem.SetAsset(masterSequence);

            m_Items.Add(newItem);
            rootItem.AddChild(newItem);

            return newItem;
        }

        SequenceTreeViewItem CreateNewSequenceFrom(TimelineSequence sequence, MasterSequenceTreeViewItem parent, MasterSequence masterSequence)
        {
            int newId = GetNextId();
            var newItem = new SequenceTreeViewItem { id = newId, depth = parent.depth + 1, displayName = sequence.name };
            newItem.owner = this;
            newItem.SetSequence(sequence);
            newItem.SetMasterSequence(masterSequence);

            m_Items.Add(newItem);
            parent.AddChild(newItem);

            return newItem;
        }

        SubSequenceTreeViewItem CreateNewSubSequenceFrom(TimelineSequence sequence, SequenceTreeViewItem parent, MasterSequence masterSequence)
        {
            int newId = GetNextId();
            SubSequenceTreeViewItem newItem = new SubSequenceTreeViewItem { id = newId, depth = parent.depth + 1, displayName = sequence.name };
            newItem.owner = this;
            newItem.SetSequence(sequence);
            newItem.SetMasterSequence(masterSequence);

            m_Items.Add(newItem);
            parent.AddChild(newItem);

            return newItem;
        }

        public void CreateNewMasterSequence()
        {
            int newId = GetNextId();

            MasterSequenceTreeViewItem newItem = new MasterSequenceTreeViewItem { id = newId, depth = 0 };
            newItem.owner = this;
            rootItem.AddChild(newItem);
            m_Items.Add(newItem);

            Reload();

            SetSelection(new int[] { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
            BeginRename(newItem);
        }

        public void CreateNewSequenceInContext(MasterSequenceTreeViewItem parent)
        {
            if (parent is MasterSequenceTreeViewItem)
            {
                int newId = GetNextId();
                var newItem = new SequenceTreeViewItem { id = newId, depth = parent.depth + 1 };
                newItem.owner = this;

                m_Items.Add(newItem);
                parent.AddChild(newItem);

                Reload();

                SetSelection(new int[] { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                BeginRename(newItem);
            }
        }

        public void CreateNewSubSequenceInContext(SequenceTreeViewItem parent)
        {
            if (parent is SequenceTreeViewItem)
            {
                int newId = GetNextId();
                SubSequenceTreeViewItem newItem = new SubSequenceTreeViewItem { id = newId, depth = parent.depth + 1};
                newItem.owner = this;

                m_Items.Add(newItem);
                parent.AddChild(newItem);

                SetupDepthsFromParentsAndChildren(m_RootTreeViewItem);

                Reload();

                SetSelection(new int[] { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                BeginRename(newItem);
            }
        }

        public bool SelectionContains(Sequence sequence)
        {
            var selections = GetSelection();
            foreach (int id in selections)
            {
                var item = GetItem(id);

                if (item == null)
                    continue;

                if ((item as EditorialElementTreeViewItem).timelineSequence == sequence)
                    return true;
            }
            return false;
        }

        TreeViewItemBase GetItem(int id)
        {
            return m_Items.Find(x => x.id == id) as TreeViewItemBase;
        }

        int GetNextId()
        {
            return m_IndexGenerator++;;
        }
    }
}
