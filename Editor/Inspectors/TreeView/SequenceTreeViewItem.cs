using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    internal class SequenceTreeViewItem : EditorialElementTreeViewItem, IEditorialDraggable
    {
        public override Texture2D icon => IconUtility.LoadIcon("MasterSequence/Sequence", IconUtility.IconType.UniqueToSkin);
        public override Texture2D iconSelected => IconUtility.LoadIcon("MasterSequence/Sequence-selected", IconUtility.IconType.CommonToAllSkin);

        public SequenceTreeViewItem()
        {
            displayName = "New Sequence";
        }

        public bool CanBeParentedWith(TreeViewItem parent)
        {
            return (parent is MasterSequenceTreeViewItem);
        }

        public override void Selected()
        {
            if (!isTargetValid)
                return;

            SelectionUtility.TrySelectSequence(timelineSequence);
        }

        public override void ContextClicked()
        {
            // TODO: this could be replace by 'isOrphan'?
            if (!(parent as MasterSequenceTreeViewItem).isTargetValid)
                return;

            SequenceContextMenu.instance.Show(this);
        }

        public override void DoubleClicked()
        {
            if (!isTargetValid)
                return;

            SelectionUtility.TrySelectSequence(timelineSequence);
            SelectionUtility.SelectTimeline(timelineSequence.timeline);
        }

        public override void Rename(string newName)
        {
            if (!canRename)
                return;

            base.Rename(newName);
            timelineSequence.Rename(newName);
        }

        public override bool ValidateCreation(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                newName = displayName;

            MasterSequence masterSequenceAsset = (parent as MasterSequenceTreeViewItem).masterSequence;

            SetSequence(SequenceUtility.CreateSequence(newName, masterSequenceAsset, masterSequenceAsset.rootSequence), masterSequenceAsset);
            displayName = timelineSequence.name;
            id = SequenceUtility.GetHashCode(timelineSequence, masterSequence);
            return true;
        }

        public override void Delete()
        {
            if (!canDelete)
                return;

            if (!UserVerifications.ValidateSequenceDeletion(timelineSequence))
                return;

            MasterSequence masterSequenceAsset = (parent as MasterSequenceTreeViewItem).masterSequence;
            SequenceUtility.DeleteSequence(timelineSequence, masterSequenceAsset);
        }
    }
}
