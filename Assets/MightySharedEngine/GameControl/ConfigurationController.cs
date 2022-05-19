using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using MGG.Buildbot;

public sealed class ConfigurationController : SingletonMono<ConfigurationController>
{
    GenericJSONDictionaryBlob configBlob;
    Dictionary<string, CommandBlock> blocks = new Dictionary<string, CommandBlock>();

    public void bootstrap()
    {
        foreach (var config in BuildTimeConfigs.configMap)
        {
            EnvironmentController.addEnvironmentValueForKey(config.Value, config.Key);
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(configResourceLocation());
        configBlob = GenericJSONDictionaryBlob.blobFromString(jsonAsset.text);

        List<string> blockKeys = configBlob.allKeys();
        for (int i = 0; i < blockKeys.Count; i++)
        {
            CommandBlock newBlock = new CommandBlock(blockKeys[i], configBlob.listWithKey(blockKeys[i]));
            blocks[newBlock.blockKey] = newBlock;
        }
    }

    public static string levelFileLocation(string file)
    {
        return Path.Combine(Application.dataPath, "Resources", "LevelData", file + ".json");
    }

    public static string configFileLocation()
    {
        return Path.Combine(Application.dataPath, "Resources", "Config", "config.json");
    }

    public static string configDownloadTimeFileLocation()
    {
        return Path.Combine(Application.dataPath, "Resources", "Config", "configDownloadTime.txt");
    }

    public static string configResourceLocation()
    {
        return Path.Combine("Config", "config");
    }

    public static string configDownloadTimeResourceLocation()
    {
        return Path.Combine("Config", "configDownloadTime");
    }

    public CommandBlock commandBlockWithKey(string key)
    {
        CommandBlock block;
        if (blocks.TryGetValue(key, out block)) return block;
        return null;
    }

    // public void loadRawConfig(string raw)
    // {
    //     config.resetConfig(raw);
    // }

    public static List<Command> hackParse(string commandToGet)
    {
        List<Command> result = new List<Command>();

        TextAsset jsonAsset = Resources.Load<TextAsset>(configResourceLocation());
        GenericJSONDictionaryBlob configBlob = GenericJSONDictionaryBlob.blobFromString(jsonAsset.text);

        List<string> blockKeys = configBlob.allKeys();
        for (int i = 0; i < blockKeys.Count; i++)
        {
            CommandBlock newBlock = new CommandBlock(blockKeys[i], configBlob.listWithKey(blockKeys[i]));

            foreach (Command command in newBlock.commandList)
            {
                if (command.argList.Count > 0 && string.Equals(command.argList[0], commandToGet, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(command);
                }
            }
        }

        return result;
    }
}
