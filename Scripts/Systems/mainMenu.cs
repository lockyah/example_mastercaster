using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class mainMenu : MonoBehaviour
{
    private Animator ani;
    private soundManagerHelper smh;
    private persistentManagerHelper pmh;
    private Slider sfx, mus;
    private TMP_Text sfxT, musT, tutT, hsT, colT;

    private int tutorialIndex = 0;
    public string[] tutorialText;
    public Sprite[] tutorialImages;
    private Image tutorialImage;

    private TMP_Text collTitle, collCount, collDesc;
    private TMP_Text[] collIngredients = new TMP_Text[3];
    private Dictionary<string,Image> collButtonImages;
    private Image collPotionImage;
    private GameObject collParticles;
    public Dictionary<string, string> collDescriptions = new Dictionary<string, string>();


    // Start is called before the first frame update
    void Start()
    {
        ani = GetComponent<Animator>();
        smh = GetComponent<soundManagerHelper>();
        pmh = GetComponent<persistentManagerHelper>();

        //MAIN - Grab High Score and Collection Number to update
        hsT = GameObject.Find("HS Text").GetComponent<TMP_Text>();
        hsT.text = "High Score: " + pmh.getHighScore();
        colT = GameObject.Find("Coll Text").GetComponent<TMP_Text>();
        colT.text = pmh.getPotionsCollectedString();

        //COLLECTION - Get potion title, ingredients, description, brew number, and image
        collTitle = GameObject.Find("Potion Title").GetComponent<TMP_Text>();
        collPotionImage = GameObject.Find("Potion Image").GetComponent<Image>();
        collParticles = GameObject.Find("Collection Particles");
        collDesc = GameObject.Find("Description").GetComponent<TMP_Text>();
        collCount = GameObject.Find("Made").GetComponent<TMP_Text>();
        for(int i = 0; i < 3; i++)
        {
            collIngredients[i] = GameObject.Find("Ingredients Layout").transform.GetChild(i).GetComponent<TMP_Text>();
        }

        //Load in descriptions from text, then parse to store in a dictionary to be called
        TextAsset descText = Resources.Load<TextAsset>("PotionDescriptions");
        foreach(string s in Regex.Split(descText.text, "\n"))
        {
            string[] split = Regex.Split(s, "#");
            collDescriptions.Add(split[0], split[1]);
        }

        //COLLECTION SCROLL - Set each button to be available or not

        GameObject[] cb = GameObject.FindGameObjectsWithTag("Collection Button");
        collButtonImages = new Dictionary<string, Image> { };

        for(int i = 0; i < cb.Length; i++)
        {
            //Each button is named for the potion is represents, so we can use that to determine if it's been made before
            //Get the Image component of each object, then use PMH to check the counter - if 0, set image black, if not, white
            string potionName = cb[i].name;

            collButtonImages.Add(potionName, cb[i].GetComponent<Image>());

            if(pmh.getNumberMade(potionName) == 0)
            {
                collButtonImages[potionName].color = Color.black;
            } else
            {
                collButtonImages[potionName].color = Color.white;
            }
        }

        //VOLUME - Grab current volumes from PlayerPrefs and set the sliders/volumes accordingly
        sfx = GameObject.Find("SFX Slider").GetComponent<Slider>();
        mus = GameObject.Find("Music Slider").GetComponent<Slider>();
        sfxT = sfx.gameObject.transform.Find("Volume").GetComponent<TMP_Text>();
        musT = mus.gameObject.transform.Find("Volume").GetComponent<TMP_Text>();

        mus.value = pmh.getVolumes()[0];
        sfx.value = pmh.getVolumes()[1];
        adjustVolumes();

        //TUTORIAL - Grab the tutorial text and image to change
        tutT = GameObject.Find("Tutorial Text").GetComponent<TMP_Text>();
        tutorialImage = GameObject.Find("Tutorial Image").GetComponent<Image>();
        tutT.text = tutorialText[0];

        //Set info to be the first potion's data and start of tutorial by default
        collectPotionInformation("Heart's Calling");
        changeTutorial(0);
    }

    //Call to transition the menu layout. Only works on either end of the animation to prevent double-taps
    public void aniTrigger(string s)
    {
        if(ani.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f || ani.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.1f)
        {
            ani.SetTrigger(s);
        }
    }

    //Called at end of START GAME animation to load the game
    public void startGame()
    {
        SceneManager.LoadScene(2);
    }

    public void changeTutorial(int amt)
    {
        if(!( (amt < 0 && tutorialIndex == 0) || (amt > 0 && tutorialIndex == tutorialText.Length-1) ))
        {
            //Move amt number of pages. Do nothing if backward on page 0 or forward on last page.
            tutorialIndex += amt;
            tutT.text = tutorialText[tutorialIndex];
            tutorialImage.sprite = tutorialImages[tutorialIndex];
        }
    }

    //Used by sliders when adjusted. Calls SMH to adjust volume accordingly
    public void adjustVolumes()
    {
        sfxT.text = ((int)sfx.value).ToString();
        musT.text = ((int)mus.value).ToString();

        smh.setMaxVolumes(mus.value/100, sfx.value/100);

        pmh.updateVolume(mus.value, sfx.value);
    }

    public void collectPotionInformation(string potionName)
    {
        //Called in collection by the buttons to set the title, count, image, etc.

        //Use the collection button image to determine if count is 0 (button image colour will be black)
        //If so, use a default text. If not, fetch the right data to replace it with.
        //Either way, the image borrows from the button it's used from

        collIngredients[0].gameObject.SetActive(false);
        collIngredients[1].gameObject.SetActive(false);
        collIngredients[2].gameObject.SetActive(false);

        if (collButtonImages[potionName].color == Color.black)
        {
            //DEFAULT TEXT
            collPotionImage.color = Color.black;

            collTitle.text = "Unknown Potion";
            collCount.text = "Brewed: 0";
            collDesc.text = "You haven't made this potion yet...";
            collIngredients[0].text = "???";
            collIngredients[0].gameObject.SetActive(true);
            collParticles.SetActive(false);

        } else
        {
            //CUSTOM TEXT
            collPotionImage.color = Color.white;

            collTitle.text = potionName;
            collCount.text = "Brewed: " + pmh.getNumberMade(potionName).ToString();
            collDesc.text = collDescriptions[potionName];

            potionRequest pr = pmh.getPotionData(potionName);
            for(int i = 0; i < pr.castList.Length; i++)
            {
                collIngredients[i].gameObject.SetActive(true);
                collIngredients[i].text = pmh.translateSpell(pr.castList[i]);
            }
            collParticles.SetActive(true);
        }


        collPotionImage.sprite = collButtonImages[potionName].sprite;
    }
}
