using System;
using FMODUnity;
using NetCode;
using UnityEngine;


public class MusicManager : MonoBehaviour
{
    [SerializeField] private EventReference menuMusic;
    [SerializeField] private EventReference gameMusic;

    private static FMOD.Studio.EventInstance _menuMusicInstance;
    private static FMOD.Studio.EventInstance _gameMusicInstance;
    
    public static bool IsPlayingGameMusic { get; private set; }

    private void Awake()
    {
        _menuMusicInstance = RuntimeManager.CreateInstance(menuMusic);
        _gameMusicInstance = RuntimeManager.CreateInstance(gameMusic);
        _menuMusicInstance.start();
    }

    // TODO | P4 | Tech Debt | Refactor MusicManager to be event-driven
    //  We shouldn't need to poll state here, but instead subscribe to a (currently non-existent)
    //  event broadcaster which lets us know when game state has changed.
    void Update()
    {
        if (IsPlayingGameMusic && !EntityWorlds.GameplayIsUnderway)
        {
            PlayMenuMusic();
        }
        else if (!IsPlayingGameMusic && EntityWorlds.GameplayIsUnderway)
        {
            PlayGameMusic();
        }
    }

    public static void PlayGameMusic()
    {
        _menuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        //_menuMusicInstance.release();

        //_gameMusicInstance = RuntimeManager.CreateInstance(_instance.gameMusic);
        _gameMusicInstance.start();
        IsPlayingGameMusic = true;
    }
    
    public static void PlayMenuMusic()
    {
        _gameMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        //_gameMusicInstance.release();

        //_menuMusicInstance = RuntimeManager.CreateInstance(_instance.menuMusic);
        _menuMusicInstance.start();
        IsPlayingGameMusic = false;
    }
}
