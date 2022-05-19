using UnityEngine;
using System;

public sealed class RemoteSettingsControllerBooter : BootableMonoBehaviour
{
    public RemoteSettingsController itemToBoot;
    public override void bootstrap(Action completion)
    {
        if (itemToBoot == null) itemToBoot = FindObjectOfType<RemoteSettingsController>();
        if (itemToBoot == null) {
            MDebug.LogError("[RemoteSettingsControllerBooter] Cannot find RemoteSettingsController to boot it.");
            return;
        }
        itemToBoot.bootstrap();
        completion();
    }
}
