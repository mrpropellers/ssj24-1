using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Forgive me!  Wanted to get something done, and UI Builder was slowing me down.  Some day!

namespace Presentation
{
    public class OldSchoolMenuController : MonoBehaviour
    {
        [SerializeField] float fadeOutTime = 3.0f;

        public void OnStartGameClicked()
        {
            Fader.Instance.FadeOut();

            Invoke("LoadMainScene", fadeOutTime);
        }

        public void LoadMainScene()
        {
            SceneManager.LoadScene(1);
        }

        public void OnExitGameClicked()
        {
#if UNITY_STANDALONE
            Application.Quit();
#endif
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

}