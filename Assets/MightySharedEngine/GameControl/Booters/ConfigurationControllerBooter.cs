using System;

public sealed class ConfigurationControllerBooter : BootableMonoBehaviour
{
    public override void bootstrap(Action completion)
    {
        MDebug.LogBlue("[CONFIGBOOTER] running config boot script...");
        ConfigurationController.instance.bootstrap();
        CommandDispatch.bootstrapScripts();
        completion();
    }

    public override void bootstrapDidComplete(Action completion)
    {
        completion();
    }
}
