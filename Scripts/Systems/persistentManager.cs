using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class persistentManager : MonoBehaviour
{
    //PD contains volumes, high score, and number of each potion made
    public persistentData persistent = new persistentData(); //The data loaded in via PlayerPrefs
    public Dictionary<string, int> potionsMade = new Dictionary<string, int> { }; //Kept separate from persistent to update as the game continues. Applies to PlayerPrefs at Game Over

    public Dictionary<string, potionRequest> potionsList = new Dictionary<string, potionRequest> { }; //Every potion combination in the game is kept here to use in collection and generation
    public Dictionary<string, string> castNames = new Dictionary<string, string>(); //Dictionary of spell class to spell names

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //If there's another Persistent Manager object in the scene (from another scene), delete this one
        if (GameObject.Find("PersistentManager") != gameObject)
        {
            Destroy(gameObject);
        } else
        {
            string data = PlayerPrefs.GetString("PersistentData");

            // --- CONSTANT DATA ---

            //Fetch every potion recipe and sort it into PotionRequests
            TextAsset[] potions = Resources.LoadAll<TextAsset>("PotionCombinations");
            foreach (TextAsset p in potions)
            {
                potionsList.Add(p.name, new potionRequest(p.name, Regex.Split(p.text, ",")));
            }

            //Fetch spell class-name translations and store them
            TextAsset ta = Resources.Load<TextAsset>("SpellNames"); //Read translation of spell shape to spell name
            string[] namesArray = Regex.Split(ta.text, "; "); //Make array of each line in the text
            string[] temp;

            for (int i = 0; i < namesArray.Length; i++)
            {
                temp = Regex.Split(namesArray[i], ",");
                castNames.Add(temp[0], temp[1]);
            }

            // --- LOADING SAVE ---

            if (data == "")
            {
                //If there is no Persistent Data saved, create a blank file from the temp persistent file
                persistent.musicVol = 50f;
                persistent.sfxVol = 50f;
                persistent.highScore = 0;

                //Make a list of names from potion resource folder, set count array the same length as it will all be 0s automatically
                persistent.potionsNames = new string[potions.Length];
                persistent.potionsMade = new int[potions.Length];

                for (int i = 0; i < potions.Length; i++)
                {
                    persistent.potionsNames[i] = potions[i].name;
                    potionsMade.Add(potions[i].name, 0);
                }

                data = JsonUtility.ToJson(persistent);
                PlayerPrefs.SetString("PersistentData", data);

                print("Created new save data");
            }
            else
            {
                //If save data exists, convert it to a persistent data object and update the running tally to match
                persistent = JsonUtility.FromJson<persistentData>(data);

                for (int i = 0; i < persistent.potionsNames.Length; i++)
                {
                    potionsMade.Add(persistent.potionsNames[i], persistent.potionsMade[i]);
                }
            }

            persistent = JsonUtility.FromJson<persistentData>(data);
            print(JsonUtility.ToJson(persistent));
        }
    }

    public float[] getVolumes()
    {
        return new float[]{persistent.musicVol, persistent.sfxVol};
    }

    public void updateVolumes(float mus, float sfx)
    {
        persistent.musicVol = mus;
        persistent.sfxVol = sfx;
        PlayerPrefs.SetString("PersistentData", JsonUtility.ToJson(persistent));
    }

    public void updateScores(int score)
    {
        //Used at end of game to evaluate player score and update potion collection
        if(score > persistent.highScore)
        {
            persistent.highScore = score;
        }

        for(int i = 0; i < persistent.potionsNames.Length; i++)
        {
            //For each potion name, make the Persistent counter equal to the new total from the running tally
            persistent.potionsMade[i] = potionsMade[persistent.potionsNames[i]];
        }

        PlayerPrefs.SetString("PersistentData", JsonUtility.ToJson(persistent));
        print(PlayerPrefs.GetString("PersistentData"));
    }

    public potionRequest getPotionData(string name)
    {
        return potionsList[name];
    }

    public string getPotionsCollectedString()
    {
        int tally = 0;

        //Iterate through the potions list and find which are not 0 (i.e. have been made before)
        foreach(int i in persistent.potionsMade)
        {
            if(i > 0)
            {
                tally++;
            }
        }

        return "Collected " + tally + "/" + persistent.potionsNames.Length +" potions";
    }

    public void resetData()
    {
        //Used in main menu to remove all PlayerPrefs data. Will close the game afterward.
        PlayerPrefs.SetString("PersistentData", ""); //Empty, will be reset when the game next opens.
        Application.Quit();
        print("Exiting game.");
    }
}
