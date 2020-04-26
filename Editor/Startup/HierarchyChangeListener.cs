using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    /// <summary>
    /// Class in charge of reflecting changes from the Hierarchy window
    /// to the data.
    /// </summary>
    [InitializeOnLoad]
    class HierarchyChangeListener
    {
        static HierarchyChangeListener()
        {
            EditorApplication.hierarchyChanged += UpdateMasterSequence;
        }

        static void UpdateMasterSequence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var sequenceFilters = Resources.FindObjectsOfTypeAll(typeof(SequenceFilter));
            foreach (var sequenceFilterObj in sequenceFilters)
            {
                var sequenceFilter = sequenceFilterObj as SequenceFilter;
                if (sequenceFilter == null || sequenceFilter.masterSequence == null) continue;

                // Process names
                var sequence = sequenceFilter.masterSequence.manager.GetAt(sequenceFilter.elementIndex) as TimelineSequence;
                if (sequence != null && sequenceFilter.gameObject.name != sequence.name)
                {
                    if (sequence == sequenceFilter.masterSequence.rootSequence)
                        sequenceFilter.masterSequence.Rename(sequenceFilter.gameObject.name);
                    else
                        sequence.Rename(sequenceFilter.gameObject.name);
                }
            }
        }
    }
}
