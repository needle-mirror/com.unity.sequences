using System.Linq;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    [PackageHelpURL("sequences-window")]
    internal class SequencesWindow : TreeViewEditorWindow<StructureTreeView>
    {
        internal const string k_CreateMasterSequenceMenuActionName = "Create Master Sequence";
        internal const string k_CreateSequenceMenuActionName = "Create Sequence";

        protected override void SetupView()
        {
            base.SetupView();

            titleContent = new GUIContent(
                "Sequences",
                IconUtility.LoadIcon("MasterSequence/MasterSequence", IconUtility.IconType.UniqueToSkin));

            AddManipulator(new ContextualMenuManipulator(OnContextMenuClick));
        }

        protected override string GetAddMenuTooltip()
        {
            return "Create a new Sequence or Master Sequence";
        }

        protected override void PopulateAddMenu(DropdownMenu menu, bool contextual = false)
        {
            menu.AppendAction(k_CreateMasterSequenceMenuActionName, CreateMasterSequenceAction);

            if (!contextual)
                menu.AppendAction(k_CreateSequenceMenuActionName, CreateSequenceAction, CreateSequenceActionStatus);
        }

        void CreateMasterSequenceAction(DropdownMenuAction action)
        {
            BeginSequenceCreation();
        }

        void CreateSequenceAction(DropdownMenuAction action)
        {
            BeginSequenceCreation(treeView.selectedIndex);
        }

        DropdownMenuAction.Status CreateSequenceActionStatus(DropdownMenuAction action)
        {
            if (treeView.selectedIndex == -1 || treeView.selectedIndices.Count() > 1)
                return DropdownMenuAction.Status.Disabled;

            return treeView.GetCreateSequenceActionStatus(treeView.selectedIndex);
        }

        void BeginSequenceCreation(int parentIndex = -1)
        {
            var parentId = treeView.GetIdForIndex(parentIndex);
            treeView.BeginItemCreation(parentId);
        }

        void OnContextMenuClick(ContextualMenuPopulateEvent evt)
        {
            PopulateAddMenu(evt.menu, true);
        }
    }
}
