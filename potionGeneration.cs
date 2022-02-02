using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class potionGeneration : MonoBehaviour
{
    /*
     * Handles generating the potions, either from a level list or randomly depending on the game's state.
     */
    public float levelDifficulty; //Which difficulty? Set by the level loader from PlayerPrefs.
    private bool maxDifficulty = false; //True when not looking for more potions to add
    private List<potionRequest> availablePotions = new List<potionRequest>(); //Store for potions in this level
    private Dictionary<string, Sprite> queueIcons = new Dictionary<string, Sprite>(); //Store for the small potion icons for new queue items

    private float[] customers; //How many customers (request generators) should be included?
    private float customerMinTime; //What is the minimum time a customer will wait between generating orders?
    private float customerMaxTime; //Same for maximum time
    private float customerWaitTime; //How long will the customer wait after arriving?
    private float customerCooldown; //How much time should be added to the timers after another order goes through?

    private List<potionRequest> potions = new List<potionRequest> { }; //Which potions are waiting to be made?
    private List<queuePotionBehaviour> potionsQueue = new List<queuePotionBehaviour> { }; //Potions in the UI
    private GameObject queueParent;
    private GameObject queueObject;
    public int maxInQueue = 6;

    private gameHandler game;

    private soundManagerHelper sounds;
    private persistentManagerHelper persist;

    private TMP_Text potionTitle;
    private int currentIngredient = -1; //Starts at -1 to highlight the first ingredient correctly
    private GameObject ingParent; //Layout object to add ingredients to as they are cast
    private List<TMP_Text> ingTexts; //List of the objects to enable/disable

    //Colours for ingredient highlighting and customer moods
    public Color colourCorrect;
    public Color colourCurrent;
    public Color colourWrong;
    private bool animatingPenalty, animatingRevive = false;

    // Start is called before the first frame update
    void Start()
    {
        game = GetComponent<gameHandler>();
        sounds = GetComponent<soundManagerHelper>();
        persist = GetComponent<persistentManagerHelper>();
        levelDifficulty = game.timeMultiplier;

        //Find potion title and objects for UI updating
        potionTitle = GameObject.Find("Potion Title").GetComponent<TMP_Text>();
        queueParent = GameObject.Find("Potion Queue");
        queueObject = Resources.Load<GameObject>("Prefabs/Items/Sprites/Queue Potion");

        //UI for the (max three) ingredients in the current potion
        ingParent = GameObject.Find("Ingredients Layout");
        ingTexts = new List<TMP_Text>();
        foreach(Transform child in ingParent.transform)
        {
            ingTexts.Add(child.GetComponent<TMP_Text>());
        }

        //Collect potion icons
        Sprite[] sprites = Resources.LoadAll<Sprite>("PotionImages/materials");
        foreach(Sprite s in sprites)
        {
            queueIcons.Add(s.name, s);
        }


        updateDifficulty(); //Get first round of potions and settings

        //Customers, aka order generators.
        customers = new float[3]; //Always three generators, their wait times get shorter
        //Assign all customers a random time to begin with
        for(int i = 0; i < customers.Length; i++)
        {
            customers[i] = Random.Range(customerMinTime, customerMaxTime);
        }
    }

    void Update()
    {
        if(game.playerLives > 0 && game.measuringTime)
        {
            checkWaitingCustomers();
            checkCurrentCustomer();
        }

        if (game.reviveTimer > 0f && !animatingRevive)
        {
            //On the frame revival starts, start up the revival function
            //Set animatingRevive, but don't reset since this can't be reused

            print("Adding time to queue");
            animatingRevive = true;
            StartCoroutine(reviveReset());
        }
    }

    private void FixedUpdate()
    {
        if (!maxDifficulty && game.playerLives > 0)
        {
            //Time Multiplier adds .5 every minute; can be used to affect level generation
            if (game.timeMultiplier != levelDifficulty)
            {
                levelDifficulty = game.timeMultiplier;
                updateDifficulty();
            }
        }
    }

    private void updateDifficulty()
    {
        //Resource Call the next text element to use to update the available potions.
        //If none is found, we're at max difficulty, so toggle the bool to stop checking

        //Level format: minimum wait between orders, maximum wait, cooldown to add after order, new potions
        TextAsset level = Resources.Load<TextAsset>("LevelSettings/" + levelDifficulty);
        if (level)
        {
            string[] levelString = Regex.Split(level.text, ",");

            customerMinTime = int.Parse(levelString[0]);
            customerMaxTime = int.Parse(levelString[1]);
            customerWaitTime = int.Parse(levelString[2]);
            customerCooldown = int.Parse(levelString[3]);

            //Parse the rest into the list of potions.
            for (int i = 4; i < levelString.Length; i++)
            {
                availablePotions.Add(persist.getPotionData(levelString[i]));
            }
        } else
        {
            print("Max difficulty!");
            maxDifficulty = true;
        }
    }

    private void checkWaitingCustomers()
    {
        //Checks each waiting potion by their timer to see if they need taking away.
        if (potionsQueue.Count > 0)
        {
            List<int> toRemove = new List<int> { };
            for (int i = 0; i < potionsQueue.Count; i++)
            {
                AnimatorStateInfo info = potionsQueue[i].ani.GetCurrentAnimatorStateInfo(0);

                if (info.IsName("potion_failed") && info.normalizedTime >= 0.75f)
                {
                    toRemove.Add(i);

                    //If we are removing the current potion, reset the game progress
                    if(i == 0)
                    {
                        game.resetPotionProgress(false);

                        foreach (TMP_Text t in ingTexts)
                        {
                            t.color = Color.black;
                            t.gameObject.SetActive(false);
                        }
                        currentIngredient = -1;
                    }
                } else if (info.IsName("potion_success") && info.normalizedTime >= 0.75f)
                {
                    toRemove.Add(i);
                    game.addScore(customers[i]/10, currentIngredient+1); //Add to score, multiplied by remaining seconds and number of ingredients
                    game.resetPotionProgress(true);

                    foreach (TMP_Text t in ingTexts)
                    {
                        t.color = Color.black;
                        t.gameObject.SetActive(false);
                    }
                    currentIngredient = -1;
                }
            }

            //After finding which need to be removed, if any, take it from the waiting list and UI.
            foreach(int i in toRemove)
            {
                potions.RemoveAt(i);
                Destroy(potionsQueue[i].gameObject);
                potionsQueue.RemoveAt(i);
            }
        }

        //Checks if the generators have made something new to add onto the queue.
        for (int i = 0; i < customers.Length; i++)
        {
            if (potionsQueue.Count < maxInQueue)
            {
                customers[i] -= Time.deltaTime * Time.timeScale;

                if (customers[i] <= 0)
                {
                    //If this timer is up, add a random potion from the available potions
                    potions.Add(availablePotions[Random.Range(0, availablePotions.Count)]);
                    potionsQueue.Add(Instantiate(queueObject, queueParent.transform).GetComponent<queuePotionBehaviour>());
                    potionsQueue[potionsQueue.Count - 1].setMaxTime(customerWaitTime);

                    //Set image of potionsqueue[last] to match the potion
                    potionsQueue[potionsQueue.Count - 1].setPotionImage(queueIcons[potions[potionsQueue.Count - 1].potionName+"_small"]);

                    //Reset this timer
                    customers[i] = Random.Range(customerMinTime, customerMaxTime);

                    sounds.requestSound("sfx_bell");

                    //Add the cooldown to the timers so they don't all appear together
                    for (int c = 0; c < customers.Length; c++)
                    {
                        customers[c] += customerCooldown;
                    }
                }
            }
        }

    }

    private void checkCurrentCustomer()
    {
        //Check if the previous potion is complete. If so, reset the UI and throw in the next if there is one!
        if (game.currentPotion.potionName == "" && game.currentPotion.castList.Length == 0)
        {
            if (game.finishedPotion)
            {
                //if(current tally = 1) then trigger "new potion!" animation with potionsQueue[0].image
                if(game.persist.getNumberMade(potions[0].potionName) == 1)
                {
                    game.newPotionBanner(queueIcons[potions[0].potionName + "_small"]);
                }

                potionsQueue[0].potionSuccess();
                game.finishedPotion = false;
            }

            if (potions.Count > 0)
            {
                game.currentPotion = potions[0];

                potionTitle.text = potions[0].potionName;

                for (int i = 0; i < potions[0].castList.Length; i++)
                {
                    //Add a text element for each ingredient,  set it to the spell name for each cast
                    ingTexts[i].gameObject.SetActive(true);
                    ingTexts[i].text = persist.translateSpell(potions[0].castList[i]);
                }
            }
            else
            {
                potionTitle.text = "Waiting for customers...";
            }
        }
        else
        {
            //Otherwise, handle events in the game.

            if (currentIngredient < game.castIndex)
            {
                //The game is on a later ingredient than the UI. Highlight everything below it in green if possible, then the current ingredient in yellow.
                for (int i = 0; i < game.castIndex; i++)
                {
                    ingTexts[i].color = colourCorrect;
                }

                ingTexts[game.castIndex].color = colourCurrent;

                currentIngredient = game.castIndex;

            }
            else if (game.isInPenalty() && !animatingPenalty)
            {
                //The player made a mistake! Start coroutine to flash colours and reset to the first ingredient.
                StartCoroutine(incorrectColours());
            }
        }
    }

    //Make each ingredient red, then return to black and highlight the first ingredient after the cooldown
    private IEnumerator incorrectColours()
    {
        animatingPenalty = true; //Stops re-triggering animation while it's still going
        float colourTimer = 2.55f; //Duration of penalty animation

        foreach (TMP_Text t in ingTexts)
        {
            t.color = colourWrong;
        }

        while(colourTimer > 0)
        {
            colourTimer -= Time.deltaTime * Time.timeScale;
            yield return null;
        }

        foreach (TMP_Text t in ingTexts)
        {
            t.color = Color.black;
        }
        ingTexts[0].color = colourCurrent;
        currentIngredient = 0;
        animatingPenalty = false;
    }


    //Called when reviving via an ad. During the revival animation, add 30 seconds to every timer and revert to ingredient one.
    private IEnumerator reviveReset()
    {
        foreach (TMP_Text t in ingTexts)
        {
            t.color = Color.black;
        }
        ingTexts[0].color = colourCurrent;
        currentIngredient = 0;

        while(game.reviveTimer > 0)
        {
            //With a 2 second timer and a x7.5 multiplier, this should give back 15 seconds
            foreach(queuePotionBehaviour q in potionsQueue)
            {
                q.addToTimer(Time.deltaTime * Time.timeScale * 7.5f);
            }
            yield return new WaitForEndOfFrame();
        }
    }
}
