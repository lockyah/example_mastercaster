using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class gameHandlerHelper : MonoBehaviour
{
    private gameHandler gh;

    //A helper for functions that need to be called from the Pause menu.
    //Mainly for GameHandler, but also handles the pause volume settings via the GameHandler's persistent data.

    private Slider sfx, mus;
    private TMP_Text sfxT, musT;


    void Start()
    {
        gh = GameObject.Find("GameCanvas").GetComponent<gameHandler>();

        //VOLUME - Grab current volumes from PlayerPrefs and set the sliders/volumes accordingly
        sfx = GameObject.Find("SFX Slider").GetComponent<Slider>();
        mus = GameObject.Find("Music Slider").GetComponent<Slider>();
        sfxT = sfx.gameObject.transform.Find("Volume").GetComponent<TMP_Text>();
        musT = mus.gameObject.transform.Find("Volume").GetComponent<TMP_Text>();

        mus.value = gh.persist.getVolumes()[0];
        sfx.value = gh.persist.getVolumes()[1];
        adjustVolumes();
    }

    //Used by sliders when adjusted. Calls SMH to adjust volume accordingly
    //Due to being in TimeScale 0 during a pause, this only updates Persistent data when the menu closes to resume or quit
    public void adjustVolumes()
    {
        sfxT.text = ((int)sfx.value).ToString();
        musT.text = ((int)mus.value).ToString();

        gh.sounds.setMaxVolumes(mus.value / 100, sfx.value / 100);

        gh.persist.updateVolume(mus.value, sfx.value);
    }

    public void pauseTrigger(string name)
    {
        gh.pauseTrigger(name);
    }

    public void exitGame()
    {
        gh.exitGame();
    }

    public void gameOver()
    {
        gh.gameOver();
    }

    public void safeGameOver()
    {
        gh.safeGameOver();
    }
}
