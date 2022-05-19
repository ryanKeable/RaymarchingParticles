using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

using System.Threading;

// disable the unused variable warning since the DEMO cuts out some code
#pragma warning disable CS0414

/*
    Description:
    CloudTime uses NTP to keep track of time and prevent users manipulation system time.

    TL;DR
    1. CloudTime has two modes:
     - Online     : Can be cheated by manipulating NTP traffic, or by blocking NTP traffic and then manipulating HTTPS request.
     - Offline    : Can be cheated by changing the system clock, but changes are tracked when the player comes back online.
    2. Use CloudTime.UtcTime() to get the time.
    3. Use CloudTime.IsReady() to check which mode you are in.
     - true: CloudTime.UtcTime() returns online time
     - false: CloudTime.UtcTime() returns offline time

    Delegates:
        CloudTimeWentOnlineDelegate - Called whenever CloudTime updates from an NTP server, typically shortly after Bootstrap is called.
        CloudTimeWentOfflineDelegate - Called when we detect abnormal time changes while the game is open, or if the app has been backgrounded for more than 10 minutes (awayManipulationThreshold).
        CloudTimeManipulatedDelegate - Called after CloudTimeWentOnlineDelegate if we detected time manipulation while the game was offline.
    Functions:
        IsReady   - Returns a bool, will be false until the device contacts an NTP server
        UtcTime   - If IsReady is true this function returns a trusted DateTime, if IsReady is false this function 
                    returns a best effort DateTime

    Using NTP to calculate an offset between system time and our server's time.
        - If the player is currently online, an NTP server is used to calculate an offset which should correct
          the system time to within a few seconds of our server time.
        - The offset is stored in player preferences for best effort when the device is offline.

*/

public sealed class CloudTime : BootableMonoBehaviour
{
    /*///////////////////////////////////|/
    /|//////// [Public external] ////////|/
    /|///////////////////////////////////*/

    public delegate void CloudTimeCallback();

    /// <summary>
    /// Called whenever CloudTime updates from an NTP server, typically shortly after Bootstrap is called. 
    /// </summary>
    public static CloudTimeCallback CloudTimeWentOnlineDelegate = delegate () { MDebug.LogGreen("[CloudTime] CloudTime is online."); };
    public static CloudTimeCallback CloudTimeWentOfflineDelegate = delegate () { MDebug.LogRed("[CloudTime] Unreliable time detected, going offline."); };
    public static CloudTimeCallback CloudTimeManipulatedDelegate = delegate () { MDebug.LogRed("[CloudTime] Cheating threashold met."); };

    /// <summary>
    /// Returns a bool, will be false until the device contacts an NTP server
    /// </summary>
    public static bool IsReady()
    {
#if DEMO
        return true;
#else
        CloudTime theInstance = instance;
        if (!theInstance.hasBooted) return false;

        bool ready = false;
        lock (isReadyLocker) {
            ready = theInstance.timeIsReady;
            if (!ready) {
                theInstance.checkCloudTime();
            }
        }
        return ready;
#endif
    }

    /// <summary>
    /// If IsReady is true this function returns a trusted DateTime, if IsReady is false this function returns a best effort DateTime
    /// </summary>
    public static DateTime UtcTime()
    {
        return instance.utcTimeWithOffset();
    }


    /*  /////////////////////////////////////
     *  ////// [Private, main thread] ///////
     *  /////////////////////////////////////
     */

    // -- Configuration --
    private List<string> CloudTimeNTPServerList = new List<string>() { };
    private string CloudTimeHTTPFallbackEndpoint = "";

    private int cheatingThreshold = 1200;
    private int runtimeManipulationThreshold = 60;
    private int awayManipulationThreshold = 600;

    // -- Singleton implementation -- 
    private static CloudTime _instance;

    // -- Ready state --
    private static System.Object isReadyLocker = new System.Object();
    private bool timeIsReady = false; // isReadyLocker - Threaded
    bool isCurrentlyChecking = false; // isReadyLocker - Threaded
    bool hasBooted = false;

    // -- Internal cooldowns and retry state --
    private int tryCount = 0;
    private static System.Object cooldownLocker = new System.Object();
    private static double retryCooldown; // cooldownLocker - Threaded
    private static int fibonacciCooldownOne = 1; // cooldownLocker - Threaded
    private static int fibonacciCooldownTwo = 1; // cooldownLocker - Threaded
    private static System.Object shouldRetryLocker = new System.Object();
    private bool shouldRetry = false; // shouldRetryLocker - Threaded

    // -- Time offset management --
    private static System.Object ntpTimeOffsetThreadedLocker = new System.Object();
    private double? ntpTimeOffsetThreaded = null; // ntpTimeOffsetThreadedLocker - Threaded
    private double ntpTimeOffset; // The main thread safe time offset between system time and 

    // -- Trust / cheating checks --
    private double rollingSystemTimestamp = 0;
    private double maxRollingSystemTimestamp = 0;
    private double timestampCache;

    string ntpServerAddress = "";

    //TODO: Luis - what is the case for running these checks in a separate thread? Could it be simplified?
    private Thread socketThread = null;

    Coroutine monitorCoroutine;

    // Singleton implementation
    private static CloudTime instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<CloudTime>();
                if (_instance == null) {
                    GameObject newGameObject = new GameObject("CloudTime");
                    _instance = newGameObject.AddComponent<CloudTime>();
                }
            }

            return _instance;
        }
    }

    public override void bootstrap(Action complete)
    {
        string keySuffix = (EnvironmentController.isInChina() ? "_China" : "");

        MDebug.Log("[CloudTime] Starting CloudTime");

        CloudTimeNTPServerList = new List<string>(){
            EnvironmentController.stringForKey("CloudTimeNTPServer1" + keySuffix),
            EnvironmentController.stringForKey("CloudTimeNTPServer2" + keySuffix),
            EnvironmentController.stringForKey("CloudTimeNTPServer3" + keySuffix)
        };

        CloudTimeHTTPFallbackEndpoint = EnvironmentController.stringForKey("CloudTimeHTTPFallbackEndpoint" + keySuffix);

        cheatingThreshold = EnvironmentController.intForKey("CloudTimeCheatingThreshold");
        runtimeManipulationThreshold = EnvironmentController.intForKey("CloudTimeRuntimeManipulationThreshold");
        awayManipulationThreshold = EnvironmentController.intForKey("CloudTimeAwayManipulationThreshold");

        // Load the last NTP offset as a best effort 
        ntpTimeOffset = MathfExtensions.parseDouble(PlayerPrefs.GetString("ntpTimeOffset", "0"), 0);
        tryCount = 0;
        retryCooldown = systemUnixTimestamp();
        updateRollingSystemTime();
        // We're done booting, set booted to true and kick off the NTP requests
        hasBooted = true;
#if !DEMO
        monitorCoroutine = StartCoroutine(MonitorCloudTime());
#endif

        complete();
    }


    private void SetCloudTimeOffline()
    {
        lock (isReadyLocker) {
            timeIsReady = false;
        }

        retryCooldown = systemUnixTimestamp();

        CloudTimeWentOfflineDelegate();
    }

    // 1. Read a new NTP value if it exist, this is where we get NTP values from the background thread back to the main thread
    // 2. Call checkCloudTime periodically, which will trigger an NTP check if CloudTime is offline. 
    // 3. Monitor the current time and compare it to an in-memory time from 1 second ago, this is one of the ways that we check for time manipulation.
    private IEnumerator MonitorCloudTime()
    {
        bool flipflop = false;
        bool shouldCallWentOnlineDelegate = false;
        bool shouldCallCheatedDelegate = false;

        while (true) {
            // Alternates between time checks and anti-cheat checks.
            // This Coroutine waits for 0.5 seconds each time, so each if / else branch should get run approximately once per second.
            flipflop = !flipflop;
            if (flipflop) {
                //  Read a new NTP value if it exist
                lock (ntpTimeOffsetThreadedLocker) {
                    if (ntpTimeOffsetThreaded != null) {
                        double newNTPOffset = (double)ntpTimeOffsetThreaded;

                        MDebug.Log("[CloudTime] new time offset: " + newNTPOffset);
                        if (Mathf.Abs((float)(ntpTimeOffset - newNTPOffset)) > cheatingThreshold) {
                            shouldCallCheatedDelegate = true;
                        }
                        if (maxRollingSystemTimestamp > systemUnixTimestamp() + cheatingThreshold) {
                            shouldCallCheatedDelegate = true;
                        }

                        ntpTimeOffset = newNTPOffset;
                        maxRollingSystemTimestamp = systemUnixTimestamp();

                        PlayerPrefs.SetString("ntpTimeOffset", ntpTimeOffset.ToString());
                        updateRollingSystemTime();

                        ntpTimeOffsetThreaded = null;

                        lock (isReadyLocker) {
                            isCurrentlyChecking = false;
                            timeIsReady = true;
                        }
                        lock (cooldownLocker) {
                            CooldownReset();
                        }

                        shouldCallWentOnlineDelegate = true;
                    }
                }

                // Delegate calls should happen outside of thread locks, because we don't know how long they will take to execute.
                if (shouldCallWentOnlineDelegate) {
                    CloudTimeWentOnlineDelegate();
                }
                if (shouldCallCheatedDelegate) {
                    CloudTimeManipulatedDelegate();
                }

                shouldCallWentOnlineDelegate = false;
                shouldCallCheatedDelegate = false;

                // Call checkCloudTime which will trigger an NTP check if CloudTime is offline. 
                checkCloudTime();
            } else {
                checkForTimeManipulation(runtimeManipulationThreshold);
            }

            // We don't have to check that often, so wait for half a second.
            yield return new WaitForSeconds(0.5f);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if !DEMO
        if (!hasFocus) {
            if (monitorCoroutine != null) {
                StopCoroutine(monitorCoroutine);
                monitorCoroutine = null;
            }
            updateRollingSystemTime();
        }
#endif
    }

    public static void ReturnFromBackground()
    {
        instance.checkForManipulationWhenReturningFromBackground();
    }

    private void checkForManipulationWhenReturningFromBackground()
    {
#if !DEMO
        checkForTimeManipulation(awayManipulationThreshold);
        monitorCoroutine = StartCoroutine(MonitorCloudTime());
#endif
    }

    void checkForTimeManipulation(int thresholdToTestAgainst)
    {
        timestampCache = systemUnixTimestamp();
        if (rollingSystemTimestamp + thresholdToTestAgainst < timestampCache) {
            SetCloudTimeOffline();
        } else if (rollingSystemTimestamp - thresholdToTestAgainst > timestampCache) {
            SetCloudTimeOffline();
        }
        updateRollingSystemTime();
    }

    void updateRollingSystemTime()
    {
        rollingSystemTimestamp = timestampCache;
        if (rollingSystemTimestamp > maxRollingSystemTimestamp) {
            maxRollingSystemTimestamp = rollingSystemTimestamp;
        }
    }

    // This is where we calculate what time it is! :D
    private DateTime utcTimeWithOffset()
    {
#if DEMO
        return DateTime.UtcNow;
#else
        return DateTime.UtcNow.AddSeconds(ntpTimeOffset);
#endif
    }

    void checkCloudTime()
    {
        // This is the most likely healthy state when checking Cloud Time so we'll put it at the top
        lock (isReadyLocker) {
            if (timeIsReady) {
                // CloudTime is ready, we don't have to do anything so move on
                // MDebug.LogGreen("[CloudTime] timeIsReady!");
                return;
            }
            // Are we already checking the time?
            if (isCurrentlyChecking) {
                // MDebug.LogPurple("[CloudTime] Already checking!");
                return;
            }
        }

        // Have we booted?
        if (!hasBooted) {
            // CloudTime has not booted so we can't make any requests.
            //MDebug.Log("[CloudTime] Has not booted!");
            return;
        }

        // Are we on cooldown?
        if (retryCooldown > systemUnixTimestamp()) {
            lock (shouldRetryLocker) {
                shouldRetry = true;
            }
            MDebug.Log("[CloudTime] CloudTime retry is on cooldown, we'll try again in " + (retryCooldown - systemUnixTimestamp() + " seconds."));
            return;
        }

        lock (isReadyLocker) {
            timeIsReady = false;
            // set isCurrentlyChecking to true, because we're about to kick off another time check
            isCurrentlyChecking = true;
        }

        MDebug.Log("[CloudTime] Fetching time");
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL doesn't support threads so the lastResort is our only option
        tryCount = 0;
#else
        tryCount--;
#endif

        // Should we try last resort?
        if (tryCount == 0) {
            StartCoroutine(MakeHttpsTimeRequest());
            return;
        } else if (tryCount < 0) {
            tryCount = CloudTimeNTPServerList.Count;
        }

        UpdateNetworkTimeOffset();
    }

    double systemUnixTimestamp()
    {
        return DateTime.UtcNow.Subtract(epoc()).TotalSeconds;
    }

    private IEnumerator MakeHttpsTimeRequest()
    {
        //MDebug.Log("[CloudTime] Failed to contact NTP servers. Falling back to HTTP");

        if (string.IsNullOrEmpty(CloudTimeHTTPFallbackEndpoint)) {
            // Oh well! ¯\_(ツ)_/¯
            Debug.LogError("[CloudTime] Last resort URL is empty.");
            NtpOrHttpsTimeRequestFailed();
            yield break;
        }

        UnityWebRequest www = UnityWebRequest.Get(CloudTimeHTTPFallbackEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log($"[CloudTime] Last resort failed, the player is probably offline. Error type: {www.result}");
            NtpOrHttpsTimeRequestFailed();
            yield break;
        } else {
            double seconds = 0L;
            Double.TryParse(www.downloadHandler.text, out seconds);
            DateTime networkDateTime = epoc().AddSeconds(seconds);

            NtpOrHttpsTimeRequestSucceeded(networkDateTime);
        }
    }

    DateTime epoc()
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private void NtpOrHttpsTimeRequestSucceeded(DateTime networkDateTime)
    {
        lock (ntpTimeOffsetThreadedLocker) {
            ntpTimeOffsetThreaded = (networkDateTime - DateTime.UtcNow).TotalSeconds;
        }
    }
    private void NtpOrHttpsTimeRequestFailed()
    {
        lock (isReadyLocker) {
            if (tryCount == 0) {
                lock (cooldownLocker) {
                    // Add to the internal cooldown
                    // Fibonacci series falloff
                    retryCooldown = CooldownGetValue() + systemUnixTimestamp();
                    CooldownIncrement();
                }
            } else {
                retryCooldown = systemUnixTimestamp() + 2;
            }
            timeIsReady = false;
            isCurrentlyChecking = false;
            lock (shouldRetryLocker) {
                shouldRetry = true;
            }
        }
    }

    // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // 
    // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // 
    // WARNING! Threading!! Tread lightly
    // Everything here basically runs on a background thread, so dont try to interact with the main thread here

    // Manage backoff when requests fail
    // Fibonacci series falloff
    // Lock cooldownLocker before use
    private void CooldownIncrement()
    {
        int temp = fibonacciCooldownOne + fibonacciCooldownTwo;
        fibonacciCooldownOne = fibonacciCooldownTwo;
        fibonacciCooldownTwo = temp;
    }

    private void CooldownReset()
    {
        fibonacciCooldownOne = 1;
        fibonacciCooldownTwo = 1;
    }

    private int CooldownGetValue()
    {
        return fibonacciCooldownOne + fibonacciCooldownTwo;
    }


    // NOTE: I took this from the unbiased time pro stuff and changed it a bit - Ben B
    private void UpdateNetworkTimeOffset()
    {
        ntpServerAddress = CloudTimeNTPServerList[tryCount - 1];
        socketThread = new Thread(new ThreadStart(MakeNtpRequest));
        socketThread.IsBackground = true;
        socketThread.Start();
    }

    private void MakeNtpRequest()
    {
        var ntpData = new byte[48];
        ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

        try {
            var addresses = Dns.GetHostEntry(ntpServerAddress).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = true;

            socket.ReceiveTimeout = 6000; // Milliseconds
            socket.SendTimeout = 6000;
            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            DateTime networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            NtpOrHttpsTimeRequestSucceeded(networkDateTime);
        } catch (Exception e) {
            Debug.Log($"[CloudTime] NtpImpl() failed with exception: {e?.ToString()}");
            NtpOrHttpsTimeRequestFailed();
        }
    }

    // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // 
    // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // // ** // 
}
