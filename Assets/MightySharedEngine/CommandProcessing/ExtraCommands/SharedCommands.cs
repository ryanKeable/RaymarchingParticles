using UnityEngine;
using System.Collections.Generic;

public sealed class SharedCommands
{
    public static bool checkArgCount(Command theCommand, int minArgs)
    {
        if (theCommand.argCount < minArgs)
        {
            theCommand.setError("Syntax error");
            return false;
        }
        return true;
    }

    public static void registerSharedCommands()
    {
        CommandDispatch theDispatcher = CommandDispatch.dispatcher;
        theDispatcher.registerFunction("run", (Command theCommand) => { SharedCommands.runScript(theCommand, theDispatcher); });

        theDispatcher.registerFunction("setArgs", SharedCommands.setArgs);
        theDispatcher.registerFunction("setExternalURL", SharedCommands.setExternalURL);
        theDispatcher.registerFunction("setEnv", SharedCommands.setEnv);
        theDispatcher.registerFunction("enableSystem", SharedCommands.enableSystem);
        theDispatcher.registerFunction("setGlobal", SharedCommands.setEnv);
        theDispatcher.registerFunction("setConfig", SharedCommands.setEnv);
        theDispatcher.registerFunction("setConfigStringArray", SharedCommands.setEnv);
        theDispatcher.registerFunction("setConfigIntArray", SharedCommands.setEnv);
        theDispatcher.registerFunction("setConfigFloatArray", SharedCommands.setEnv);
        theDispatcher.registerFunction("clearEnvCache", SharedCommands.clearEnvCache);

        theDispatcher.registerFunction("setVolume", (Command theCommand) => { SharedCommands.setEnvWithPrefix(theCommand, "sound volume "); });
        theDispatcher.registerFunction("setSound", (Command theCommand) => { SharedCommands.setEnvWithPrefix(theCommand, "sound key "); });
        theDispatcher.registerFunction("postNotification", SharedCommands.postNotification);
        theDispatcher.registerFunction("echo", SharedCommands.echo);

        theDispatcher.registerFunction("roll", Randomness.roll);
        theDispatcher.registerFunction("randomRange", Randomness.random);
    }

    private static void runScript(Command theCommand, CommandDispatch theDispatcher)
    {
        if (theCommand.argCount < 2)
        {
            theCommand.setError("Syntax error");
            return;
        }
        CommandBlock theBlock = ConfigurationController.instance.commandBlockWithKey(theCommand[1]);
        if (theBlock == null)
        {
            MDebug.LogError("[SharedCommands] Cannot find script: " + theCommand[1]);
            theCommand.setError("Cannot find script: " + theCommand[1]);
            return;
        }
        theDispatcher.processCommandBlock(theBlock);
    }

    private static void setExternalURL(Command theCommand)
    {
        theCommand.recapitalize();

        // setExternalURL	url_key	iOS	tvOS	GooglePlay	Amazon

        string theKey = theCommand[1];

        string theValue = "";

        if (EnvironmentController.marketplace() == EnvironmentMarketplace.iOS)
        {
            theValue = theCommand[2];
        }
        if (EnvironmentController.marketplace() == EnvironmentMarketplace.tvOS)
        {
            theValue = theCommand[3];
        }
        if (EnvironmentController.marketplace() == EnvironmentMarketplace.GooglePlay)
        {
            theValue = theCommand[4];
        }
        if (EnvironmentController.marketplace() == EnvironmentMarketplace.Amazon)
        {
            theValue = theCommand[5];
        }
        EnvironmentController.addEnvironmentValueForKey(theValue, theKey);
    }

    private static void setArgs(Command theCommand)
    {
        int argIndex = 0;
        string theArg = theCommand[argIndex + 1];
        while (theArg != null)
        {
            EnvironmentController.addEnvironmentValueForKey(theArg, "argv" + argIndex.ToString());
            argIndex++;
            theArg = theCommand[argIndex + 1];
        }
    }

    private static void enableSystem(Command theCommand)
    {
        // pretty low tech, just set some env vars like the older system does
        int index = 1;
        string theSystemID = theCommand[index++];
        bool enabledInEditor = theCommand.boolForArg(index++);
        bool enabledInDev = theCommand.boolForArg(index++);
        bool enabledInRelease = theCommand.boolForArg(index++);
        EnvironmentController.enableSystem(theSystemID, EnvironmentController.PRODUCTION_STAGE_EDITOR, enabledInEditor);
        EnvironmentController.enableSystem(theSystemID, EnvironmentController.PRODUCTION_STAGE_DEV, enabledInDev);
        EnvironmentController.enableSystem(theSystemID, EnvironmentController.PRODUCTION_STAGE_RELEASE, enabledInRelease);
    }

    private static void setEnv(Command theCommand)
    {
        string theValue = theCommand[2];
        string theKey = theCommand[1];
        EnvironmentController.addEnvironmentValueForKey(theValue, theKey);
    }

    private static void setEnvWithPrefix(Command theCommand, string prefix)
    {
        string theValue = theCommand[2];
        string theKey = prefix + theCommand[1];
        EnvironmentController.addEnvironmentValueForKey(theValue, theKey);
    }

    private static void clearEnvCache(Command theCommand)
    {
        EnvironmentController.instance.clearCache();
    }

    private static void postNotification(Command theCommand)
    {
        if (theCommand.argCount < 2)
        {
            theCommand.setError("Syntax error");
            return;
        }
        NotificationServer.instance.postNotification(theCommand[1], theCommand[2]); // If theCommand[2] is null, that is OK
    }

    private static void echo(Command theCommand)
    {
#if UNITY_EDITOR
        theCommand.recapitalize();
        MDebug.Log("[ECHO] " + ConsoleText.pink(theCommand.subCommand(1)) + " [Ticks:" + System.DateTime.Now.Ticks + "]");
#endif
    }
}
