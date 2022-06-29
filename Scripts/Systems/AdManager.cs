using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdManager : MonoBehaviour, IUnityAdsListener
{
    public gameHandler gH; //Ads only play on game screen, but need to load during title.

    // Start is called before the first frame update
    void Start()
    {
        //Built for Android only - Apple ID is 4434774
        Advertisement.Initialize("4434775");
        Advertisement.AddListener(this);
    }

    public void playRewardAd()
    {
        if (Advertisement.IsReady("Rewarded_Android"))
        {
            Advertisement.Show("Rewarded_Android");
        } else
        {
            print("No ads ready!");
        }
    }

    public void OnUnityAdsDidError(string message)
    {
        print("Ad didn't load correctly...");
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        if(placementId == "Rewarded_Android" && showResult == ShowResult.Finished)
        {
            print("Finished ad!");
            gH.adSuccessful();

        }
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        //EMPTY - Needs to be implemented for ads to work, but is unused.
    }

    public void OnUnityAdsReady(string placementId)
    {
        print("Ads loaded!");
    }
}
