using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundManagerHelper : MonoBehaviour
{
    //An intermediary script attached to anything that needs to make a sound.
    //Relays any function called by scripts or animations to the sound manager.

    soundManager sM;

    void Awake()
    {
        sM = GameObject.Find("SoundManager").GetComponent<soundManager>();
    }

    public void toggleCauldron()
    {
        sM.toggleCauldron();
    }

    public void requestSound(string name)
    {
        sM.requestSound(name);
    }

    public void setCurrentVolumes(float vol)
    {
        sM.setCurrentVolumes(vol);
    }

    public void setMaxVolumes(float m, float s)
    {
        sM.setMaxVolumes(m, s);
    }

    public void changeMusic(string name)
    {
        sM.changeMusic(name);
    }

    public void changeMusicPitch(float pitch)
    {
        sM.changeMusicPitch(pitch);
    }
}
