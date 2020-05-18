using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    internal abstract class EditorialElementTreeViewItem : TreeViewItemBase
    {
        internal MasterSequence masterSequence { get; set; }
        internal abstract TimelineSequence timelineSequence { get; }

        internal SequenceUtility.SequenceValidity GetTargetValidity()
        {
            var result = SequenceUtility.GetSequenceValidity(timelineSequence, masterSequence);

            if (result == SequenceUtility.SequenceValidity.MissingMasterSequence)
                (owner as StructureTreeView).Reload();

            return result;
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
