using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    partial class AssetCollectionsTreeView
    {
        void PopulateContextMenuForAssetCollection(DropdownMenu menu, int index)
        {
            if (menu.MenuItems().Any())
                return;

            menu.AppendAction("Create Sequence Asset", CreateSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
        }

        void CreateSequenceAsset(DropdownMenuAction action)
        {
            var index = (int)action.userData;
            var id = GetIdForIndex(index);
            BeginItemCreation<AssetCollectionTreeViewItem>(id);
        }

        void PopulateContextMenuForSequenceAsset(DropdownMenu menu, int index)
        {
            if (menu.MenuItems().Any())
                return;

            menu.AppendAction("Create Variant", CreateSequenceAssetVariant, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendSeparator();
            menu.AppendAction("Open", OpenSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendAction("Rename", RenameSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendAction("Delete", DeleteSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
        }

        void PopulateContextMenuForSequenceAssetVariant(DropdownMenu menu, int index)
        {
            if (menu.MenuItems().Any())
                return;

            menu.AppendAction("Open", OpenSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendAction("Rename", RenameSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendAction("Duplicate", DuplicateSequenceAssetVariant, DropdownMenuAction.AlwaysEnabled, index);
            menu.AppendAction("Delete", DeleteSequenceAsset, DropdownMenuAction.AlwaysEnabled, index);
        }

        void CreateSequenceAssetVariant(DropdownMenuAction action)
        {
            var index = (int)action.userData;
            BeginItemCreation<AssetCollectionTreeViewItem>(viewController.GetIdForIndex(index));
        }

        void OpenSequenceAsset(DropdownMenuAction action)
        {
            var index = (int)action.userData;
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);
            AssetDatabase.OpenAsset(data.asset);
        }

        void RenameSequenceAsset(DropdownMenuAction action)
        {
            BeginRenameAtIndex((int)action.userData);
        }

        void DuplicateSequenceAssetVariant(DropdownMenuAction action)
        {
            var index = (int)action.userData;
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);

            SequenceAssetUtility.DuplicateVariant(data.asset);
        }

        void DeleteSequenceAsset(DropdownMenuAction action)
        {
            var index = (int)action.userData;
            var data = GetItemDataForIndex<AssetCollectionTreeViewItem>(index);

            if (!UserVerifications.ValidateSequenceAssetDeletion(data.asset))
                return;

            var parent = GetItemDataForId<AssetCollectionTreeViewItem>(GetParentIdForIndex(index));
            if (parent.treeViewItemType == AssetCollectionTreeViewItem.Type.Header)
                SequenceAssetUtility.DeleteSourceAsset(data.asset);
            else
                SequenceAssetUtility.DeleteVariantAsset(data.asset);
        }
    }
}
