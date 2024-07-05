using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
#if UNITY_2023_3_OR_NEWER
    [UxmlElement]
    partial class SequenceAssetInstanceItem : SelectableScrollViewItem
#else
    class SequenceAssetInstanceItem : SelectableScrollViewItem
#endif
    {
#if !UNITY_2023_3_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SequenceAssetFoldoutItem, UxmlTraits> {}
#endif

        static readonly string ussClassName = "unity-asset-instance";

        TextField m_SequenceAssetInstanceNameField;
        VisualElement m_HeaderContainer;

        public SequenceAssetInstanceItem()
        {
            AddToClassList(ussClassName);

            m_HeaderContainer = new VisualElement();
            m_HeaderContainer.style.flexDirection = FlexDirection.Row;
            hierarchy.Add(m_HeaderContainer);

            m_SequenceAssetInstanceNameField = new TextField();
            m_SequenceAssetInstanceNameField.style.flexGrow = 1;
            m_SequenceAssetInstanceNameField.isReadOnly = true;
            m_HeaderContainer.Add(m_SequenceAssetInstanceNameField);
        }

        internal override void  BindItem(PlayableDirector director, GameObject assetInstance)
        {
            m_Director = director;
            m_SequenceAssetInstanceNameField.value = assetInstance.name;
        }

        public override void Dispose()
        {
        }
    }
}
