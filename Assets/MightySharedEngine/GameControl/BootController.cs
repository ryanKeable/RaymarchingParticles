using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public sealed class BootController : MonoBehaviour
{
    public bool selfStart = false;

    // the pre boot event is the first thing the boot loader calls. This is usually to show some sort of loading screen.
    public UnityEvent preBootEvent;

    // the boot chain is the list of dependant boot items in the order they get called
    public List<BootableMonoBehaviour> preConfigChain = new List<BootableMonoBehaviour>();
    public BootableMonoBehaviour configBooter;
    public List<BootableMonoBehaviour> postConfigChain = new List<BootableMonoBehaviour>();

    // the game start event is what the boot controller calls once the boot is fully completed.
    public UnityEvent gameStartEvent;

    public int loadTimeLogLevel = 0;
    public bool loadTimeVerbose = false;

    void Start()
    {
        if (selfStart) bootLoadDidComplete();
    }

    public bool cancelBoot = false;
    public void bootLoadDidComplete()
    {
        MDebug.SetLogFormat(MDebug.LogFormat.Timestamped);

#if UNITY_EDITOR || UNITY_STANDALONE
        MDebug.SetLogLevel(MDebug.LogLevel.All);
#elif (UNITY_IOS || UNITY_ANDROID) && !DEVELOPMENT_BUILD
        // We want to minimise logging in release mobile builds as logging can be expensive
        MDebug.SetLogLevel(MDebug.LogLevel.ErrorOnly);
#else
        MDebug.SetLogLevel(MDebug.LogLevel.All);
#endif

        CommandDispatch.dispatcher.clearCommandRegistry(); // really only relevant in the editor but harmless otherwise

        preBootEvent.Invoke();

        // we allow everything currently instantiated to start
        if (!cancelBoot) // the preboot event can find us and cancel the boot if need be 
            AnimationHelper.playOnNextFrame(this, startBootChain);
    }

    BootChainController chainControl;
    public void startBootChain()
    {
        MDebug.LogBlue("[BOOT] Starting the boot chain");

        chainControl = new BootChainController();
        runPreConfig();

    }

    void runPreConfig()
    {
        MDebug.LogBlue("[BOOT] runPreConfig");
        chainControl.startBootChain(this, preConfigChain, loadTimeLogLevel, loadTimeVerbose, () =>
        {
            // we are going to pause a frame to let all the CPU spikes smoothout a wee bit
            runConfig();
        });
    }

    void runConfig()
    {
        MDebug.LogBlue("[BOOT] runConfig");
        if (configBooter != null)
        {
            configBooter.bootstrap(runPostConfig);
        }
        else
        {
            runPostConfig();
        }
    }

    void runPostConfig()
    {
        MDebug.LogBlue("[BOOT] runPostConfig");
        chainControl.startBootChain(this, postConfigChain, loadTimeLogLevel, loadTimeVerbose, () =>
        {
            // we are going to pause a frame to let all the CPU spikes smoothout a wee bit
            completeChain();
        });
    }

    void completeChain()
    {
        MDebug.LogBlue("[BOOT] completeChain");
        List<BootableMonoBehaviour> completeChain = new List<BootableMonoBehaviour>(preConfigChain.Count + postConfigChain.Count + 1);
        completeChain.AddRange(preConfigChain);
        if (configBooter != null)
            completeChain.Add(configBooter);
        completeChain.AddRange(postConfigChain);
        chainControl.completeBootChain(this, completeChain, loadTimeLogLevel, loadTimeVerbose, () =>
        {
            // we are going to pause a frame to let all the CPU spikes smoothout a wee bit
            AnimationHelper.playOnNextFrame(this, () =>
            {
                NotificationServer.instance.postNotification("BootstrapDidCompleteNotification");
                MDebug.LogBlue("[BOOT] Game Start Event Invoke");
                gameStartEvent.Invoke();
            });
        });
    }
}
