using UnityEngine;

public sealed class CommandTester : MonoBehaviour
{
    void bootstrap()
    {
        // BOOTSTRAP EVERYTHING HERE

        SharedCommands.registerSharedCommands();
	    CommandDispatch.runBlock("bootstrap");
    }

    void Start()
    {
        bootstrap();
    }
}
