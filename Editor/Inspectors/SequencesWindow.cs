using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    internal class SequencesWindow : EditorWindow
    {
        static readonly string k_UXMLFilePath = "Packages/com.unity.sequences/Editor/UI/SequencesWindow.uxml";

        class Styles
        {
            public static readonly string k_StructureContentViewPath = "structure_content";
            public static readonly string k_AssetCollectionsContentViewPath = "asset_collections_content";
            public static readonly string k_SequencesWindowAddDropdownViewPath = "add_dropdown";
        }

        TreeViewState m_State;
        StructureTreeView m_Structure;
        TreeViewState m_AssetCollectionsState;
        AssetCollectionsTreeView m_AssetCollectionsTreeView;

        IMGUIContainer m_StructureTreeViewContainer;
        IMGUIContainer m_AssetCollectionsTreeViewContainer;

        SequencesWindowAddMenu m_MainMenu;

        internal StructureTreeView structureTreeView => m_Structure;

        void OnEnable()
        {
            titleContent = new GUIContent("Sequences", IconUtility.LoadIcon("MasterSequence/MasterSequence", IconUtility.IconType.UniqueToSkin));

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Minimum window size
            minSize = new Vector2(200.0f,  250.0f);

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXMLFilePath);
            visualTree.CloneTree(root);

            // Set style
            StyleSheetUtility.SetStyleSheets(root);
            StyleSheetUtility.SetIcon(root.Q<Label>(null, "seq-create-add-new"), "CreateAddNew");
            StyleSheetUtility.SetIcon(root.Q<Label>(null, "seq-dropdown"), "icon dropdown");

            // Header, search
            Button addDropdownButton = root.Q<Button>(Styles.k_SequencesWindowAddDropdownViewPath);

            // Hierarchy
            m_State = new TreeViewState();

            m_StructureTreeViewContainer = root.Q<IMGUIContainer>(Styles.k_StructureContentViewPath);
            m_Structure = new StructureTreeView(m_State, m_StructureTreeViewContainer);

            m_StructureTreeViewContainer.onGUIHandler = m_Structure.OnGUI;
            m_Structure.RefreshData();

            // Asset Collections
            m_AssetCollectionsState = new TreeViewState();

            m_AssetCollectionsTreeViewContainer = root.Q<IMGUIContainer>(Styles.k_AssetCollectionsContentViewPath);
            m_AssetCollectionsTreeView = new AssetCollectionsTreeView(m_AssetCollectionsState, m_AssetCollectionsTreeViewContainer);

            m_AssetCollectionsTreeViewContainer.onGUIHandler = m_AssetCollectionsTreeView.OnGUI;
            m_AssetCollectionsTreeView.RefreshData();

            // Popup menus
            m_MainMenu = new SequencesWindowAddMenu();
            m_MainMenu.userClickedOnCreateMasterSequence += m_Structure.CreateNewMasterSequence;
            m_MainMenu.userClickedOnCreateSequenceAsset += m_AssetCollectionsTreeView.CreateSequenceAssetInContext;

            addDropdownButton.clicked += m_MainMenu.Show;

            // todo: make alternative
            //EditorApplication.projectChanged += Refresh;
        }

        internal void Refresh()
        {
            m_Structure.RefreshData();
            m_AssetCollectionsTreeView.RefreshData();
        }
    }
}
