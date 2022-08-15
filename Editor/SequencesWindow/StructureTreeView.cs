using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    partial class StructureTreeView : SequencesTreeView
    {
        public static readonly string masterSequenceClassName = itemIconClassName + "-master-sequence";
        public static readonly string sequenceClassName = itemIconClassName + "-sequence";
        public static readonly string shotClassName = itemIconClassName + "-shot";
        public static readonly string itemInvalidClassName = itemLabelClassName + "-invalid";
        public static readonly string itemNotLoadedClassName = itemLabelClassName + "-not-loaded";

        bool m_PreventSelectionLoop = false;

        public StructureTreeView() : base()
        {
            if (SequenceIndexer.instance.isEmpty)
            {
                // If the indexer is not yet initialized, delay the tree view data generation.
                SequenceIndexer.indexerInitialized += InitializeRootItems;
                SetRootItems(new List<TreeViewItemData<StructureItem>>());
                return;
            }

            SequenceIndexer.indexerInitialized -= InitializeRootItems;
            InitializeRootItems();
        }

        protected override string GetItemTextForIndex(int index)
        {
            var itemData = GetItemDataForIndex<StructureItem>(index);
            if (!itemData.IsNull())
                return itemData.displayName;

            var parentId = GetParentIdForIndex(index);
            return parentId == -1
                ? SequenceUtility.k_DefaultMasterSequenceName
                : SequenceUtility.k_DefaultSequenceName;
        }

        protected override void ContextClicked(DropdownMenu menu)
        {
            PopulateContextMenu(menu);
        }

        protected override void DeleteSelectedItems()
        {
            if (CanDeleteSelection())
                DeleteSelectedItemsInternal();
        }

        void DeleteSelectedItemsInternal()
        {
            var timelinesToDelete = new List<TimelineAsset>();
            var items = GetSelectedItems<StructureItem>().ToArray();

            foreach (var item in items)
            {
                if (item.data.timeline != null)
                    timelinesToDelete.Add(item.data.timeline);
            }

            if (timelinesToDelete.Count > 0 && !UserVerifications.ValidateSequencesDeletion(timelinesToDelete.ToArray()))
                return;

            foreach (var item in items)
            {
                if (item.data.timeline == null)
                {
                    DeleteInvalidItem(viewController.GetIndexForId(item.id));
                    viewController.TryRemoveItem(item.id, false);
                    continue;
                }

                MasterSequenceUtility.GetLegacyData(item.data.timeline, out var masterSequence, out var sequence);
                if (masterSequence.rootSequence == sequence)
                {
                    using (new SequenceIndexer.DisableEvent())
                        masterSequence.Delete();
                }
                else
                {
                    using (new SequenceIndexer.DisableEvent())
                        SequenceUtility.DeleteSequence(sequence, masterSequence);
                }
            }

            viewController.RebuildTree();
            RefreshItems();
        }

        void DeleteInvalidItem(int index)
        {
            var parentId = GetParentIdForIndex(index);
            if (parentId != -1 && IsSelected(viewController.GetIndexForId(parentId)))
                return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Delete invalid sequence");
            var groupIndex = Undo.GetCurrentGroup();

            var itemData = GetItemDataForIndex(index);
            if (itemData.gameObject != null)
                Undo.DestroyObjectImmediate(itemData.gameObject);

            if (parentId == -1)
            {
                Undo.CollapseUndoOperations(groupIndex);
                return;
            }

            var parentData = GetItemDataForId(parentId);
            if (parentData.timeline != null)
            {
                var parentSequence = SequenceIndexer.instance.GetSequence(parentData.timeline);

                Undo.RecordObject(parentSequence.timeline, "Delete invalid sequence");
                parentSequence.timeline.DeleteClip(itemData.editorialClip);
            }

            Undo.CollapseUndoOperations(groupIndex);
            // Ideally, UI should refresh on undo/redo of this.
        }

        protected override void RenameEnded(int id, bool canceled = false)
        {
            var itemData = GetItemDataForId(id).timeline;

            var root = GetRootElementForId(id);
            var label = root.Q<RenameableLabel>();
            var newName = label.text;

            canceled |= itemData != null && string.IsNullOrWhiteSpace(newName);
            if (canceled)
            {
                if (itemData == null)
                    TryRemoveItem(id);

                var index = viewController.GetIndexForId(id);
                RefreshItem(index);
                return;
            }

            newName = SequencesAssetDatabase.SanitizeFileName(newName);
            label.text = newName;

            if (itemData == null)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    // TODO: This validation (and more) should be dealt with when actually creating or renaming a sequence.
                    var index = viewController.GetIndexForId(id);
                    newName = GetItemTextForIndex(index);
                }

                // Create a new MasterSequence or Sequence.
                var parentId = viewController.GetParentId(id);
                viewController.TryRemoveItem(id,
                    false); // Remove the dummy item. Don't rebuild the tree, it will be rebuilt when creating a definitive item.

                if (parentId == -1)
                {
                    SequenceUtility.CreateMasterSequence(newName);
                }
                else
                {
                    var parentItemData = GetItemDataForId(parentId);
                    if (parentItemData.timeline == null)
                        return;

                    MasterSequenceUtility.GetLegacyData(parentItemData.timeline, out var masterSequence,
                        out var parentSequence);
                    SequenceUtility.CreateSequence(
                        newName,
                        masterSequence,
                        parentSequence);
                }
            }
            else
            {
                // Rename
                MasterSequenceUtility.GetLegacyData(itemData, out var masterSequence, out var timelineSequence);
                var sequence = SequenceIndexer.instance.GetSequence(itemData);
                if (sequence.parent != null)
                {
                    timelineSequence.Rename(newName);
                }
                else
                {
                    masterSequence.Rename(newName);
                }
            }
        }

        protected override void InitClassListAtIndex(VisualElement ve, int index)
        {
            var icon = GetIconElement(ve);
            var parentId = GetParentIdForIndex(index);
            string classToEnable;

            if (parentId == -1)
                classToEnable = masterSequenceClassName;
            else if (viewController.GetParentId(parentId) == -1)
                classToEnable = sequenceClassName;
            else
                classToEnable = shotClassName;

            icon.EnableInClassList(classToEnable, true);

            // Check validity/loading state of data
            var label = GetLabelElement(ve);

            var itemData = GetItemDataForIndex<StructureItem>(index);
            if (itemData.IsNull())
            {
                label.EnableInClassList(itemInvalidClassName, false);
                label.EnableInClassList(itemNotLoadedClassName, false);
                return;
            }

            if (itemData.timeline == null)
                label.EnableInClassList(itemInvalidClassName, true);

            else
            {
                var sequence = SequenceIndexer.instance.GetSequence(itemData.timeline);

                if (!sequence.isValid) // Validity is more important than loading status.
                    label.EnableInClassList(itemInvalidClassName, true);

                else if (!sequence.hasGameObject)
                    label.EnableInClassList(itemNotLoadedClassName, true);
            }
        }

        protected override void ResetClassListAtIndex(VisualElement ve, int index)
        {
            var icon = GetIconElement(ve);
            icon.EnableInClassList(masterSequenceClassName, false);
            icon.EnableInClassList(sequenceClassName, false);
            icon.EnableInClassList(shotClassName, false);

            var label = GetLabelElement(ve);
            label.EnableInClassList(itemInvalidClassName, false);
            label.EnableInClassList(itemNotLoadedClassName, false);
        }

        protected override string GetTooltipForIndex(int index)
        {
            var itemData = GetItemDataForIndex<StructureItem>(index);
            if (itemData.IsNull())
                return string.Empty;

            var tooltips = string.Empty;

            SequenceNode sequence = null;
            if (itemData.timeline != null)
                sequence = SequenceIndexer.instance.GetSequence(itemData.timeline);

            if (sequence != null && sequence.isValid && !sequence.hasGameObject)
                tooltips = "Not in the Hierarchy";

            else if (sequence != null && sequence.parent != null && !sequence.parent.isValid)
                tooltips = "Invalid parent Sequence";

            else if (itemData.timeline == null)
                tooltips = "Missing Timeline asset or missing binding on the PlayableDirector";

            else if (sequence != null && !sequence.isValid && sequence.gameObject == null ||
                     sequence == null && itemData.gameObject == null)
            {
                tooltips = "Missing GameObject or PlayableDirector";
            }
            else if (sequence != null && !sequence.isValid)
                tooltips = "Missing binding on the Editorial clip";

            return tooltips;
        }

        protected override bool CanRename(int index)
        {
            if (inPlaymode)
                return false;

            var itemData = GetItemDataForIndex(index);
            if (itemData.IsNull())
                return true; // Item is being created, it can be rename.

            if (itemData.timeline == null)
                return false;

            var sequence = SequenceIndexer.instance.GetSequence(itemData.timeline);
            if (!sequence.isValid || !sequence.hasGameObject || sequence.isPrefabRoot)
                return false;

            return true;
        }

        bool CanDeleteSelection()
        {
            if (inPlaymode)
                return false;

            foreach (var index in selectedIndices)
            {
                var itemData = GetItemDataForIndex(index);
                if (itemData.timeline == null)
                    continue;

                var sequence = SequenceIndexer.instance.GetSequence(itemData.timeline);
                if (sequence.isValid && !sequence.hasGameObject || (sequence.isPrefabRoot && inPrefabStage))
                    return false;
            }

            return true;
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // Ensure to reflect selections in all views.
#if UNITY_2022_2_OR_NEWER
            selectedIndicesChanged += OnSelectionChanged;
#else
            onSelectedIndicesChange += OnSelectionChanged;
#endif
            SelectionUtility.sequenceSelectionChanged += OnSequenceSelectionChanged;

            // Add or remove tree view items when sequences are created or deleted from API.
            HierarchyDataChangeVerifier.sequenceCreated += OnSequenceCreated;
            SequenceUtility.sequenceDeleted += OnSequenceDeleted;

            // Add or remove tree view items when sequences are created or deleted manually.
            SequenceIndexer.sequenceRegistered += AddItemForSequence;
            SequenceIndexer.sequenceUpdated += OnSequenceUpdated;
            MasterSequenceUtility.masterSequencesRemoved += OnMasterSequencesRemoved;

            // Ensure the UI refresh to reflect invalid sequences or unloaded sequences.
            SequenceIndexer.validityChanged += RefreshItems;
            SequenceIndexer.sequencesRemoved += OnSequencesRemoved;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();
#if UNITY_2022_2_OR_NEWER
            selectedIndicesChanged -= OnSelectionChanged;
#else
            onSelectedIndicesChange -= OnSelectionChanged;
#endif
            SelectionUtility.sequenceSelectionChanged -= OnSequenceSelectionChanged;
            HierarchyDataChangeVerifier.sequenceCreated -= OnSequenceCreated;
            SequenceUtility.sequenceDeleted -= OnSequenceDeleted;
            SequenceIndexer.sequenceRegistered -= AddItemForSequence;
            SequenceIndexer.sequenceUpdated -= OnSequenceUpdated;
            MasterSequenceUtility.masterSequencesRemoved -= OnMasterSequencesRemoved;
            SequenceIndexer.validityChanged -= RefreshItems;
            SequenceIndexer.sequencesRemoved -= OnSequencesRemoved;
        }

        void OnSelectionChanged(IEnumerable<int> indices)
        {
            if (m_PreventSelectionLoop || !indices.Any())
            {
                m_PreventSelectionLoop = false;
                return;
            }

            // Select the first index if any.
            var itemData = GetItemDataForIndex<StructureItem>(indices.First());
            if (itemData.timeline != null)
                SelectionUtility.TrySelectSequenceWithoutNotify(itemData.timeline);
        }

        void OnSequenceSelectionChanged()
        {
            var sequence = SelectionUtility.activeSequenceSelection;
            if (sequence == null)
                return;

            foreach (var id in viewController.GetAllItemIds())
            {
                var itemData = GetItemDataForId(id);
                if (itemData.timeline != sequence)
                    continue;

                m_PreventSelectionLoop = true;
                SetSelectionById(id);
                break;
            }
        }

        void OnSequenceCreated(TimelineAsset timeline)
        {
            var sequence = SequenceIndexer.instance.GetSequence(timeline);
            AddItemForSequence(sequence);
        }

        void AddItemForSequence(SequenceNode sequence)
        {
            var parentId = -1;
            if (sequence.parent != null)
                parentId = sequence.parent.timeline.GetHashCode();

            var childIndex = -1;
            if (parentId == -1)
                childIndex = GetChildIndexForRootItem(sequence);

            var id = sequence.timeline.GetHashCode();
            var item = new TreeViewItemData<StructureItem>(id, new StructureItem(sequence));
            AddItem(item, parentId, childIndex);

            viewController.ExpandItem(parentId, false);
            SetSelectionById(id);
        }

        void OnSequenceDeleted()
        {
            var allIds = viewController.GetAllItemIds().ToArray();
            foreach (var id in allIds)
            {
                var itemData = GetItemDataForId(id);
                if (itemData.timeline == null)
                {
                    viewController.TryRemoveItem(id, false);
                }
            }

            viewController.RebuildTree();
            RefreshItems();
        }

        void OnSequenceUpdated(SequenceNode sequence)
        {
            var id = sequence.timeline.GetHashCode();
            var parentId = viewController.GetParentId(id);

            var childIndex = -1;
            if (parentId == -1)
                childIndex = GetChildIndexForRootItem(sequence);
            else
                childIndex = viewController.GetChildIndexForId(id);

            viewController.TryRemoveItem(id, false);
            var newItemData = GenerateDataItem(sequence);

            AddItem(newItemData, parentId, childIndex);
        }

        void OnSequencesRemoved()
        {
            RemoveBrokenMasterSequences();
            RemoveBrokenChildren();
            viewController.RebuildTree();
            RefreshItems();
        }

        void RemoveBrokenMasterSequences()
        {
            foreach (var id in viewController.GetRootItemIds())
            {
                var itemData = GetItemDataForId(id);

                if (itemData.timeline == null)
                    viewController.TryRemoveItem(id, false);
            }
        }

        void RemoveBrokenChildren(int id = -1)
        {
            var childrenIds = id == -1 ?
                viewController.GetRootItemIds().ToArray() :
                viewController.GetChildrenIds(id).ToArray();

            foreach (var childId in childrenIds)
            {
                var itemData = GetItemDataForId(childId);
                if (itemData.timeline == null)
                    TryRemoveChildren(childId);

                else
                    RemoveBrokenChildren(childId);
            }
        }

        void TryRemoveChildren(int id)
        {
            var childrenIds = viewController.GetChildrenIds(id).ToArray();
            foreach (var childId in childrenIds)
                viewController.TryRemoveItem(childId, false);
        }

        void OnMasterSequencesRemoved()
        {
            var legacyMasterSequences = MasterSequenceUtility.GetLegacyMasterSequences().ToList();
            var didRemoveItem = false;

            foreach (var id in viewController.GetRootItemIds())
            {
                var itemData = GetItemDataForId(id);

                if (!legacyMasterSequences.Exists(masterSequence => masterSequence.masterTimeline == itemData.timeline))
                    didRemoveItem |= viewController.TryRemoveItem(id, false);
            }

            if (didRemoveItem)
            {
                viewController.RebuildTree();
                RefreshItems();
            }
        }

        void InitializeRootItems()
        {
            var rootItems = GenerateDataTree();
            SetRootItems(rootItems);
        }

        List<TreeViewItemData<StructureItem>> GenerateDataTree()
        {
            var rootItems = new List<TreeViewItemData<StructureItem>>();

            foreach (var legacyMasterSequence in MasterSequenceUtility.GetLegacyMasterSequences())
            {
                var masterSequence = SequenceIndexer.instance.GetSequence(legacyMasterSequence.rootSequence.timeline);
                // The legacy data might return a MasterSequence that doesn't have a TimelineAsset.
                // In this case, the SequenceIndexer will always return null, we have to skip it as there's no
                // SequenceNode associated to it.
                if (masterSequence != null)
                    rootItems.Add(GenerateDataItem(masterSequence));
            }

            return rootItems;
        }

        TreeViewItemData<StructureItem> GenerateDataItem(SequenceNode sequence)
        {
            var id = sequence.timeline.GetHashCode();
            var childItems = new List<TreeViewItemData<StructureItem>>();

            foreach (var child in sequence.children)
                childItems.Add(GenerateDataItem(child));

            foreach (var invalidChild in sequence.GetEmptyClips())
                childItems.Add(GenerateDataItem(invalidChild, id));

            return new TreeViewItemData<StructureItem>(id, new StructureItem(sequence), childItems);
        }

        TreeViewItemData<StructureItem> GenerateDataItem(KeyValuePair<TimelineClip, GameObject> clip, int parentId)
        {
            var id = parentId + clip.Key.GetHashCode();
            return new TreeViewItemData<StructureItem>(id, new StructureItem(clip.Key, clip.Value));
        }

        StructureItem GetItemDataForId(int id)
        {
            return GetItemDataForId<StructureItem>(id);
        }

        StructureItem GetItemDataForIndex(int index)
        {
            return GetItemDataForIndex<StructureItem>(index);
        }

        internal void BeginItemCreation(int parentId = -1)
        {
            BeginItemCreation<StructureItem>(parentId);
        }

        int GetChildIndexForRootItem(SequenceNode sequence)
        {
            foreach (var rootId in GetRootIds())
            {
                var rootItem = GetItemDataForId(rootId);
                if (sequence.timeline.name.CompareTo(rootItem.displayName) <= 0)
                {
                    return viewController.GetChildIndexForId(rootId);
                }
            }

            return -1;
        }
    }
}
