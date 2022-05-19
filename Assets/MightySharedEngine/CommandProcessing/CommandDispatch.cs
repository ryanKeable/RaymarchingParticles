using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// this class handles the queueing and marshalling of commands and sends them one at a time to the command processor
// this could in theory have multiple command processors that handled different kinds of commands
// but for now, we will go with the one
[System.Serializable]
public sealed class CommandBlock
{
    public string blockKey = "";
    public List<Command> commandList = new List<Command>();
    public bool fast = false;

    public CommandBlock(string key)
    {
        blockKey = key;
    }

    public CommandBlock(string key, List<object> listOfLists)
    {
        blockKey = key;
        for (int i = 0; i < listOfLists.Count; i++)
        {
            Command cmd = new Command(listOfLists[i] as List<object>);
            commandList.Add(cmd);
        }
    }

    public void resetCommand()
    {
        commandList.Clear();
    }

    public void addCommands(string[] commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            addCommand(commands[i]);
        }
    }

    public void addCommands(List<string> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            addCommand(commands[i]);
        }
    }

    public void addCommand(string rawCommand)
    {
        if (fast)
        {
            Command theCommand = new Command(rawCommand, false, false);
            commandList.Add(theCommand);

        }
        else
        {
            Command theCommand = new Command(rawCommand);
            commandList.Add(theCommand);

        }
    }

    public override string ToString()
    {
        System.Text.StringBuilder dumpString = new System.Text.StringBuilder(1000);
        dumpString.AppendLine(blockKey);
        for (int i = 0; i < commandList.Count; i++)
        {
            dumpString.AppendLine(commandList[i].ToString());
        }
        return dumpString.ToString();
    }

    public CommandBlock Clone()
    {
        CommandBlock result = new CommandBlock(this.blockKey);
        result.commandList.AddRange(commandList); // TODO: Do we need to clone each individual Command in the list?
        return result;
    }
}


public sealed class CommandDispatch
{
    CommandBlock theBlock = new CommandBlock("adhoc");

    private static CommandDispatch sharedInstance;
    public static CommandDispatch dispatcher
    {
        get
        {
            if (sharedInstance == null)
            {
                sharedInstance = new CommandDispatch();
            }
            return sharedInstance;
        }
    }

    public static void bootstrapScripts()
    {
        SharedCommands.registerSharedCommands();
        CommandDispatch.runBlock("bootstrap");
    }

    public void splitAndExecuteFast(string commandBlock)
    {
        // then run the command
        // this is the fast path, so we are assuming the data is already lowercase etc
        string[] lines = commandBlock.Split('\n');
        Command theCommand = new Command();
        for (int i = 0; i < lines.Length; i++)
        {
            theCommand.setCommand(lines[i], false, false);

            if (executeCommand(theCommand))
            {
                if (!theCommand.success) MDebug.LogYellow("Command Failed: " + theCommand.error);
            }
            else
            {
                // MDebug.LogOrange("Unknown Command: " + theCommand.commandName());
            }
        }
    }

    public void splitAndExecute(string commandBlock)
    {
        // then run the command
        string[] lines = commandBlock.Split('\n');
        theBlock.resetCommand();
        theBlock.addCommands(lines);
        CommandDispatch.dispatcher.processCommandBlock(theBlock);
    }

    public void runCommand(string singleCommand)
    {
        // we need to convert this list to a command block
        theBlock.resetCommand();
        theBlock.addCommand(singleCommand);
        CommandDispatch.dispatcher.processCommandBlock(theBlock);
    }

    public void runCommands(List<string> commands)
    {
        // we need to convert this list to a command block
        theBlock.resetCommand();
        theBlock.addCommands(commands);
        CommandDispatch.dispatcher.processCommandBlock(theBlock);
    }

    public static void runBlock(string command)
    {
        if (ConfigurationController.instance == null)
        {
            MDebug.LogError("[CommandDispatch] ConfigurationController.instance is null");
            return;
        }

        CommandBlock theBlock = ConfigurationController.instance.commandBlockWithKey(command);
        if (theBlock == null)
        {
            MDebug.LogError("[CommandDispatch] Cannot find command: " + command);
            return;
        }
        CommandDispatch.dispatcher.processCommandBlock(theBlock);
    }

    Dictionary<string, Action<Command>> commands = new Dictionary<string, Action<Command>>(20, StringComparer.OrdinalIgnoreCase);

    public void clearCommandRegistry()
    {
        commands.Clear();
    }

    public void registerFunction(string command, Action<Command> delegateFunction)
    {
        string commandName = command.ToLower();
        commands[commandName] = delegateFunction;
    }

    public void processCommandBlock(CommandBlock block)
    {
        MDebug.LogSeaFoam($"processCommandBlock {block}");
        if (block == null || block.commandList == null)
        {
            return;
        }
        int count = block.commandList.Count;
        Command theCommand = null;
        for (int i = 0; i < count; i++)
        {
            theCommand = block.commandList[i];
            if (executeCommand(theCommand))
            {
                if (!theCommand.success) MDebug.LogYellow("Command Failed: " + theCommand.error);
            }
            else
            {
                // MDebug.LogOrange("Unknown Command: " + theCommand.commandName());
            }
        }
    }

    Action<Command> executor;
    public bool executeCommand(Command theCommand)
    {
        string commandName = theCommand?.commandName();
        if (string.IsNullOrEmpty(commandName))
        {
            MDebug.LogError("[CommandDispatch] Found empty/null commandName for command: " + theCommand?.ToString());
            return false;
        }

        if (commands.TryGetValue(commandName, out executor))
        {
            executor(theCommand);
            return true;
        }
        return false;
    }
}
