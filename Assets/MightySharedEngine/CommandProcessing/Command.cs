using UnityEngine;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public sealed class Command
{
    public List<string> argList = new List<string>();

    [SerializeField, HideInInspector]
    List<string> argListToggle = new List<string>();

    [HideInInspector]
    public bool success = false;
    [HideInInspector]
    public string error = "";
    string commandDelimiter = "\t";

    public Command(string raw = null, bool buildToggle = true, bool lowerCase = true, string delimiter = "\t")
    {
        if (raw == null) return;
        commandDelimiter = delimiter;
        setCommand(raw, buildToggle, lowerCase);
    }

    public Command(List<object> args, bool lowerCase = true)
    {
        success = true; // innocent until proven guilty
        error = "";
        argList.Clear();
        // gotta box em or un box em, whatever
        if (lowerCase)
        {
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i] as string;
                argList.Add(arg.ToLower());
                argListToggle.Add(arg);
            }
        }
        else
        {
            for (int i = 0; i < args.Count; i++)
            {
                argList.Add(args[i] as string);
                argListToggle.Add(args[i] as string);
            }
        }
    }

    public void setCommand(string raw, bool buildToggle = true, bool lowerCase = true)
    {
        success = true; // innocent until proven guilty
        error = "";
        argList.Clear();
        argList.AddRange(Command.lexLineTSV(raw, lowerCase, commandDelimiter[0]));
        if (buildToggle)
        {
            argListToggle.Clear();
            argListToggle.AddRange(Command.lexLineTSV(raw, false, commandDelimiter[0]));
        }
    }

    // this is used if you want to preserve the capitalization of the command
    // use with care! 
    bool isOriginal = false;
    public void recapitalize()
    {
        if (isOriginal) return;
        isOriginal = true;
        toggleLists();
    }

    public void reToLower()
    {
        if (!isOriginal) return;
        isOriginal = false;
        toggleLists();
    }

    void toggleLists()
    {
        List<string> swap = argList;
        argList = argListToggle;
        argListToggle = swap;
    }

    public string commandName()
    {
        if (argList.Count > 0) return argList[0];
        return "NOP"; // no operation
    }

    public void setError(string errorString)
    {
        success = false;
        error = errorString + " :: " + commandName();
    }

    public override string ToString()
    {
        return SystemHelper.listToString(argList);
    }

    public int argCount
    {
        get
        {
            return argList.Count;
        }
    }

    public string this[int key]
    {
        get
        {
            if (key >= argList.Count) return null;
            return argList[key];
        }
    }

    public bool boolForArg(int argIndex)
    {
        if (argIndex >= argList.Count) return false;
        return MathfExtensions.parseBool(argList[argIndex]);
    }

    public float floatForArg(int argIndex)
    {
        if (argIndex >= argList.Count) return 0f;
        return MathfExtensions.parseFloat(argList[argIndex]);
    }

    public int intForArg(int argIndex)
    {
        if (argIndex >= argList.Count) return -99999;
        return MathfExtensions.parseInt(argList[argIndex]);
    }

    // this is particularly slow, so you know, dont use it unless it really makes sense
    public string argForFlag(string flag)
    {
        int index = 0;
        // here we are lookgin for flagged args
        // these look like -flag <value>
        index = indexForArg(flag, argList);
        if (index < 0) return null;
        if (index + 1 >= argList.Count) return null;
        return argList[index + 1];
    }

    public int indexForArg(string arg, List<string> theList)
    {
        for (int i = 0; i < theList.Count; i++)
        {
            if (theList[i] == arg) return i;
        }
        return -1;
    }

    public List<string> slicedArgList(int startIndex)
    {
        List<string> destination = new List<string>();
        for (int i = startIndex; i < argList.Count; i++)
        {
            destination.Add(argList[i].Trim());
        }
        return destination;
    }

    public string subCommand(int startIndex)
    {
        if (startIndex >= argCount) return "";
        List<string> slices = slicedArgList(startIndex);
        if (slices == null || slices.Count == 0)
        {
            MDebug.LogError($"[Command] Invalid subCommand at startIndex '{startIndex}'");
            return "";
        }

        StringBuilder sub = new StringBuilder(slices[0]);
        for (int i = 1; i < slices.Count; i++)
        {
            sub.Append(commandDelimiter);
            sub.Append(slices[i]);
        }
        return sub.ToString();
    }

    public string joinedArgs(int startIndex)
    {
        return Command.joinArgs(slicedArgList(startIndex), commandDelimiter);
    }

    public static string joinArgs(List<string> args, string delimiter)
    {
        return string.Join(delimiter, args.ToArray());
    }

    public static List<string> lexLineTSV(string command, bool lowercaseEverything, char delimiter)
    {
        List<string> lexedTokens = new List<string>();
        string rawCommand = command;
        if (lowercaseEverything) rawCommand = command.ToLower();

        string[] tokens = rawCommand.Split(delimiter);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Length < 1)
            {
                lexedTokens.Add("");
            }
            else
            {
                lexedTokens.Add(tokens[i]);
            }
        }
        // lexing the tokens is pretty easy, just a split
        // then we need to look for variables
        return lexedTokens;
    }

    public static string lexToken(string theToken)
    {
        // looking for ${<variablename>} to replace
        if (!theToken.Contains("${")) return theToken;
        List<string> vars = new List<string>();
        int characterIndex = 0;
        StringBuilder currentVar = new StringBuilder();
        bool inVar = false;
        while (characterIndex < theToken.Length)
        {
            char thisCharacter = theToken[characterIndex];
            characterIndex++;
            if (thisCharacter == '{')
            {
                inVar = true;
                continue;
            }
            if (thisCharacter == '}')
            {
                inVar = false;
                string varString = currentVar.ToString();
                if (varString.Length > 0) vars.Add(varString);
                currentVar.Remove(0, currentVar.Length);
                continue;
            }
            if (inVar) currentVar.Append(thisCharacter);
        }
        for (int i = 0; i < vars.Count; i++)
        {
            string replacement = replacementFor(vars[i]);
            string replaceSource = "${" + vars[i] + "}";
            if (replacement != null) theToken = theToken.Replace(replaceSource, replacement);
        }
        return theToken.Trim();
    }

    public static string replacementFor(string token)
    {
        if (token == "br") return "\n";
        string theValue = EnvironmentController.stringForKey(token);
        if (theValue != null) return theValue;
        return "cannot find value for " + token;
    }
}
