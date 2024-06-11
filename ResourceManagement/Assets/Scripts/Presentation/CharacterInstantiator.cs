using UnityEngine;

namespace Presentation 
{
    public class CharacterInstantiator : MonoBehaviour
    {
        static CharacterInstantiator _instance;

        [SerializeField]
        GameObject CharacterPrefab;
        
        static CharacterInstantiator Instance
        {
            get
            {
                if (ReferenceEquals(null, _instance))
                {
                    _instance = FindObjectOfType<CharacterInstantiator>();
                }

                return _instance;
            }
        }

        public static GameObject CreateCharacterPresentation()
        {
            return Instantiate(Instance.CharacterPrefab);
        }
    }
}
