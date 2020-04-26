using System;
using UnityEngine.Sequences;

namespace UnityEditor.Sequences
{
    [CustomEditor(typeof(SequenceAsset))]
    public class SequenceAssetInspector : Editor
    {
        SerializedProperty m_Type;

        void OnEnable()
        {
            m_Type = serializedObject.FindProperty("m_Type");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var options = CollectionType.instance.GetTypes();
            var selected = Array.IndexOf(options, m_Type.stringValue);
            selected = EditorGUILayout.Popup("Type", selected, options);
            m_Type.stringValue = options[selected];

            serializedObject.ApplyModifiedProperties();
        }
    }
}
