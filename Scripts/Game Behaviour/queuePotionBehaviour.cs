using UnityEngine;
using UnityEngine.UI;

public class queuePotionBehaviour : MonoBehaviour
{
    public float patienceMaxTime; //Sets max length of slider
    public float patienceValue; //Current value of slider

    public bool finished = false;
    public float warningTime; //When to swap to yellow. Usually 1/3 of time.
    public float panicTime; //When to warn the player and swap to red.
    public Animator ani;
    private Slider patience;
    private Image patienceColour, potionImage;

    private gameHandler gH; //Allows potions to check if time is being measured

    // Start is called before the first frame update
    void Awake()
    {
        ani = GetComponent<Animator>();
        patience = GetComponentInChildren<Slider>();
        patienceColour = patience.gameObject.GetComponentsInChildren<Image>()[0];
        potionImage = gameObject.transform.Find("Image").GetComponent<Image>();
        gH = GameObject.Find("GameCanvas").GetComponent<gameHandler>();
    }

    //Called when creating the potion to set the timer correctly
    public void setMaxTime(float time)
    {
        patienceMaxTime = time;

        patience.maxValue = patienceMaxTime;
        warningTime = time / 2;
        panicTime = time / 6;

        patience.value = patienceMaxTime;
        patienceValue = patience.value;
    }

    //Called by potionGeneration when the player finishes their current potion
    public void potionSuccess()
    {
        ani.SetTrigger("success");
        finished = true;
    }

    //Called by potionGeneration when instantiating a new potion
    public void setPotionImage(Sprite s)
    {
        potionImage.sprite = s;
    }

    public void setMeterColour(Color col)
    {
        patienceColour.color = col;
    }

    public void addToTimer(float time)
    {
        if(patienceValue + time < patienceMaxTime)
        {
            patienceValue += time;
        } else
        {
            patienceValue = patienceMaxTime;
        }
        patience.value = patienceValue;

        if(patienceValue > warningTime)
        {
            ani.SetTrigger("revert");
            setMeterColour(Color.green);
        } else if (patienceValue > panicTime)
        {
            ani.SetTrigger("revert");
            setMeterColour(Color.yellow);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(gH.measuringTime && patience.value > 0 && !finished)
        {
            patienceValue -= Time.deltaTime * Time.timeScale;
            patience.value = patienceValue;

            if (patienceValue < panicTime)
            {
                //Trigger warning animation. Handled first since it's the shortest time
                ani.SetTrigger("warning");
                setMeterColour(Color.red);
            } else if (patienceValue < warningTime)
            {
                //Trigger changing to yellow.
                setMeterColour(Color.yellow);
            } else if (patienceValue > warningTime)
            {
                //Return to normal otherwise. Won't do anything unless it's in a warning phase.
                setMeterColour(Color.green);
            }
        } else if(patienceValue <= 0)
        {
            //Potion generation handles removing the object so it can report a penalty to the game.
            ani.SetTrigger("failed");
        }
    }
}
