using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace UnityEditor.Sequences
{
    [PackageHelpURL("sequence-assembly-window")]
    partial class SequenceAssemblyWindow : BaseEditorWindow
    {
        SequenceAssemblyInspector m_CachedEditor;
        TextField m_SequenceNameTextField;
        bool m_PlayMode;

        class Styles
        {
            public static readonly string k_AssemblyStyleSheetName = "SequenceAssemblyInspector";
        }

        /// <summary>
        /// Called by OnEnable sets the view
        /// </summary>
        protected override void SetupView()
        {
            base.SetupView();

            StyleSheetUtility.SetStyleSheets(rootVisualElement, Styles.k_AssemblyStyleSheetName);
            titleContent = new GUIContent("Sequence Assembly", IconUtility.LoadIcon("MasterSequence/Shot", IconUtility.IconType.UniqueToSkin));

            m_SequenceNameTextField = new TextField
            {
                label = "Name",
                bindingPath = "m_Name",
                focusable = false
            };

            SetHeaderContent(m_SequenceNameTextField);

            m_PlayMode = EditorApplication.isPlayingOrWillChangePlaymode;

            SelectionUtility.playableDirectorChanged += ShowSelection;
            ShowSelection();

            EditorApplication.playModeStateChanged += Rebuild;
        }

        void OnDisable()
        {
            SelectionUtility.playableDirectorChanged -= ShowSelection;
            EditorApplication.playModeStateChanged -= Rebuild;
            ClearView();
        }

        void Rebuild(PlayModeStateChange stateChange)
        {
            rootVisualElement.Clear();

            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                m_PlayMode = true;
            }
            else if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                m_PlayMode = false;
            }

            LoadUIData();
            SetupView();
        }

        void ShowSelection()
        {
            var director = SelectionUtility.activePlayableDirector;
            if (director == null || director.playableAsset == null)
                return;

            // The new PlayableDirector selected is already shown or is not the one of a Sequence so there is no
            // need to change the view.
            if (IsAlreadyShown(director) || director.gameObject.GetComponent<SequenceFilter>() == null)
                return;

            ClearView();
            CreateView(director);
        }

        bool IsAlreadyShown(PlayableDirector target)
        {
            return (m_CachedEditor && m_CachedEditor.target == target);
        }

        void CreateView(PlayableDirector data)
        {
            if (!m_PlayMode)
                m_CachedEditor = SequenceAssemblyInspector.CreateEditor(data, typeof(SequenceAssemblyInspector)) as SequenceAssemblyInspector;
            else
                m_CachedEditor = SequenceAssemblyInspector.CreateEditor(data, typeof(SequenceAssemblyPlayModeInspector)) as SequenceAssemblyPlayModeInspector;

            rootVisualElement.Bind(new SerializedObject(data.playableAsset as TimelineAsset));

            rootVisualElement.Add(m_CachedEditor.CreateInspectorGUI());
        }

        void ClearView()
        {
            if (m_CachedEditor && m_CachedEditor.m_RootVisualElement != null)
            {
                rootVisualElement.Remove(m_CachedEditor.m_RootVisualElement);
                rootVisualElement.Unbind();
                m_SequenceNameTextField.value = "";
                DestroyImmediate(m_CachedEditor);
            }
        }

        void OnFocus()
        {
            if (m_CachedEditor != null)
                m_CachedEditor.SelectPlayableDirector();
        }

        void OnHierarchyChange()
        {
            // This callback is called even when nothing else than the selection changed in the Hierarchy.
            // This means that `m_CachedEditor.Refresh` is called way too often. This causes UX discomfort in some
            // selection scenario.
            if (m_CachedEditor != null)
            {
                var director = SelectionUtility.activePlayableDirector;
                if (director == null)
                    ClearView();
                else
                    m_CachedEditor.Refresh();
            }
        }
    }
}
