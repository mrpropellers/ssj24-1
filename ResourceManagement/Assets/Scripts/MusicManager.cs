using FMODUnity;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private EventReference menuMusic;
    [SerializeField] private EventReference gameMusic;

    private FMOD.Studio.EventInstance _menuMusicInstance;
    private FMOD.Studio.EventInstance _gameMusicInstance;

    private void Awake()
    {
        _menuMusicInstance = RuntimeManager.CreateInstance(menuMusic);
        _menuMusicInstance.start();
    }

    public void PlayGameMusic()
    {
        _menuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _menuMusicInstance.release();

        _gameMusicInstance = RuntimeManager.CreateInstance(gameMusic);
        _gameMusicInstance.start();
    }
    
    public void PlayMenuMusic()
    {
        _gameMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _gameMusicInstance.release();

        _menuMusicInstance = RuntimeManager.CreateInstance(menuMusic);
        _menuMusicInstance.start();
    }
}
