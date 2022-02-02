using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class gameHandler : MonoBehaviour
{
    public bool measuringTime = true; //Allows pausing for tutorials, etc. without pausing entirely
    private float timeUsed = 0;
    public float reviveTimer = 0;
    private float timePercentage = 0; //Progression of each minute for difficulty upgrades

    public int score = 0;
    public int potionScore = 1000; //Base score for each potion
    public float timeMultiplier = 1; //Multi from difficulty
    private int maxScore = 999999999; //Max score that can be displayed without changing format

    public int playerLives = 3; //How many times did the player use the wrong ingredient?
    private bool upgradeDifficulty = true; //Whether to change difficulty. Disables to avoid applying multiple times

    public potionRequest currentPotion; //Which potion are we making?
    public int castIndex; //Which ingredient are we comparing to?
    public bool finishedPotion = false; //Used by potion generation to trigger removing from the queue

    public spellDraw draw; //The code that lets the player draw. Disabled on pause, potion check, or game over.

    private TMP_Text scoreText, endScore, endHighScore;
    private GameObject newRecord;

    private Animator gameAni;
    private Animator[] livesAni;
    private Animator environmentAni;
    private Animator pauseAni;
    private Image bannerImage;

    public soundManagerHelper sounds;
    public persistentManagerHelper persist;
    private AdManager ads;

    // Start is called before the first frame update
    void Start()
    {
        GameObject pa = GameObject.Find("Play Area");
        draw = pa.GetComponent<spellDraw>();

        scoreText = GameObject.Find("Score").GetComponent<TMP_Text>();
        endScore = GameObject.Find("EndScore").GetComponent<TMP_Text>();
        endHighScore = GameObject.Find("EndHighScore").GetComponent<TMP_Text>();
        newRecord = GameObject.Find("Record");

        gameAni = GameObject.Find("GameCanvas").GetComponent<Animator>();
        environmentAni = pa.GetComponent<Animator>();
        pauseAni = GameObject.Find("PauseCanvas").GetComponent<Animator>();
        bannerImage = GameObject.Find("New Potion Image").GetComponent<Image>();

        sounds = GetComponent<soundManagerHelper>();
        persist = GetComponent<persistentManagerHelper>();

        endHighScore.text = "High Score: " + persist.getHighScore();
        newRecord.SetActive(false); //Hide to re-enable with a new high score

        //Link to ads manager when loading the game scene
        ads = GameObject.Find("PersistentManager").GetComponent<AdManager>();
        ads.gH = this;

        //For each child (heart object) in the life UI, grab its animator to use
        livesAni = GameObject.Find("Lives").transform.GetComponentsInChildren<Animator>();
    }

    private void Update()
    {
        draw.canDraw = measuringTime; //If time isn't passing, player shouldn't be able to draw.

        //If not paused or in tutorial, measure time
        if (Time.timeScale != 0 && measuringTime)
        {
            timeUsed += Time.deltaTime;

            //Use mod60 to determine percentage of time passed in each minute.
            timePercentage = ((timeUsed % 60) / 60) * 100f;

            //If starting a new minute (approximately) and not at the start of the game, up the difficulty!
            if (timePercentage < 1 && upgradeDifficulty && timeUsed > 60)
            {
                print("Difficulty up!");
                timeMultiplier += .50f;
                upgradeDifficulty = false;
            }

            //Halfway through, reset the bonus trigger so it doesn't apply multiple times.
            if (timePercentage > 50 && !upgradeDifficulty)
            {
                upgradeDifficulty = true;
            }

            //Normally, ability to draw = time is passing.
            //If the player is in a penalty, this overrides that logic to deny it
            if (isInPenalty())
            {
                draw.canDraw = false;
            }
        } else if (Time.timeScale != 0 && reviveTimer > 0)
        {
            //If not paused and playing the revival animation, count down timer until the correct time to continue
            reviveTimer -= Time.deltaTime;

            if(reviveTimer <= 0)
            {
                //Reset the gameplay to continue
                measuringTime = true;
                draw.canDraw = true;
            }   
        }
    }

    //Called from PAUSE and RESUME buttons. Invert timescale, time measuring, and ability to draw.
    public void togglePause()
    {
        if (pauseAni.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f || pauseAni.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.1f)
        {
            if (reviveTimer <= 0)
            {
                //If not reviving, enable time measurement
                //If reviving, will always be false anyway
                measuringTime = !measuringTime;
            }

            Time.timeScale = Time.timeScale == 0 ? 1 : 0;
            pauseAni.SetBool("paused", !pauseAni.GetBool("paused"));
        }        
    }

    //Called from SPELLS button to check patterns. Inverse toggle animation and toggles "checkingSpells" on the spellDraw
    public void toggleSpells()
    {
        if(gameAni.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f || gameAni.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.1f)
        {
            gameAni.SetBool("checking_spells", !gameAni.GetBool("checking_spells"));
            draw.checkingSpells = !draw.checkingSpells;
        }   
    }

    public void pauseTrigger(string name)
    {
        if (pauseAni.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.98f || pauseAni.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.08f)
        {
            pauseAni.SetTrigger(name);
        }
    }

    //Using the time left on the potion and how many ingredients were in it, calculate and add its new score.
    public void addScore(float potionMultiplier, int ingredientMultiplier)
    {
        score += (int)Mathf.Floor(potionScore * potionMultiplier * timeMultiplier * ingredientMultiplier);
        if(score > maxScore)
        {
            score = maxScore;
        }

        scoreText.text = score.ToString();
        endScore.text = "Score: " + scoreText.text;

        if (newRecord.active == false && score > persist.getHighScore())
        {
            newRecord.SetActive(true);

            if (persist.getHighScore() != 0)
            {
                //If there is a high score to beat, trigger the banner when it's passed
                gameAni.SetTrigger("banner_newrecord");
            }
        }
    }

    public void updateHealth(int amount)
    {
        playerLives += amount;

        if(playerLives < 0)
        {
            //Catch in case multiple game-ending events happen together. Resets it to 0 for the sake of reviving, but otherwise lets the first event trigger everything.
            playerLives = 0;
        } else
        {
            //For lives below count, add. Will add lives when reviving or do nothing otherwise.
            for (int i = 0; i < playerLives; i++)
            {
                //Reset triggers to stop interference
                livesAni[i].ResetTrigger("add");
                livesAni[i].ResetTrigger("drop");

                livesAni[i].SetTrigger("add");
            }

            //For lives above count, drop. Used when losing a life.
            for (int i = playerLives; i < livesAni.Length; i++)
            {
                //Reset triggers to stop interference
                livesAni[i].ResetTrigger("add");
                livesAni[i].ResetTrigger("drop");

                livesAni[i].SetTrigger("drop");
            }

            //Handle music changing with lives and when to trigger game over
            switch (playerLives)
            {
                case 3:
                    sounds.changeMusicPitch(1f);
                    break;
                case 2:
                    sounds.changeMusicPitch(1.15f);
                    break;
                case 1:
                    sounds.changeMusicPitch(1.30f);
                    break;
                case 0:
                    gameOver();
                    break;
            }
        }
    }

    //Sets everything up to run the GO animation and to revive if necessary
    public void gameOver()
    {
        measuringTime = false;
        draw.canDraw = false;
        draw.checkingSpells = false;
        draw.clearDrawing();

        gameAni.SetBool("game_over", true);
        gameAni.SetBool("checking_spells", false);
        environmentAni.SetBool("game_over", true);
        pauseAni.SetTrigger("game_over");

        sounds.changeMusic("none"); //Set to none for effect; animation sets the game over music going
        sounds.changeMusicPitch(1f);

        persist.updateScores(score);
    }

    //Alt to GameOver() used when voluntarily ending the run. Plays a different animation, resets the timescale, and disables continuing
    public void safeGameOver()
    {
        Time.timeScale = 1;
        pauseAni.SetBool("paused", false);

        measuringTime = false;
        draw.canDraw = false;
        draw.checkingSpells = false;
        draw.clearDrawing();

        gameAni.SetBool("game_over", true);
        gameAni.SetBool("checking_spells", false);
        pauseAni.SetBool("used_continue", true);
        environmentAni.SetBool("game_over_safe", true);

        persist.updateScores(score);
    }

    //Only in GO screen, but pause can end a run early to get to it
    public void exitGame()
    {
        SceneManager.LoadScene(1);
    }

    //Called from the "Start Ad" button
    public void startAd()
    {
        ads.playRewardAd();
    }

    //Called from Ad Manager if the rewarded ad played correctly.
    public void adSuccessful()
    {
        //Add 1 heart back and trigger animation effect to hide Continue button from re-use
        if (playerLives + 1 < 3)
        {
            updateHealth(1); //Add extra life if it can fit, then revert to normal play
        }

        pauseAni.SetBool("used_continue", true);
        sounds.changeMusic("mus_normal");

        reviveTimer = 2f;

        gameAni.SetBool("game_over", false);
        environmentAni.SetBool("game_over", false);
        environmentAni.SetBool("game_over_safe", false);
        pauseAni.SetTrigger("game_over");
        pauseAni.SetTrigger("confirm");
    }

    //Reference for if the player is in a penalty phase. Used here and for UI.
    public bool isInPenalty()
    {
        return environmentAni.GetCurrentAnimatorClipInfo(0)[0].clip.name == "spell_failed";
    }

    //Called from potion generation if the current potion is failed. Resets progress to match the new potion.
    public void resetPotionProgress(bool success)
    {
        //If not successful (potion ran out of time), lose a life
        if (!success)
        {
            updateHealth(-1);
            sounds.requestSound("sfx_explosion");

            if (playerLives > 0)
            {
                //Doesn't trigger on last life to avoid overwriting game over animation
                environmentAni.SetTrigger("spell_failed");
            }
        }
        currentPotion = new potionRequest("", new string[] { });
        castIndex = 0;
    }

    public void newPotionBanner(Sprite s)
    {
        bannerImage.sprite = s;
        gameAni.SetTrigger("banner_newpotion");
    }

    //When an ingredient hits the water, it calls this to add it to the current spell.
    public void addToCast(string ingredientName)
    {
        //Only process the ingredient in active play (when measuring time)
        if (measuringTime)
        {
            if (currentPotion.castList.Length != 0 && ingredientName == currentPotion.castList[castIndex])
            {
                //If this new item is the same as the current needed ingredient, move on to the next.
                castIndex++;

                sounds.requestSound("random_splash");
                sounds.requestSound("sfx_tingle");

                if (castIndex == currentPotion.castList.Length)
                {
                    //Spell is complete! Play a short victory animation, then empty the current potion for the potion generation script to bring new ones in.
                    environmentAni.SetTrigger("spell_success");
                    sounds.requestSound("sfx_success");
                    persist.createPotion(currentPotion.potionName);

                    currentPotion = new potionRequest("", new string[] { });
                    finishedPotion = true; //Will be reset to false after the queue removes the earliest potion.

                    castIndex = 0;
                }
            }
            else if (currentPotion.castList.Length == 0 || isInPenalty())
            {
                //If there is no potion active or a penalty is playing, just play the sound
                sounds.requestSound("random_splash");
            }
            else
            {
                //Otherwise, it's a mistake. Boom, reset that progress!
                updateHealth(-1);
                draw.clearDrawing();
                castIndex = 0;
                sounds.requestSound("sfx_explosion");

                if (playerLives > 0)
                {
                    //Doesn't trigger on last life to avoid overwriting game over animation
                    environmentAni.SetTrigger("spell_failed");
                }
            }
        } else
        {
            //For ingredients that hit after a game over, just play the sound and don't process it
            sounds.requestSound("random_splash");
        }
        
        
    }
}
