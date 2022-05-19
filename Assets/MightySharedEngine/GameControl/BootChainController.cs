using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

public sealed class BootChainController
{
    MonoBehaviour callingMonoBehaviour;
    List<BootableMonoBehaviour> bootChain = new List<BootableMonoBehaviour>();
    int loadTimeLogLevel = 0;
    bool loadTimeVerbose = false;
    Stopwatch loadTimeStopwatch = null;
    Action finalAction;
    int bootIndex = 0;
    public string bootID = "[BOOT]";

    public void startBootChain(MonoBehaviour callingMonoBehaviour, List<BootableMonoBehaviour> chain, int loadTimeLogLevel, bool loadTimeVerbose, Action completion)
    {
        this.callingMonoBehaviour = callingMonoBehaviour;
        this.loadTimeLogLevel = loadTimeLogLevel;
        this.loadTimeVerbose = loadTimeVerbose;
        finalAction = completion;

        bootChain.Clear();
        bootChain.AddRange(chain);

        bootIndex = 0;
        if (bootChain.Count == 0) {
            MDebug.LogBlue(bootID + " chain is empty");
            bootChainComplete();
            return;
        }

        startLoadTimeStopwatch();
        MDebug.LogSeaFoam("boot: " + bootChain[bootIndex]);

        bootChain[bootIndex].bootstrap(bootNext); // boot this one, and then have it call the next one
    }

    void bootNext()
    {
        if (loadTimeVerbose) stopLoadTimeStopwatch(bootChain[bootIndex], "bootstrap");

        NotificationServer.instance.postNotification("BootstrapProgressUpdateNotification", (float)bootIndex / (float)(bootChain.Count * 2));

        AnimationHelper.playOnNextFrame(callingMonoBehaviour, () => {
            bootIndex++;
            if (loadTimeVerbose) startLoadTimeStopwatch();
            if (bootIndex >= bootChain.Count) {
                bootStartComplete();
                return;
            }
            MDebug.LogSeaFoam("boot: " + bootChain[bootIndex]);
            bootChain[bootIndex].bootstrap(bootNext); // boot this one, and then have it call the next one
        });
    }


    public void completeBootChain(MonoBehaviour callingMonoBehaviour, List<BootableMonoBehaviour> chain, int loadTimeLogLevel, bool loadTimeVerbose, Action completion)
    {
        this.callingMonoBehaviour = callingMonoBehaviour;
        this.loadTimeLogLevel = loadTimeLogLevel;
        this.loadTimeVerbose = loadTimeVerbose;
        finalAction = completion;

        bootChain.Clear();
        bootChain.AddRange(chain);

        bootIndex = 0;
        if (bootChain.Count == 0) {
            MDebug.LogBlue(bootID + " chain is empty");
            bootChainComplete();
            return;
        }

        startLoadTimeStopwatch();
        bootChain[bootIndex].bootstrapDidComplete(bootCompleteNext); // boot this one, and then have it call the next one
    }


    // this runs after all the bootstraps have run
    void bootCompleteNext()
    {
        if (loadTimeVerbose) stopLoadTimeStopwatch(bootChain[bootIndex], "bootstrapDidComplete");

        NotificationServer.instance.postNotification("BootstrapProgressUpdateNotification", (float)(bootIndex + bootChain.Count) / (float)(bootChain.Count * 2));

        AnimationHelper.playOnNextFrame(callingMonoBehaviour, () => {
            bootIndex++;
            if (bootIndex >= bootChain.Count) {
                bootChainComplete();
                return;
            }

            if (loadTimeVerbose) startLoadTimeStopwatch();
            if (bootChain[bootIndex].needsCompletion) {
                MDebug.LogSeaFoam("boot complete: " + bootChain[bootIndex]);
                bootChain[bootIndex].bootstrapDidComplete(bootCompleteNext); // boot this one, and then have it call the next one
            } else {
                bootCompleteNext();
            }
        });
    }

    void bootStartComplete()
    {
        if (!loadTimeVerbose) stopLoadTimeStopwatch(null, "bootstrapDidStart");

        NotificationServer.instance.postNotification("BootstrapProgressUpdateNotification", 1f);

        if (finalAction != null) finalAction();
    }

    void bootChainComplete()
    {
        if (!loadTimeVerbose) stopLoadTimeStopwatch(null, "bootstrapDidComplete");

        NotificationServer.instance.postNotification("BootstrapProgressUpdateNotification", 1f);

        if (finalAction != null) finalAction();
    }

    void startLoadTimeStopwatch()
    {
        if (loadTimeLogLevel <= 0) return;

        if (loadTimeStopwatch == null) {
            loadTimeStopwatch = new Stopwatch();
            loadTimeStopwatch.Start();
        } else {
            loadTimeStopwatch.Restart();
        }
    }

    void stopLoadTimeStopwatch(BootableMonoBehaviour justBooted, string loadStage)
    {
        if (loadTimeLogLevel <= 0) return;
        if (loadTimeStopwatch == null) return;

        loadTimeStopwatch.Stop();
        string loadTime = loadTimeStopwatch.Elapsed.TotalSeconds.ToString("0.00");
        string loadStageName = (justBooted != null ? $"{justBooted.name}.{loadStage}" : "BootChainController Boot Process");
        string loadTimeString = $"[BootChainController] Load time for '{loadStageName}' was: {loadTime}s";

        if (loadTimeLogLevel == 1) Debug.Log(loadTimeString);
        else if (loadTimeLogLevel == 2) Debug.LogWarning(loadTimeString);
        else Debug.LogError(loadTimeString);
    }
}
