using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Forgive me!  Wanted to get something done, and UI Builder was slowing me down.  Some day!

namespace Presentation
{
    public class OldSchoolMenuController : MonoBehaviour
    {
        List<AsyncOperation> m_SceneLoaders;
        bool m_IsLoadingMain;
        
        [SerializeField] float fadeOutTime = 3.0f;

        void Start()
        {
            StartSceneLoads();
        }

        public void OnStartGameClicked()
        {
            if (m_IsLoadingMain)
            {
                Debug.Log("Already loading main. Clicking again does nothing.");
                return;
            }

            m_IsLoadingMain = true;
            StartCoroutine(LoadMainScene());
        }

        void StartSceneLoads()
        {
            SceneManager.LoadSceneAsync(4, LoadSceneMode.Additive);
            
            m_SceneLoaders = new List<AsyncOperation>();
            for (var i = 1; i < 4; ++i)
            {
                var load = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
                load.allowSceneActivation = false;
                m_SceneLoaders.Add(load);
            }
        }

        public IEnumerator LoadMainScene()
        {
            yield return Fader.Instance.FadeOut();
            
            //yield return new WaitForSeconds(fadeOutTime);
            for (var i = 0; i < m_SceneLoaders.Count; i++)
            {
                m_SceneLoaders[i].allowSceneActivation = true;
            }

            SceneManager.UnloadSceneAsync(0);
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