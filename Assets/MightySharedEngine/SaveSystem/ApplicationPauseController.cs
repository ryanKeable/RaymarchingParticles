using UnityEngine;
using System.Collections.Generic;

public interface IPauseable
{
    void prepareForPause();
    void appDidReturnFromPause();
}

// this object handles all the pause flow control
// otherwise things can happen out of order
public sealed class ApplicationPauseController : SingletonMono<ApplicationPauseController>
{
    bool appDidBoot = false;
    List<IPauseable> thingsToPause = new List<IPauseable>(8);

    protected override void Awake()
    {
        base.Awake();

        Application.wantsToQuit += ApplicationWantsToQuit;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Application.wantsToQuit -= ApplicationWantsToQuit;
    }

    public static void registerForPauses(IPauseable itemToPause)
    {
        ApplicationPauseController.instance.thingsToPause.Add(itemToPause);
    }

    public static void deregisterForPauses(IPauseable itemToPause)
    {
        ApplicationPauseController.instance.thingsToPause.Remove(itemToPause);
    }

    public void appDidCompleteBooting()
    {
        appDidBoot = true;
        refreshFromAway();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!appDidBoot) return;

        if (pauseStatus)
        {
            prepareToGoAway();
        }
        else
        {
            refreshFromAway();
        }
    }

    bool ApplicationWantsToQuit()
    {
        if (appDidBoot)
        {
            prepareToGoAway();
        }
        return true;
    }

    public void prepareToGoAway()
    {
        if (!appDidBoot) return;

        MDebug.LogLtBlue("[ApplicationPauseController] Application is entering background");

        for (int i = 0; i < thingsToPause.Count; i++)
        {
            thingsToPause[i].prepareForPause();
        }

        // note that we call this specifically, instead of having game state as one of the things to pause
        // this guarantees that all other objects have updated their state before it gets saved down to disk
        GameStateController.instance.saveState(); // just in case
    }

    public void refreshFromAway()
    {
        if (!appDidBoot) return;
        MDebug.LogLtBlue("[ApplicationPauseController] Application is resuming focus");

        for (int i = 0; i < thingsToPause.Count; i++)
        {
            thingsToPause[i].appDidReturnFromPause();
        }
    }
}
