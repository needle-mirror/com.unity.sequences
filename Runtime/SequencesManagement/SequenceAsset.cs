namespace UnityEngine.Sequences
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class SequenceAsset : MonoBehaviour
    {
        [SerializeField] string m_Type = "Character";

        public string type
        {
            get => m_Type;
            set => m_Type = value;
        }
    }
}
