using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.IO.Compression;

public sealed class SystemHelper
{
    public static string logList<T>(List<T> theList)
    {
        StringBuilder theLog = new StringBuilder("<color=white>");
        theLog.Append(listToString(theList));
        theLog.Append("</color>");
        return theLog.ToString();
    }

    public static string listToString<T>(List<T> theList)
    {
        StringBuilder theLog = new StringBuilder("[");
        for (int i = 0; i < theList.Count; i++)
        {
            if (i > 0) theLog.Append(",");
            theLog.Append("\"" + theList[i].ToString() + "\"");
        }
        theLog.Append("]");
        return theLog.ToString();
    }

    public static string listToString<T>(List<T> theList, string separator)
    {
        StringBuilder theLog = new StringBuilder("");
        for (int i = 0; i < theList.Count; i++)
        {
            if (i > 0) theLog.Append(separator);
            theLog.Append(theList[i].ToString());
        }
        return theLog.ToString();
    }

    public static string IEnumerableToString<T>(IEnumerable<T> theList)
    {
        StringBuilder theLog = new StringBuilder("[");
        bool doneFirst = false;
        foreach (T item in theList)
        {
            if (doneFirst)
            {
                theLog.Append(",");
            }
            else
            {
                doneFirst = true;
            }
            theLog.Append("\"" + item.ToString() + "\"");
        }
        theLog.Append("]");
        return theLog.ToString();
    }

    public static void setItemsFromLinesOfText(List<string> theList, string raw)
    {
        theList.Clear();
        string[] lines = raw.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length < 1) continue;
            theList.Add(lines[i].ToLower());
        }
    }

    public static string itemsAsText(List<string> theList)
    {
        string list = "";
        for (int i = 0; i < theList.Count; i++)
        {
            list += theList[i] + "\n";
        }
        return list;
    }

    public static string GetDirectoryNameSafe(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return filePath;

        // First, let's try the built-in Path.GetDirectoryName. It is a bit too eager to throw exceptions though so let's try-catch it.
        try
        {
            return Path.GetDirectoryName(filePath);
        }
        catch (Exception e)
        {
            MDebug.Log($"[SystemHelper] GetDirectoryNameSafe error (this is probably harmless): {e?.ToString()}");
        }

        // Path.GetDirectoryName failed so let's try some fallbacks
        int index = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        if (index == 0) return string.Empty;
        else if (index > 0) return filePath.Substring(0, index);

        index = filePath.LastIndexOf(Path.AltDirectorySeparatorChar);
        if (index == 0) return string.Empty;
        else if (index > 0) return filePath.Substring(0, index);

        MDebug.LogError($"[SystemHelper] GetDirectoryNameSafe failed to find the file name in the path: {filePath}");
        return string.Empty;
    }

    public static string GetFileNameSafe(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return filePath;

        // First, let's try the built-in Path.GetFileName. It is a bit too eager to throw exceptions though so let's try-catch it.
        try
        {
            return Path.GetFileName(filePath);
        }
        catch (Exception e)
        {
            MDebug.Log($"[SystemHelper] GetFileNameSafe error (this is probably harmless): {e?.ToString()}");
        }

        // Path.GetFileName failed so let's try some fallbacks
        int index = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        int filePathLength = filePath.Length;
        if (index == (filePathLength - 1)) return string.Empty;
        else if (index >= 0) return filePath.Substring(index + 1);

        index = filePath.LastIndexOf(Path.AltDirectorySeparatorChar);
        if (index == (filePathLength - 1)) return string.Empty;
        else if (index >= 0) return filePath.Substring(index + 1);

        MDebug.LogError($"[SystemHelper] GetFileNameSafe failed to find the file name in the path: {filePath}");
        return string.Empty;
    }

    public static string streamingAssetsDirectory()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            return Application.dataPath + "/Raw/";
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            return Application.dataPath + "!/assets/";
        }
        // Otherwise desktop:
        return Application.dataPath + "/StreamingAssets/";
    }

    public static string resourcesDirectory()
    {
        return Path.Combine(Application.dataPath, "Resources");
    }

    public static bool stringsMatch(string a, string b)
    {
        return (string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase));
    }

    public static string streamingAssetsDirectoryURL()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            return "file://" + SystemHelper.streamingAssetsDirectory();
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            return "jar:file://" + SystemHelper.streamingAssetsDirectory();
        }
        // Otherwise desktop:
        return "file://" + SystemHelper.streamingAssetsDirectory();
    }

    public static bool saveStringToFile(string data, string filepath)
    {
        string dir = Path.GetDirectoryName(filepath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return SystemHelper.writeStringToFile(filepath, data);
    }

    public static string guid()
    {
        Guid g = Guid.NewGuid();
        return g.ToString();
    }

    public static bool writeBytesToFile(string filePath, byte[] data, bool append)
    {
        string documentsDirectory = SystemHelper.documentsDirectory();
        if (documentsDirectory == null)
        {
            Debug.LogError("[SystemHelper] writeBytesToFile error: Can't write to documents directory on this platform");
            return false;
        }

        string atomicFilePath = null;

        try
        {
            if (!Directory.Exists(documentsDirectory))
            {
                Directory.CreateDirectory(documentsDirectory);
            }

            if (append)
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(filePath, FileMode.Append)))
                {
                    bw.Write(data);
                }
                return true;
            }
            else
            {
                string atomicDirectory = GetDirectoryNameSafe(filePath);
                if (string.IsNullOrEmpty(atomicDirectory))
                {
                    Debug.LogError($"[SystemHelper] writeBytesToFile failed to get directory from filePath: {filePath}");
                    return false;
                }
                atomicFilePath = Path.Combine(atomicDirectory, Path.GetRandomFileName());

                using (BinaryWriter bw = new BinaryWriter(File.Open(atomicFilePath, FileMode.Create)))
                {
                    bw.Write(data);
                }
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(atomicFilePath, filePath);

                return true;
            }
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] writeBytesToFile error: {err?.ToString()}");
        }

        // Do we need to clean up the temp file after an exception happened above?
        if (!string.IsNullOrEmpty(atomicFilePath))
        {
            try
            {
                if (File.Exists(atomicFilePath))
                {
                    File.Delete(atomicFilePath);
                }
            }
            catch (Exception err)
            {
                Debug.LogError($"[SystemHelper] writeBytesToFile error: {err?.ToString()}");
            }
        }

        return false;
    }

    public static string documentFilePath(string lastPathComponent)
    {
        string documentsDirectory = SystemHelper.documentsDirectory();
        if (documentsDirectory == null)
            return null;
        else
            return Path.Combine(documentsDirectory, lastPathComponent);
    }

    public static bool writeStringToFile(string filePath, string data)
    {
        string documentsDirectory = SystemHelper.documentsDirectory();
        if (documentsDirectory == null)
        {
            Debug.LogError("[SystemHelper] writeStringToFile error: Can't write to documents directory on this platform");
            return false;
        }
        File.WriteAllText(filePath, data);

        return true;
    }

    public static string stringWithContentsOfFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }
        // Read a file
        try
        {
            string contents = null;
            // contents = File.ReadAllText(filePath);
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs, Encoding.UTF8))
                {
                    // read the stream
                    contents = sr.ReadToEnd();
                }
            }

            if (string.IsNullOrEmpty(contents))
            {
                return null; // Zero length files are bad.
            }
            return contents;
        }
        catch (FileNotFoundException err)
        {
            Debug.Log($"[SystemHelper] Did not find file (This message is harmless). Error: {err?.ToString()}");
            return null;
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] stringWithContentsOfFile error: {err?.ToString()}");
            return null;
        }
    }

    public static string getReadErrorForFile(string filePath)
    {
        try
        {
            string contents = null;

            using (StreamReader sr = new StreamReader(filePath))
            {
                contents = sr.ReadToEnd();
            }
            if (string.IsNullOrEmpty(contents))
            {
                return "Zero length file";
            }
            return "";
        }
        catch (Exception err)
        {
            return err?.Message ?? "null exception";
        }
    }

    public static void deleteFileAtPath(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] deleteFileAtPath error: {err?.ToString()}");
        }
    }

    public static bool fileExists(string filePath)
    {
        try
        {
            return File.Exists(filePath);
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] fileExists error: {err?.ToString()}");
            return false;
        }
    }

    public static void moveFile(string filePathOriginal, string filePathNew)
    {
        try
        {
            if (File.Exists(filePathNew))
            {
                File.Delete(filePathNew);
            }

            File.Move(filePathOriginal, filePathNew);
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] deleteFileAtPath error: {err?.ToString()}");
        }
    }

    public static string[] getFiles(string path)
    {
        try
        {
            return Directory.GetFiles(path);
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] getFiles error: {err?.ToString()}");
            return new string[0];
        }
    }

    public static long getFileSize(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return -1L;
            }

            FileInfo file = new FileInfo(filePath);
            return file.Length;
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] removeFileAtPath error: {err?.ToString()}");
            return -1L;
        }
    }

    public static DateTime getLastModifiedDate(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return DateTime.MinValue;
            }

            return File.GetLastWriteTime(filePath);
        }
        catch (Exception err)
        {
            Debug.LogError($"[SystemHelper] getLastModifiedDate error: {err?.ToString()}");
            return DateTime.MinValue;
        }
    }

    private static string documentsDirectoryCache = null;
    public static string documentsDirectory()
    {
        if (documentsDirectoryCache != null) return documentsDirectoryCache;

#if UNITY_EDITOR
        documentsDirectoryCache = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
        documentsDirectoryCache = documentsDirectoryCache.Substring(0, documentsDirectoryCache.LastIndexOf('/')) + "/Documents/";
        if (!Directory.Exists(documentsDirectoryCache))
        {
            Directory.CreateDirectory(documentsDirectoryCache);
        }
#elif UNITY_PS4 || UNITY_XBOXONE || UNITY_SWITCH
        documentsDirectoryCache = null;
#elif UNITY_TVOS
        documentsDirectoryCache = Application.temporaryCachePath + "/";
#else
        documentsDirectoryCache = Application.persistentDataPath + "/";
#endif

        return documentsDirectoryCache;
    }

    public static string playerPrefsSavePath()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"Library/Preferences/unity.{Application.companyName}.{Application.productName}.plist");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // TODO: Windows PlayerPrefs aren't stored in a file but are instead stored in the registry which makes reading from them harder. Unity PlayerPrefs are stored at the following key: $"HKCU\\Software\\{Application.companyName}\\{Application.productName}"
        Debug.LogError("[SystemHelper] playerPrefsSavePath() hasn't been implemented for this platform");
        return null;
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $".config/unity3d/{Application.companyName}/{Application.productName}");
#elif UNITY_IOS
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"Library/Preferences/{Application.identifier}.plist");
#elif UNITY_ANDROID
        return $"/data/data/{Application.identifier}/shared_prefs/{Application.identifier}.v2.playerprefs.xml";
#else
        Debug.LogError("[SystemHelper] playerPrefsSavePath() hasn't been implemented for this platform");
        return null;
#endif
    }

    public static DateTime unixTimeStampToDateTimeUTC(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
        return dtDateTime;
    }

    public static string timeToSimpleString(TimeSpan interval)
    {
        if (interval.TotalSeconds <= 0f) return "00:00:00";
        int hours = interval.Hours;
        int minutes = interval.Minutes;
        int seconds = interval.Seconds;

        string hourText = hours.ToString("D2");
        string minuteText = minutes.ToString("D2");
        string secondText = seconds.ToString("D2");

        return hourText + ":" + minuteText + ":" + secondText;
    }

    public static void destroyChildren(Transform transform, bool immediate)
    {
        if (!Application.isPlaying) immediate = true; // Always immediate in edit mode

        List<Transform> kids = new List<Transform>();
        int count = transform.childCount;
        for (int i = 0; i < count; i++)
        {
            kids.Add(transform.GetChild(i));
        }
        for (int i = 0; i < kids.Count; i++)
        {
            if (immediate)
            {
                GameObject.DestroyImmediate(kids[i].gameObject);
            }
            else
            {
                GameObject.Destroy(kids[i].gameObject);
            }
        }
    }

    public static string animationCurveToString(AnimationCurve curve)
    {
        StringBuilder theString = new StringBuilder();
        Keyframe[] keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (i > 0) theString.Append(",");
            theString.Append(keyframeToString(keys[i]));
        }
        return theString.ToString();
    }

    public static AnimationCurve animationCurveFromString(string raw)
    {
        MDebug.LogGreen("animationCurveFromString " + raw);
        if (raw == null || raw.Length <= 2) return new AnimationCurve(); // Empty

        string[] tokens = raw.Replace("},{", "}:{").Split(':'); // This is a bit barf
        AnimationCurve newCurve = new AnimationCurve();
        for (int i = 0; i < tokens.Length; i++)
        {
            newCurve.AddKey(keyframeFromString(tokens[i]));
        }
        return newCurve;
    }

    // Not meant to be particularly fast or garbage friendly
    public static string keyframeToString(Keyframe frame)
    {
        StringBuilder theString = new StringBuilder();
        theString.Append("{");
        theString.Append(frame.time.ToString("N4"));
        theString.Append(",");
        theString.Append(frame.value.ToString("N4"));
        theString.Append(",");
        theString.Append(frame.inTangent.ToString("N4"));
        theString.Append(",");
        theString.Append(frame.outTangent.ToString("N4"));
        theString.Append("}");
        return theString.ToString();
    }

    public static Keyframe keyframeFromString(string raw)
    {
        MDebug.LogBlue("keyframeFromString " + raw);
        // Looks like {time,value,in,out}
        if (raw == null || raw.Length <= 2) return new Keyframe(); // Empty frame, probably just {} 
        string[] tokens = raw.Split(',');
        if (tokens.Length < 4) return new Keyframe(); // Empty frame
        float time = MathfExtensions.parseFloat(tokens[0].Substring(1)); // Ignore the leading {
        float value = MathfExtensions.parseFloat(tokens[1]); // Ignore the leading {
        float inTangent = MathfExtensions.parseFloat(tokens[2]); // Ignore the leading {
        float outTangent = MathfExtensions.parseFloat(tokens[3].Substring(0, tokens[3].Length - 1)); // Ignore the trailing }
        return new Keyframe(time, value, inTangent, outTangent);
    }


    public static string runTimeArgForFlag(string flag, string defaultValue)
    {
        //is there a command line arg?
        string finalArg = "";
        string[] ARGS = System.Environment.GetCommandLineArgs();
        bool inFlag = false;
        for (int i = 0; i < ARGS.Length; i++)
        {
            if (inFlag)
            {
                if (ARGS[i].StartsWithFast("-"))
                {
                    // we are done
                    inFlag = false;
                    break;
                }
                if (finalArg.Length > 0)
                {
                    finalArg += " ";
                }
                finalArg += ARGS[i];
                continue;
            }
            if (ARGS[i] == flag)
            {
                inFlag = true;
            }
        }
        if (finalArg.Length == 0)
        {
            return defaultValue;
        }
        finalArg = finalArg.Replace("\"", "");
        return finalArg;
    }

    public static void deleteZombieFiles()
    {
        string bigList = PlayerPrefs.GetString("testwall_to_delete_later", "");
        if (bigList.Length == 0) return;
        MDebug.Log($"Removing Zombie Files: {bigList}");

        string[] filePaths = bigList.Split(',');
        PlayerPrefs.SetString("testwall_to_delete_later", "");
        for (int i = 0; i < filePaths.Length; i++)
        {
            removePath(filePaths[i]);
        }
    }

    public static void removePath(string path, bool secondTry = false)
    {
        try
        {
            if (Directory.Exists(path))
            {
                MDebug.Log($"Remove Directory: {path}");
                Directory.Delete(path, true);
                return;
            }
            if (File.Exists(path))
            {
                MDebug.Log($"Remove File: {path}");
                File.Delete(path);
                return;
            }
        }
        catch (IOException e)
        {
            MDebug.Log($"Remove Path FAIL: {e} [second try? {secondTry}]");
            if (secondTry)
            {
                // rename it and try again later
                // windows is the worst. I really just want this file to GO AWAY
                // this almost never works BTW.. sigh
                string dest = path + "_" + DateTime.Now.Ticks.ToString();
                // need to clear this up later
                string bigList = PlayerPrefs.GetString("testwall_to_delete_later", "");
                if (bigList.Length > 0)
                {
                    bigList += "," + path;
                }
                else
                {
                    bigList = path;
                }
                PlayerPrefs.SetString("testwall_to_delete_later", bigList);
                if (Directory.Exists(path))
                {
                    MDebug.Log($"Rename Directory: {path}");
                    Directory.Move(path, dest);
                    return;
                }
                if (File.Exists(path))
                {
                    MDebug.Log($"Rename File: {path}");
                    File.Move(path, dest);
                    return;
                }
                return;
            }
            removePath(path, true);
        }
        MDebug.Log($"Path does not exist: {path}");
    }

    // public static void zip(string sourceDirectory, string destinationZip)
    // {
    //     MDebug.LogGreen($"Zipping {sourceDirectory} {destinationZip}");
    //     ZipFile.CreateFromDirectory(sourceDirectory, destinationZip);
    //     if (File.Exists(destinationZip))
    //     {
    //         MDebug.LogGreen($"Zipping SUCCESS!");
    //     }
    //     else
    //     {
    //         MDebug.LogGreen($"Zipping FAIL!");
    //     }
    // }

    // public static void unzip(string zipFile, string destination)
    // {
    //     if (!File.Exists(zipFile))
    //     {
    //         MDebug.Log("<color=red>CANNOT FIND: " + zipFile + "</color>");
    //         return;
    //     }

    //     MDebug.Log("<color=red>Extract to: " + destination + "</color>");
    //     ZipFile.ExtractToDirectory(zipFile, destination);
    //     MDebug.Log("<color=red>Extracted</color>");
    // }

    public static bool pathIsDirectory(string path)
    {
        FileAttributes attr = File.GetAttributes(path);

        if (attr.HasFlag(FileAttributes.Directory))
            return true;
        return false;
    }

    public static string findDirectoryPath(string directory, string searchString)
    {
        if (string.IsNullOrEmpty(directory)) return null;
        if (!Directory.Exists(directory)) return null;
        foreach (string dir in Directory.EnumerateDirectories(directory, searchString, SearchOption.AllDirectories))
        {
            return dir;
        }
        return null;
    }

    // i think there is maybe a way to just do this natively with Directory.blah blah
    // but I havent found it yet
    public static string findFilePath(string directory, string searchString)
    {
        if (string.IsNullOrEmpty(directory)) return null;
        if (!Directory.Exists(directory)) return null;
        foreach (string file in Directory.EnumerateFiles(directory, searchString, SearchOption.AllDirectories))
        {
            return file;
        }
        return null;
    }

    public static string runCommandAndWaitForExit(string command, string args, string workingDirectory = "")
    {
        if (Application.platform == RuntimePlatform.WindowsEditor && (command == "bash" || command == "chmod"))
        {
            MDebug.LogError($"[BuildFromConfig] Windows Editor can't run the command: {command}");
            return "";
        }

        // MDebug.Log("runCommandAndWaitForExit: " + command + " " + args);
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = command;
        startInfo.CreateNoWindow = true;
        startInfo.Arguments = args;
        startInfo.WorkingDirectory = workingDirectory;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        MDebug.Log($"runCommand: {command} {args}");

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo = startInfo;
        process.Start();
        StringBuilder result = new StringBuilder(10);
        while (!process.HasExited)
        {
            result.Append(process.StandardOutput.ReadToEnd());
        }

        if (process.ExitCode == 0)
        {
            return result.ToString();
        }
        // Fail
        string bad = process.StandardError.ReadToEnd();
        return "[1]" + bad; // This is a shit hack
    }


    // returns a rect that is the same aspect as the child, but
    // fits within the container
    public static Rect fitRect(Rect child, Rect container)
    {
        Rect result = new Rect();

        // we can fit it by height or width, let us try width first
        float childAspect = child.width / child.height; // 1920/1080 = 1.777
        if (childAspect > 1)
        {
            // landscape
            result.width = container.width;
            result.height = container.width / childAspect;
        }
        else
        {
            result.height = container.height;
            result.width = container.height * childAspect;
        }

        return result;
    }

    public static bool isNone(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return true;
        if (source.Equals("none", StringComparison.OrdinalIgnoreCase)) return true;
        if (source.Equals("na", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static int guidInt()
    {
        System.Guid guid = System.Guid.NewGuid();
        byte[] guidBytes = guid.ToByteArray();

        if (System.BitConverter.IsLittleEndian)
        {
            System.Array.Reverse(guidBytes);
        }

        int guidInt = System.BitConverter.ToInt32(guidBytes, 0);

        return guidInt;
    }

    public static string guidString()
    {
        System.Guid guid = System.Guid.NewGuid();
        return guid.ToString();
    }

    // also works in windows
    public static void openInFinder(string path)
    {
#if !MGG_AUTOTESTER
        try
        {
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                System.Diagnostics.Process.Start("open", $"-R {path}");
                return;
            }
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                path = path.Replace(@"/", @"\");   // explorer doesn't like front slashes
                System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
                return;
            }
            // otherwise go the fil:// route
            Application.OpenURL($"file://{path}");
        }
        catch (Exception e)
        {
            MDebug.LogGreen("Somehting bad! " + e.ToString());
        }
#endif
    }
}
