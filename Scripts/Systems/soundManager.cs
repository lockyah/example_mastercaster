using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundManager : MonoBehaviour
{
    private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>(); //Dictionary of sounds and their names to call them

    //COROUTINE - For pitch changes
    Coroutine musicPitch;

    //SOUND SETTINGS - Volumes from Settings Menu
    float bgmVolume = 0.8f;
    float sfxVolume = 1f;
    float workingVolume = 1f; //Reference for current volume setting in-game

    //CONSISTENT SOUNDS - Sounds that will always be playing
    public AudioSource bgMusic; //BGM
    public AudioSource cauldronBubble; //SFX - automatically using correct sound

    //ON-DEMAND SOUNDS - Variable Audio Sources that are called by functions when needed.
    AudioSource[] sounds = new AudioSource[5];

    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        //If there's another Sound Manager object in the scene (from another scene), delete this one
        if(GameObject.Find("SoundManager") != gameObject)
        {
            Destroy(gameObject);
        } else
        {
            //Create array of SFX audio sources and keep references
            for (int i = 0; i < sounds.Length; i++)
            {
                sounds[i] = gameObject.AddComponent<AudioSource>();
            }

            //Pre-load sounds to avoid overhead during the game
            foreach (AudioClip ac in Resources.LoadAll<AudioClip>("Sound/"))
            {
                soundDict.Add(ac.name, ac); //Add the sound with its name as key
            }
        }   
    }

    //Used in settings menu to set top volume
    //Also used from title screen when this and persistent first load
    public void setMaxVolumes(float bgm, float sfx)
    {
        bgmVolume = bgm;
        sfxVolume = sfx;

        setCurrentVolumes(workingVolume);
    }

    //Used in animation events to change volumes proportional to the maximum volume
    public void setCurrentVolumes(float vol)
    {
        workingVolume = vol; //Store the last volume setting used to allow re-adjusting max volumes

        bgMusic.volume = vol * bgmVolume;
        cauldronBubble.volume = vol * sfxVolume;

        foreach(AudioSource a in sounds)
        {
            a.volume = vol * sfxVolume;
        }
    }

    public void toggleCauldron()
    {
        //Used when entering or exiting the game scene
        if (!cauldronBubble.isPlaying)
        {
            cauldronBubble.Play();
        } else
        {
            cauldronBubble.Stop();
        }
    }

    public void requestSound(string name)
    {
        AudioClip clip = null; //Empty variable, filled by the switch and given to an available audio source after
        
        //Switch case for randomised sounds
        switch (name)
        {
            case "random_splash":
                clip = soundDict["sfx_splash_" + Random.Range(1, 3)];
                break;
            case "random_pop":
                clip = soundDict["sfx_pop_" + Random.Range(1, 6)];
                break;
            default:
                clip = soundDict[name];
                break;
        }

        //Find an open audio source, set the new sound, and play!
        foreach(AudioSource source in sounds)
        {
            if (!source.isPlaying)
            {
                source.clip = clip;
                source.Play();
                break;
            }
        }
    }

    public void changeMusic(string name)
    {
        bgMusic.Stop();

        if(name == "none")
        {
            bgMusic.clip = null;
        } else
        {
            bgMusic.clip = soundDict[name];
        }

        bgMusic.Play();
    }

    //Changes pitch of music to be higher as the player loses lives
    public void changeMusicPitch(float pitch)
    {
        if(musicPitch != null)
        {
            StopCoroutine(musicPitch);
        }

        if(pitch == 1)
        {
            //If setting to one, do immediately (only in Game Over)
            bgMusic.pitch = pitch;
        } else
        {
            //Otherwise, smooth it
            musicPitch = StartCoroutine(changePitch(pitch));
        }
    }

    private IEnumerator changePitch(float p)
    {

        while(bgMusic.pitch != p)
        {
            bgMusic.pitch = Mathf.Lerp(bgMusic.pitch, p, 0.55f);
            yield return new WaitForSeconds(0.25f);
        }
        
        yield return null;
    }
}
