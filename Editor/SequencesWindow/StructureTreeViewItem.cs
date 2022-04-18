using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Sequences
{
    partial class StructureTreeView
    {
        /// <summary>
        /// The StructureItem allows to store the SequenceNode important information to be able to display invalid
        /// items as well as valid ones.
        /// </summary>
        struct StructureItem
        {
            internal TimelineAsset timeline;
            internal TimelineClip editorialClip;
            internal GameObject gameObject;

            internal StructureItem(SequenceNode sequence)
            {
                timeline = sequence.timeline;
                editorialClip = sequence.editorialClip;
                gameObject = sequence.gameObject;
            }

            internal StructureItem(TimelineClip editorialClip, GameObject gameObject)
            {
                this.timeline = null;
                this.editorialClip = editorialClip;
                this.gameObject = gameObject;
            }

            internal bool IsNull()
            {
                return timeline == null && editorialClip == null && gameObject == null;
            }

            internal string displayName
            {
                get
                {
                    if (timeline != null)
                        return timeline.name;

                    if (editorialClip != null)
                        return editorialClip.displayName;

                    if (gameObject != null)
                        return gameObject.name;

                    return string.Empty;
                }
            }
        }
    }
}
