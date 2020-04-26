using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    internal class MasterSequenceTreeViewItem : EditorialElementTreeViewItem, IEditorialDraggable
    {
        internal override TimelineSequence timelineSequence =>
            (state == State.Ok) ? masterSequence.rootSequence : null;

        public override Texture2D icon => IconUtility.LoadIcon("MasterSequence/MasterSequence", IconUtility.IconType.UniqueToSkin);
        public override Texture2D iconSelected => IconUtility.LoadIcon("MasterSequence/MasterSequence-selected", IconUtility.IconType.CommonToAllSkin);

        public MasterSequenceTreeViewItem()
            : base()
        {
            displayName = "New Master Sequence";
        }

        public bool CanBeParentedWith(TreeViewItem parent)
        {
            return !(parent is SequenceTreeViewItem) && !(parent is SubSequenceTreeViewItem);
        }

        public override void Selected()
        {
            if (GetTargetValidity() != SequenceUtility.SequenceValidity.Valid)
                return;

            SelectionUtility.TrySelectSequence(timelineSequence);
        }

        public override void ContextClicked()
        {
            if (masterSequence == null)
                return;

            MasterSequenceContextMenu.instance.isItemValid = GetTargetValidity() == SequenceUtility.SequenceValidity.Valid;
            MasterSequenceContextMenu.instance.Show(this);
        }

        public override void DoubleClicked()
        {
            if (GetTargetValidity() != SequenceUtility.SequenceValidity.Valid)
                return;

            SelectionUtility.TrySelectSequence(masterSequence.rootSequence);
            SelectionUtility.SelectTimeline(masterSequence.rootSequence.timeline);
        }

        public override void Rename(string newName)
        {
            if (GetTargetValidity() != SequenceUtility.SequenceValidity.Valid)
                return;

            if (masterSequence.Rename(newName))
                base.Rename(newName);
        }

        public void SetAsset(MasterSequence existingAsset)
        {
            masterSequence = existingAsset;
            state = State.Ok;
        }

        public override bool ValidateCreation(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                newName = displayName;

            displayName = newName;
            SequenceUtility.CreateMasterSequence(newName);

            state = State.Ok;
            return true;
        }

        public override void Delete()
        {
            if (masterSequence == null)
                return;

            if (!UserVerifications.ValidateSequenceDeletion(timelineSequence))
                return;

            masterSequence.Delete();
            (owner as StructureTreeView).RefreshData();
        }
    }
}
