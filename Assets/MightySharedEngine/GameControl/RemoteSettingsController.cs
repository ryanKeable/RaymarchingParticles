using UnityEngine;
using System.Collections.Generic;

public sealed class RemoteSettingsController : MonoBehaviour
{
#if UNITY_ANALYTICS

    List<string> remoteCommandKeys = new List<string>(8);
    bool bootChainCompleted = false;

    public static bool HasReceivedIsInChinaThisBoot { get; private set; } = false;
    public static bool HasReceivedIsInChinaAnyBoot {
        get { return PlayerPrefs.GetInt("rsc_heriic", 0) != 0; }
        private set { PlayerPrefs.SetInt("rsc_heriic", value ? 1 : 0); }
    }

    public void bootstrap()
    {
        MDebug.LogFern("[REMOTE SETTINGS] booting up");
        bootChainCompleted = false;
        string[] remoteKeys = RemoteSettings.GetKeys();
        MDebug.LogFern("[REMOTE SETTINGS] booting up: keycount:" + remoteKeys.Length);
        RemoteSettings.Updated += handleRemoteUpdate;
        NotificationServer.instance.addObserver(gameObject, "BootstrapDidCompleteNotification", bootChainDidComplete);
    }

    private void OnDestroy()
    {
        RemoteSettings.Updated -= handleRemoteUpdate;
    }

    private void bootChainDidComplete()
    {
        bootChainCompleted = true;
        processRemoteCommands();
    }

    // NOTE: this can be called anytime, but in my experience it is NEVER called because I dont know why
    private void handleRemoteUpdate()
    {
        MDebug.LogFern("[REMOTE SETTINGS] handleRemoteUpdate");
        // basically if the boot chain has not finished yet, then we will get the boot chain 
        // complete notification
        if (!bootChainCompleted) return;

        // if the boot chain is done, then we have take too long to finish the remote update and we should
        // go ahead and process the commands now
        processRemoteCommands();
    }

    // this should never run before the config has completed (in theory!)
    private void processRemoteCommands()
    {
        NotificationServer.instance.postNotification("RemoteSettingsWillUpdate"); // maybe we dont need this?
        remoteCommandKeys.Clear();

        string[] remoteKeys = RemoteSettings.GetKeys();
        MDebug.LogFern("[REMOTE SETTINGS] processRemoteCommands. keycount:" + remoteKeys.Length);
        for (int i = 0; i < remoteKeys.Length; i++) {
            handleKey(remoteKeys[i]);
        }

        if (remoteCommandKeys.Count == 0) return; // nothing to do 
        remoteCommandKeys.Sort(); // get the into order
        List<string> commandList = new List<string>();
        bool containsIsInChina = false;

        // go through all the command keys
        // get their command list and make a big ole list of commands
        for (int i = 0; i < remoteCommandKeys.Count; i++) {
            string commandString = RemoteSettings.GetString(remoteCommandKeys[i], null);
            if (commandString.Contains("IsInChina")) containsIsInChina = true;
            commandList.AddRange(breakdownCommand(commandString));
        }

        CommandDispatch.dispatcher.runCommands(commandList);

        if (containsIsInChina) {
            HasReceivedIsInChinaThisBoot = true;
            HasReceivedIsInChinaAnyBoot = true;
        }

        NotificationServer.instance.postNotification("RemoteSettingsDidUpdate");
    }

    private static List<string> breakdownCommand(string commandRaw)
    {
        List<string> commandList = new List<string>();
        if (commandRaw == null) return commandList;
        string[] commandLines = commandRaw.Split(new string[] { "[br]" }, System.StringSplitOptions.None);
        commandList.AddRange(commandLines);
        return commandList;
    }

    private static bool shouldHandleKey(string key)
    {
#if DEMO
        return key.ToLower().StartsWithFast("rc_demo_");
#else
        return key.StartsWithFast("rc_") && !key.ToLower().StartsWithFast("rc_demo_");
#endif
    }

    private void handleKey(string key)
    {
        if (shouldHandleKey(key)) {
            remoteCommandKeys.Add(key);
            return;
        }

        // other things?
        MDebug.LogFern("REMOTE SETTINGS] unhandled remote key: " + key);
    }

    // This is slow and should only be used in the case where the bootchain hasn't finished yet and therefore processRemoteCommands() hasn't run yet
    public static string findCommandThatContains(string containsSearchString)
    {
        string[] remoteKeys = RemoteSettings.GetKeys();
        for (int i = 0; i < remoteKeys.Length; i++) {
            if (!shouldHandleKey(remoteKeys[i])) continue;

            string commandString = RemoteSettings.GetString(remoteKeys[i], null);
            if (string.IsNullOrEmpty(commandString)) continue;

            List<string> commands = breakdownCommand(commandString);
            for (int j = 0; j < commands.Count; j++) {
                if (!commands[j].StartsWithFast("//") && commands[j].Contains(containsSearchString)) {
                    return commands[j];
                }
            }
        }

        return null;
    }

#else

    public static bool HasReceivedIsInChinaThisBoot { get { return false; } private set { } }
    public static bool HasReceivedIsInChinaAnyBoot { get { return false; } private set { } }
    public void bootstrap() { }
    public static string findCommandThatContains(string containsSearchString) { return null; }

#endif
}
