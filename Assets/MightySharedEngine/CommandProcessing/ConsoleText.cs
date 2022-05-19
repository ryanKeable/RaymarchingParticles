using UnityEngine;

public sealed class ConsoleText : MonoBehaviour
{
    public static string grey(string content)
    {
        return "<color=grey>" + content + "</color>";
    }

    public static string pink(string content)
    {
        return "<color=#FF66FF>" + content + "</color>";
    }

    public static string magenta(string content)
    {
        return "<color=magenta>" + content + "</color>";
    }

    public static string white(string content)
    {
        return "<color=white>" + content + "</color>";
    }

    public static string blue(string content)
    {
        return "<color=#66CCFF>" + content + "</color>";
    }

    public static string green(string content)
    {
        return "<color=green>" + content + "</color>";
    }

    public static string yellow(string content)
    {
        return "<color=yellow>" + content + "</color>";
    }

    public static string red(string content)
    {
        return "<color=red>" + content + "</color>";
    }
}
