using UnityEngine;
using System;

public class BootableMonoBehaviour : MonoBehaviour
{
    public virtual void bootstrap(Action completion)
    {
        completion();
    }

    public bool needsCompletion = false;

    public virtual void bootstrapDidComplete(Action completion)
    {
        completion();
    }
}
