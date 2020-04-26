using System;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace UnityEngine.Sequences.Timeline
{
    [Serializable]
    [TrackClipType(typeof(SceneActivationPlayableAsset))]
    [TrackColor(0.55f, 0.5f, 0.14f)]
    public class SceneActivationTrack : TrackAsset
    {
        public SceneReference scene;

        List<GameObject> m_Buffer = new List<GameObject>();

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            ScriptPlayable<SceneActivationMixer> mixer = ScriptPlayable<SceneActivationMixer>.Create(graph, inputCount);
            mixer.GetBehaviour().SetData(scene);
            return mixer;
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            Scene loadedScene = SceneActivationManager.GetScene(scene.path);
            if (!loadedScene.isLoaded)
                return;

            m_Buffer.Clear();

            // TODO: this should be defined by the SceneActivationBehaviour.
            loadedScene.GetRootGameObjects(m_Buffer);

            foreach (GameObject root in m_Buffer)
                driver.AddFromName(root, "m_IsActive");
        }
    }
}
