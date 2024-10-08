using System;
using System.Linq;

namespace UnityEngine.UIElements
{
#if UNITY_2023_3_OR_NEWER
    [UxmlElement]
    partial class SelectableScrollView : ScrollView
#else
    class SelectableScrollView : ScrollView
#endif
    {
        public new static readonly string ussClassName = "seq-list-content";
        public static readonly string itemUssClassName = "seq-list-item";
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";
#if !UNITY_2023_3_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SelectableScrollView, UxmlTraits> {}
#endif

        public static Action<SelectableScrollView, SelectableScrollViewItem> itemSelected;

        SelectableScrollViewItem m_SelectedItem;

        public SelectableScrollView()
        {
            AddToClassList(ussClassName);
        }

        public void AddItem(SelectableScrollViewItem item)
        {
            Add(item);
            item.AddToClassList(itemUssClassName);
            item.itemSelected += OnItemSelected;
        }

        void OnItemSelected(SelectableScrollViewItem selectedItem)
        {
            m_SelectedItem?.SetSelected(false);
            if (m_SelectedItem == null || m_SelectedItem != selectedItem)
            {
                m_SelectedItem = selectedItem;
                itemSelected?.Invoke(this, selectedItem);
            }
            else
                m_SelectedItem = null;
        }

        public void ClearSelection()
        {
            m_SelectedItem?.SetSelected(false);
            m_SelectedItem = null;
        }

        public int selectedIndex => contentContainer.Children().ToList().FindIndex(element => element == m_SelectedItem);

        public void Refresh()
        {
            Clear();
        }
    }
}
