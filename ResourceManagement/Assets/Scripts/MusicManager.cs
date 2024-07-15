using FMODUnity;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    static MusicManager _instance;
    
    [SerializeField] private EventReference menuMusic;
    [SerializeField] private EventReference gameMusic;

    private static FMOD.Studio.EventInstance _menuMusicInstance;
    private static FMOD.Studio.EventInstance _gameMusicInstance;
    
    public static bool IsPlayingGameMusic { get; private set; }

    private void Awake()
    {
        _instance = this;
        _menuMusicInstance = RuntimeManager.CreateInstance(menuMusic);
        _menuMusicInstance.start();
    }

    public static void PlayGameMusic()
    {
        _menuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _menuMusicInstance.release();

        _gameMusicInstance = RuntimeManager.CreateInstance(_instance.gameMusic);
        _gameMusicInstance.start();
        IsPlayingGameMusic = true;
    }
    
    public static void PlayMenuMusic()
    {
        _gameMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _gameMusicInstance.release();

        _menuMusicInstance = RuntimeManager.CreateInstance(_instance.menuMusic);
        _menuMusicInstance.start();
        IsPlayingGameMusic = false;
    }
}
