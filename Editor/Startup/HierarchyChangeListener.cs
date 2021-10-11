using System;
using UnityEngine.Sequences;
#if UNITY_2021_2_OR_NEWER
using PrefabStage = UnityEditor.SceneManagement.PrefabStage;
#else
using PrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStage;
#endif

namespace UnityEditor.Sequences
{
    /// <summary>
    /// Class in charge of reflecting changes from the Hierarchy window
    /// to the data.
    /// </summary>
    [InitializeOnLoad]
    class HierarchyChangeListener
    {
        internal static event Action sequencePrefabInstantiated;

        static HierarchyChangeListener()
        {
            EditorApplication.hierarchyChanged += UpdateMasterSequence;
            PrefabStage.prefabStageOpened += SequencePrefabOpened;
        }

        static void UpdateMasterSequence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var sequenceFilters = ObjectsCache.FindObjectsFromScenes<SequenceFilter>();
            foreach (var sequenceFilter in sequenceFilters)
            {
                if (sequenceFilter.masterSequence == null)
                    continue;

                // Process names
                var sequence = sequenceFilter.masterSequence.manager.GetAt(sequenceFilter.elementIndex) as TimelineSequence;
                if (sequence != null && sequenceFilter.gameObject.name != sequence.name)
                {
                    if (sequence == sequenceFilter.masterSequence.rootSequence)
                        sequenceFilter.masterSequence.Rename(sequenceFilter.gameObject.name);
                    else
                        sequence.Rename(sequenceFilter.gameObject.name);
                }

                // Process instantiation of "prefabized" sequences.
                var parent = sequenceFilter.transform.parent;
                if (parent == null || parent.GetComponent<SequenceFilter>() == null)
                {
                    sequenceFilter.gameObject.SetActive(true);
                    sequencePrefabInstantiated?.Invoke();
                }
            }
        }

        static void SequencePrefabOpened(PrefabStage stage)
        {
            var root = stage.prefabContentsRoot;
            if (root.GetComponent<SequenceFilter>() != null)
                root.SetActive(true);
        }
    }
}
