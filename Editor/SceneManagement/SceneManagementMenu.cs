using System.IO;
using UnityEngine;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    internal class SceneManagementMenu
    {
        internal class ContextInfo
        {
            public TimelineSequence sequence;
            public bool canCreateOrLoadScenes;
        }

        internal static void AppendMenuFrom(ContextInfo context, GenericMenu destinationMenu)
        {
            bool hasSequenceScenes = context.sequence.HasScenes();

            AddItem(destinationMenu, "Load Scenes", context.canCreateOrLoadScenes && hasSequenceScenes, false, LoadAllScenes, context);

            if (hasSequenceScenes)
            {
                if (!context.canCreateOrLoadScenes)
                    AddItem(destinationMenu, "Load specific Scene", false);

                else
                {
                    foreach (string path in context.sequence.GetRelatedScenes())
                    {
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        bool isLoaded = SceneManagement.IsLoaded(path);
                        AddItem(destinationMenu, $"Load specific Scene/{fileName}", !isLoaded, isLoaded, LoadScene, path);
                    }
                }
            }

            AddItem(destinationMenu, "Create Scene...", context.canCreateOrLoadScenes, false, AddNewScene, context);
        }

        static void AddItem(
            GenericMenu menu,
            string content,
            bool enabled,
            bool on = false,
            GenericMenu.MenuFunction2 func = null,
            object userData = null)
        {
            if (enabled)
                menu.AddItem(new GUIContent(content), on, func, userData);
            else
                menu.AddDisabledItem(new GUIContent(content), on);
        }

        static void LoadScene(object pathObject)
        {
            string path = pathObject as string;
            SceneManagement.OpenScene(path, true);
        }

        static void LoadAllScenes(object contextObject)
        {
            ContextInfo context = contextObject as ContextInfo;
            SceneManagement.OpenAllScenes(context.sequence, true);
        }

        static void AddNewScene(object contextObject)
        {
            ContextInfo context = contextObject as ContextInfo;
            SceneManagement.AddNewScene(context.sequence);
        }
    }
}
