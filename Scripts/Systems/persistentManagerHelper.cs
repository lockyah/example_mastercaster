using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class persistentManagerHelper : MonoBehaviour
{
    private persistentManager pM;

    //Inbetween for Persistent Manager, allows separate objects to update and use save data

    void Awake()
    {
        pM = GameObject.Find("PersistentManager").GetComponent<persistentManager>();
    }

    public void resetData()
    {
        pM.resetData();
    }

    public void updateVolume(float mus, float sfx)
    {
        //Called when using sliders for volumes
        StopAllCoroutines();
        StartCoroutine(volumeTimer(mus, sfx));
    }

    private IEnumerator volumeTimer(float mus, float sfx)
    {
        //Coroutine used for "On Value Changed" of sliders
        //Waits for player to leave them for a few seconds before changing to avoid constantly overwriting
        float timer = 1.5f;

        while(timer > 0)
        {
            timer -= 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        print("Saved volumes! MUS: " + mus + ", SFX: " + sfx);
        pM.updateVolumes(mus, sfx);
    }

    public void updateScores(int score)
    {
        //Called at the end of a game to update the high score and potion list.
        //Uses the PotionsMade dictionary to add onto the Persistent file's potion collection
        //Player Score is added via parameter to be evaluated
        pM.updateScores(score);
    }

    public void createPotion(string name)
    {
        //Called when a potion is made. Adds 1 to pM's PotionsMade dictionary for UpdateScores to use later
        pM.potionsMade[name] += 1;
    }
    
    public string translateSpell(string name)
    {
        //Called in game and in collection to translate spell classes to their in-game name
        return pM.castNames[name];
    }

    public float[] getVolumes()
    {
        return pM.getVolumes();
    }

    public int getHighScore()
    {
        return pM.persistent.highScore;
    }

    public int getNumberMade(string potion)
    {
        //Called when setting up the collection
        //Returns the number of a potion made via the PotionsMade dictionary
        return pM.potionsMade[potion];
    }

    public potionRequest getPotionData(string name)
    {
        //Called in game and in collection to get the ingredients needed for a potion
        return pM.getPotionData(name);
    }

    public string getPotionsCollectedString()
    {
        //Called on main menu to update the text below the Collection button
        //Returns in format "Collected x/y potions"
        return pM.getPotionsCollectedString();
    }
}
