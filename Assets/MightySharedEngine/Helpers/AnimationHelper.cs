using UnityEngine;
using System;
using System.Collections;
using System.Collections.Concurrent;

public enum AnimationCurveType
{
    linear,
    easeInEaseOut,
    easeIn,
    easeOut,
    ring,
    bounce,
    overshoot,
    UIPanelMoveIn,
    UIScalePop,
    UIScalePopIn,
    UIScalePopInSmall,
    UIScalePopOut,
    UIScalePopDown,
    mechanicalEaseInEaseOut,
    mechanicalEaseIn,
    mechanicalEaseOut
}

public sealed class AnimationHelper : SingletonMono<AnimationHelper>
{
    public AnimationCurve linear;
    public AnimationCurve easeInEaseOut;
    public AnimationCurve easeIn;
    public AnimationCurve easeOut;
    public AnimationCurve bounce;
    public AnimationCurve overshoot;
    public AnimationCurve ring;
    public AnimationCurve UIPanelMoveIn;

    [Header("UIAnimationCurves")]
    public AnimationCurve UIScalePop;
    public AnimationCurve UIScalePopIn;
    public AnimationCurve UIScalePopInSmall;
    public AnimationCurve UIScalePopOut;
    public AnimationCurve UIScalePopDown;

    [Header("AdvancedAnimationCurves")]
    public AnimationCurve mechanicalEaseInEaseOut;
    public AnimationCurve mechanicalEaseIn;
    public AnimationCurve mechanicalEaseOut;

    public AnimationCurve curveWithType(AnimationCurveType theType)
    {
        if (theType == AnimationCurveType.bounce) return bounce;
        if (theType == AnimationCurveType.linear) return linear;
        if (theType == AnimationCurveType.ring) return ring;
        if (theType == AnimationCurveType.easeIn) return easeIn;
        if (theType == AnimationCurveType.easeOut) return easeOut;
        if (theType == AnimationCurveType.overshoot) return overshoot;
        if (theType == AnimationCurveType.UIPanelMoveIn) return UIPanelMoveIn;
        if (theType == AnimationCurveType.UIScalePop) return UIScalePop;
        if (theType == AnimationCurveType.UIScalePopIn) return UIScalePopIn;
        if (theType == AnimationCurveType.UIScalePopInSmall) return UIScalePopInSmall;
        if (theType == AnimationCurveType.UIScalePopOut) return UIScalePopOut;
        if (theType == AnimationCurveType.UIScalePopDown) return UIScalePopDown;
        if (theType == AnimationCurveType.mechanicalEaseInEaseOut) return mechanicalEaseInEaseOut;
        if (theType == AnimationCurveType.mechanicalEaseIn) return mechanicalEaseIn;
        if (theType == AnimationCurveType.mechanicalEaseOut) return mechanicalEaseOut;

        return easeInEaseOut; // Default
    }

    private ConcurrentQueue<Action> executeOnMainThreadQueue = new ConcurrentQueue<Action>();
    public static void executeOnMainThread(Action theAction)
    {
        instance.executeOnMainThreadQueue.Enqueue(theAction);
    }

    private void Update()
    {
        // Only allow a certain amount of actions to run per frame so that we don't tank the framerate
        int availableDequeuesLeft = 10;

        Action theAction = null;
        while (availableDequeuesLeft > 0 && executeOnMainThreadQueue.TryDequeue(out theAction))
        {
            if (theAction != null)
            {
                theAction();
            }
            availableDequeuesLeft--;
        }
    }

    public static Coroutine lerpMe(float time, Action<float> lerpAction)
    {
        return AnimationHelper.lerpMe(null, time, lerpAction, null);
    }

    public static Coroutine lerpMe(float time, Action<float> lerpAction, Action completion)
    {
        return AnimationHelper.lerpMe(null, time, lerpAction, completion);
    }

    public static Coroutine lerpMe(MonoBehaviour script, float time, AnimationCurve shape, Action<float> lerpAction, Action completion)
    {
        return AnimationHelper.lerpMe(script, time, lerpAction, completion, shape);
    }

    public static Coroutine lerpMe(MonoBehaviour script, float time, AnimationCurve shape, Action<float> lerpAction)
    {
        return AnimationHelper.lerpMe(script, time, lerpAction, null, shape);
    }

    public static Coroutine lerpMe(MonoBehaviour script, float time, Action<float> lerpAction)
    {
        if (script == null || !script.isActiveAndEnabled)
        {
            return AnimationHelper.instance.StartCoroutine(AnimationHelper.doStandardLerp(time, lerpAction, null, AnimationHelper.instance.linear));
        }
        return script.StartCoroutine(AnimationHelper.doStandardLerp(time, lerpAction, null, AnimationHelper.instance.linear));
    }

    public static Coroutine lerpMe(MonoBehaviour script, float time, Action<float> lerpAction, Action completion)
    {
        return AnimationHelper.lerpMe(script, time, lerpAction, completion, AnimationHelper.instance.easeInEaseOut);
    }

    public static Coroutine lerpMe(MonoBehaviour script, float time, Action<float> lerpAction, Action completion, AnimationCurve shape)
    {
        if (script == null || !script.isActiveAndEnabled)
        {
            return AnimationHelper.instance.StartCoroutine(AnimationHelper.doStandardLerp(time, lerpAction, completion, shape));
        }
        return script.StartCoroutine(AnimationHelper.doStandardLerp(time, lerpAction, completion, shape));
    }

    static IEnumerator doStandardLerp(float time, Action<float> lerpAction, Action completion, AnimationCurve shape)
    {
        if (time == 0f)
        {
            lerpAction(shape.Evaluate(1.0f));
            if (completion != null) completion();
            yield break;
        }
        float fStartTime = Time.time;
        float fLerpLength = time;
        float fCurrLerp = (Time.time - fStartTime) / fLerpLength;

        while (fCurrLerp <= 1.0f)
        {
            fCurrLerp = (Time.time - fStartTime) / fLerpLength;
            lerpAction(shape.Evaluate(fCurrLerp));
            yield return null;
        }
        if (completion != null) completion();
    }

    public static Coroutine playWhenTrue(MonoBehaviour script, Func<bool> isTrueCheck, Action completion)
    {
        if (script.gameObject.activeInHierarchy)
        {
            return script.StartCoroutine(AnimationHelper.waitForTrue(isTrueCheck, completion));
        }

        return null;
    }

    public static Coroutine playWhenFalse(MonoBehaviour script, Func<bool> waitWhile, Action completion)
    {
        if (script.gameObject.activeInHierarchy)
        {
            return script.StartCoroutine(AnimationHelper.waitWhile(waitWhile, completion));
        }

        return null;
    }

    public static Coroutine playOnNextFrame(MonoBehaviour script, Action completion)
    {
        if (script.gameObject.activeInHierarchy)
        {
            return script.StartCoroutine(AnimationHelper.oneFrame(completion));
        }
        return null;
    }

    public static Coroutine playAfterFrames(MonoBehaviour script, int frameCount, Action completion)
    {
        if (script.gameObject.activeInHierarchy)
        {
            return script.StartCoroutine(AnimationHelper.waitForFrames(frameCount, completion));
        }
        return null;
    }

    static IEnumerator oneFrame(Action completion)
    {
        yield return null;
        completion();
    }

    static IEnumerator waitForFrames(int frameCount, Action completion)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
        completion();
    }

    static IEnumerator waitForTrue(Func<bool> isTrueCheck, Action completion)
    {
        yield return new WaitUntil(isTrueCheck);
        completion();
    }

    static IEnumerator waitWhile(Func<bool> isTrueCheck, Action completion)
    {
        yield return new WaitWhile(isTrueCheck);
        completion();
    }

    public static Coroutine playAfterDelay(MonoBehaviour script, float delay, Action completion)
    {
        if (delay <= 0.001f)
        {
            completion();
            return null;
        }
        return script.StartCoroutine(AnimationHelper.afterDelay(delay, completion));
    }

    static IEnumerator afterDelay(float delay, Action completion)
    {
        yield return new WaitForSeconds(delay);
        completion();
    }

    public static float lerpNoClamp(float start, float end, float lerpValue)
    {
        float diff = (end - start) * lerpValue;
        return start + diff;
    }

    public static Vector3 lerpNoClamp(Vector3 start, Vector3 end, float lerpValue)
    {
        return new Vector3(AnimationHelper.lerpNoClamp(start.x, end.x, lerpValue), AnimationHelper.lerpNoClamp(start.y, end.y, lerpValue), AnimationHelper.lerpNoClamp(start.z, end.z, lerpValue));
    }
}
