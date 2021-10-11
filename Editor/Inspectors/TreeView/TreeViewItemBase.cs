using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    internal abstract class EditorialElementTreeViewItem : TreeViewItemBase
    {
        internal MasterSequence masterSequence { get; set; }
        internal TimelineSequence timelineSequence => masterSequence?.manager.GetAt(sequenceIndex) as TimelineSequence;

        int sequenceIndex { get; set; }

        internal bool isTargetValid => GetTargetState().HasFlag(SequenceUtility.SequenceState.Valid);

        internal bool canRename =>
            SequenceUtility.GetSequenceEditionStatus(timelineSequence, masterSequence)
                .HasFlag(SequenceUtility.SequenceEditionStatus.CanRename);

        internal bool canDelete =>
            SequenceUtility.GetSequenceEditionStatus(timelineSequence, masterSequence)
                .HasFlag(SequenceUtility.SequenceEditionStatus.CanDelete);

        internal SequenceUtility.SequenceState GetTargetState()
        {
            var result = SequenceUtility.GetSequenceState(timelineSequence, masterSequence);

            if (result == SequenceUtility.SequenceState.MissingMasterSequence)
                (owner as StructureTreeView).Reload();

            return result;
        }

        internal void SetSequence(TimelineSequence existingSequence, MasterSequence existingMasterSequence)
        {
            masterSequence = existingMasterSequence;
            sequenceIndex = masterSequence.manager.GetIndex(existingSequence);
            state = State.Ok;
        }
    }

    internal abstract class TreeViewItemBase : TreeViewItem
    {
        public virtual Texture2D iconSelected { get; private set; }

        internal enum State
        {
            Creation,
            Ok
        }

        State m_State;

        public object owner;
        public State state
        {
            get => m_State;
            protected set => m_State = value;
        }

        protected TreeViewItemBase() : base()
        {
            state = State.Creation;
        }

        public abstract bool ValidateCreation(string newName);
        public abstract void ContextClicked();

        public abstract void DoubleClicked();

        public abstract void Selected();
        public virtual void Rename(string newName)
        {
            displayName = newName;
        }

        public abstract void Delete();
    }
}
