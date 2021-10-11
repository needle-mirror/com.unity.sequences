using UnityEngine;

namespace UnityEditor.Sequences
{
    internal class SequenceContextMenu : SequencesWindowContextMenu<SequenceContextMenu, EditorialElementTreeViewItem>
    {
        GenericMenu m_Menu;

        bool isValid { get; set; }
        bool canRename { get; set; }
        bool canDelete { get; set; }
        bool canCreate { get; set; }
        bool canManipulate { get; set; }

        new EditorialElementTreeViewItem target { get; set; }

        protected override void SetTarget(EditorialElementTreeViewItem newTarget)
        {
            target = newTarget;

            var editionStatus = SequenceUtility.GetSequenceEditionStatus(target.timelineSequence, target.masterSequence);

            canRename = editionStatus.HasFlag(SequenceUtility.SequenceEditionStatus.CanRename);
            canDelete = editionStatus.HasFlag(SequenceUtility.SequenceEditionStatus.CanDelete);
            canCreate = editionStatus.HasFlag(SequenceUtility.SequenceEditionStatus.CanCreate);
            canManipulate = editionStatus.HasFlag(SequenceUtility.SequenceEditionStatus.CanManipulate);
            isValid = target.isTargetValid;
        }

        public override void Show(EditorialElementTreeViewItem targetItem)
        {
            SetTarget(targetItem);
            m_Menu = new GenericMenu();
            PopulateMenu();
            m_Menu.ShowAsContext();
        }

        void AddItem(string content, bool enabled = true, GenericMenu.MenuFunction func = null)
        {
            if (enabled)
                m_Menu.AddItem(new GUIContent(content), false, func);
            else
                m_Menu.AddDisabledItem(new GUIContent(content), false);
        }

        void PopulateMenu()
        {
            if (isValid)
                PopulateMenuBase();
            else
                PopulateMenuInvalidItem();
        }

        void PopulateMenuBase()
        {
            var context = new SceneManagementMenu.ContextInfo();
            context.sequence = target.timelineSequence;
            context.canCreateOrLoadScenes = canManipulate;

            if (!(target is SubSequenceTreeViewItem))
            {
                AddItem("Create Sequence", canCreate, CreateSequenceAction);
                m_Menu.AddSeparator("");
            }

            // Sequence manipulation
            AddItem("Rename", canRename, RenameAction);
            AddItem("Delete", canDelete, DeleteAction);
            m_Menu.AddSeparator("");

            // Sequence scenes
            SceneManagementMenu.AppendMenuFrom(context, m_Menu);
            m_Menu.AddSeparator("");

            // Sequence recording
            AddItem("Record", canManipulate, RecordAction);
            AddItem("Record As...", canManipulate, RecordAsAction);
        }

        void PopulateMenuInvalidItem()
        {
            m_Menu.AddItem(new GUIContent("Delete"), false, DeleteAction);
        }

        void CreateSequenceAction()
        {
            if (target is MasterSequenceTreeViewItem)
                (target.owner as StructureTreeView).CreateNewSequenceInContext(target as MasterSequenceTreeViewItem);

            else if (target is SequenceTreeViewItem)
                (target.owner as StructureTreeView).CreateNewSubSequenceInContext(target as SequenceTreeViewItem);

            ResetTarget();
        }

        void RenameAction()
        {
            (target.owner as StructureTreeView).BeginRename(target);
            ResetTarget();
        }

        void DeleteAction()
        {
            target.Delete();
            ResetTarget();
        }

        void RecordAction()
        {
            target.timelineSequence.Record();
            ResetTarget();
        }

        void RecordAsAction()
        {
            target.timelineSequence.Record(true);
            ResetTarget();
        }
    }
}
