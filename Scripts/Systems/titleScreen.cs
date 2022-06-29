using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class titleScreen : MonoBehaviour
{
    private RuntimePlatform platform;
    private Animator ani;
    private bool canTap = true;
    private float timer = 0;
    private persistentManagerHelper pmh;
    private soundManagerHelper smh;

    private void Start()
    {
        platform = Application.platform;
        ani = GetComponent<Animator>();
        smh = GetComponent<soundManagerHelper>();
        pmh = GetComponent<persistentManagerHelper>();

        //Load in sound settings from PlayerPrefs, set to 100% of set volume
        smh.setMaxVolumes(pmh.getVolumes()[0] / 100, pmh.getVolumes()[1] / 100);
        smh.setCurrentVolumes(1f);
    }


    //Simple script to continue the title animation when the screen is tapped
    void Update()
    {
        if (ani.GetCurrentAnimatorStateInfo(0).IsName("title_idle"))
        {
            if (platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    ani.SetTrigger("screen_tapped");
                    canTap = false;
                    timer = 1f;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ani.SetTrigger("screen_tapped");
                    canTap = false;
                    timer = 1f;
                }
            }
        }

    }


    //Called from animation event to load the next scene
    public void continueToMenu()
    {
        SceneManager.LoadScene(1);
    }
}
