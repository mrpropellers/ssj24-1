using UnityEngine;

namespace NetCode
{
    public class EnabledOnlyForDevBuild : MonoBehaviour
    {
        public void Start()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
                gameObject.SetActive(false);
        }
    }
}
