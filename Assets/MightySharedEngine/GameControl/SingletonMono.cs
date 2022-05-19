using UnityEngine;

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T sharedInstance;
    public static T instance
    {
        get
        {
            if (sharedInstance == null)
            {
                Debug.Log($"[SingletonMono] instance of {typeof(T).ToString()} is null, looking for one.");
                sharedInstance = FindObjectOfType<T>();
                if (sharedInstance == null)
                {
                    Debug.Log($"[SingletonMono] Could not locate a {typeof(T).ToString()}. Add one to the scene.");
                    return null;
                }
            }
            return sharedInstance;
        }
    }

    protected virtual void Awake()
    {
        if (sharedInstance == null) sharedInstance = this as T;
    }

    protected virtual void OnDestroy()
    {

    }

    protected virtual void ApplicationQuitting()
    {

    }
}
