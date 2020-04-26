using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Sequences;
using UnityEngine.Sequences.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEditor.Sequences
{
    internal static class TimelineUtility
    {
        static MethodInfo s_TimelineWindowSetCurrentTimelineMethod;

        static MethodInfo timelineWindowSetCurrentTimelineMethod
        {
            get
            {
                if (s_TimelineWindowSetCurrentTimelineMethod == null)
                {
                    try
                    {
                        var window = TimelineEditor.GetWindow();
                        if (window == null)
                            return null;

                        s_TimelineWindowSetCurrentTimelineMethod = window.GetType().GetMethod("SetCurrentTimeline", new Type[] { typeof(PlayableDirector), typeof(TimelineClip) });
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Exception {0}", e.Message);
                    }
                }
                return s_TimelineWindowSetCurrentTimelineMethod;
            }
        }

        /// <summary>
        /// Check if all reflected methods and properties are available.
        /// Returns true if developer can use breadcrumb. Otherwise, returns false.
        /// In that case, Timeline's API might have changed.
        /// </summary>
        internal static bool breadcrumbIsAvailable => timelineWindowSetCurrentTimelineMethod != null;

        internal static TimelinePath breadcrumb = new TimelinePath();

        internal class TimelinePath
        {
            internal struct Element
            {
                public PlayableDirector director;
                public TimelineClip hostClip;
            }
            Stack<Element> path;

            internal int count => path.Count;

            public TimelinePath()
            {
                path = new Stack<Element>();
            }

            public Element Pop()
            {
                return path.Pop();
            }

            public void Clear()
            {
                path.Clear();
            }

            public void BuildAndAppend(TimelineSequence destinationClip)
            {
                TimelineSequence current = destinationClip;
                while (current != null)
                {
                    if (current.parent != null)
                    {
                        var track = (current.parent as TimelineSequence).childrenTrack;
                        if (track == null)
                            return;

                        var hostClip = track.GetFirstClipWithName(current.name);
                        if (hostClip == null)
                            return;

                        path.Push(new Element() { director = current.timeline.FindDirector(), hostClip = hostClip });
                    }
                    else
                        path.Push(new Element() { director = current.timeline.FindDirector(), hostClip = null });

                    current = current.parent as TimelineSequence;
                }
            }

            public void Append(PlayableDirector director, TimelineClip hostClip)
            {
                path.Push(new Element() { director = director, hostClip = hostClip });
            }
        }

        static void PushItemIntoBreadcrumb(PlayableDirector director, TimelineClip hostClip)
        {
            var window = TimelineEditor.GetWindow();
            if (window == null)
                return;

            timelineWindowSetCurrentTimelineMethod?.Invoke(window, new object[] { director, hostClip });
        }

        public static void RefreshBreadcrumb(TimelinePath path = null)
        {
            if (path == null)
                path = breadcrumb;

            TimelinePath.Element parent = default;
            while (path.count > 0)
            {
                TimelinePath.Element drillInClip = path.Pop();
                if (parent.director != null)
                {
                    PushItemIntoBreadcrumb(drillInClip.director, drillInClip.hostClip);
                }
                else
                {
                    // Add a PlayableDirectorInternalState only on the master timeline.
                    if (!drillInClip.director.GetComponent<PlayableDirectorInternalState>())
                        Undo.AddComponent<PlayableDirectorInternalState>(drillInClip.director.gameObject);

                    PushItemIntoBreadcrumb(drillInClip.director, null);
                }
                parent = drillInClip;
            }

            if (TimelineEditor.masterDirector
                && TimelineEditor.masterDirector.TryGetComponent<PlayableDirectorInternalState>(out PlayableDirectorInternalState component))
            {
                component.RestoreTimeState();
                TimelineEditor.Refresh(RefreshReason.ContentsModified);
            }
        }
    }
}
