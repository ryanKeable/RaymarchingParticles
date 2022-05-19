using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/*
EXAMPLE:

Signing up for a notification:

    NotificationServer.instance.addObserver(gameObject, "MenuBlockShouldHighlight", (object note) => {
        MenuEntry theBlock = note as MenuEntry;
        if (theBlock == this) {
            highlightItem(true);
        } else {
            highlightItem(false);
        }
    });

Posting a notification:

    NotificationServer.instance.postNotification("MenuBlockShouldHighlight", this);

Cleaning up:

    NotificationServer.instance.removeObserver(gameObject, "MenuBlockShouldHighlight");

 */

public sealed class Notification
{
    // Contains a gameobject and an action
    public GameObject theObject;
    public Action<object> theAction;
    public Action theSimpleAction;

    public Notification(GameObject obj, Action<object> act, Action simple)
    {
        theObject = obj;
        theAction = act;
        theSimpleAction = simple;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void send(object notificationObject)
    {
        if (theAction != null) theAction(notificationObject);
        if (theSimpleAction != null) theSimpleAction();
    }
}

public sealed class NotificationServer
{
    private static NotificationServer sharedInstance = null;

    private Dictionary<string, List<Notification>> notifications = new Dictionary<string, List<Notification>>(128, StringComparer.OrdinalIgnoreCase);

    // This defines a static instance property that attempts to find the manager object in the scene and
    // returns it to the caller.
    public static NotificationServer instance {
        get {
            if (sharedInstance == null) {
                sharedInstance = new NotificationServer();
            }
            return sharedInstance;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clearAllNotifications()
    {
        notifications.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void addObserver(GameObject owner, string notificationMessage, Action simpleNotificationAction)
    {
        addObserver(owner, notificationMessage, null, simpleNotificationAction);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void addObserver(GameObject owner, string notificationMessage, Action<object> notificationAction)
    {
        addObserver(owner, notificationMessage, notificationAction, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool observerExistsInList(GameObject owner, List<Notification> theList)
    {
        if (!Application.isPlaying) return false;

        for (int i = 0; i < theList.Count; i++) {
            if (theList[i].theObject == owner) return true;
        }
        return false;
    }

    public void addObserver(GameObject owner, string notificationMessage, Action<object> notificationAction, Action simpleNotificationAction)
    {
        if (!Application.isPlaying) return;
        if (owner == null || string.IsNullOrEmpty(notificationMessage)) return;

        List<Notification> theList;
        if (notifications.TryGetValue(notificationMessage, out theList)) {
            if (observerExistsInList(owner, theList)) {
                MDebug.LogError("[NotificationServer] Notification already exists: " + owner.name + " " + notificationMessage);
                return;
            }
            // list exists, but this observer is not in it
            theList.Add(new Notification(owner, notificationAction, simpleNotificationAction));
            notifications[notificationMessage] = theList;
        } else {
            //list doesnt exist, so we need to add it and the observer
            List<Notification> newList = new List<Notification>(16);
            newList.Add(new Notification(owner, notificationAction, simpleNotificationAction));
            notifications[notificationMessage] = newList;
        }
    }

    public void dumpListForNotification(string notificationMessage)
    {
        List<Notification> theList = new List<Notification>(notifications[notificationMessage]);
        for (int i = 0; i < theList.Count; i++) {
            MDebug.LogPink("############### " + theList[i].theObject.name + " " + notificationMessage);
        }
    }

    public void removeObserver(GameObject owner, string notificationMessage)
    {
        if (!Application.isPlaying) return;
        if (owner == null || string.IsNullOrEmpty(notificationMessage)) return;

        List<Notification> originalList = null;
        notifications.TryGetValue(notificationMessage, out originalList);
        if (originalList == null || originalList.Count == 0) return;

        // May be more than one, make a copy so I can adjust the original
        List<Notification> listCopy = new List<Notification>(originalList);
        List<Notification> itemsToRemove = new List<Notification>();
        for (int i = 0; i < listCopy.Count; i++) {
            if (listCopy[i].theObject == owner) {
                itemsToRemove.Add(listCopy[i]);
            }
        }
        for (int i = 0; i < itemsToRemove.Count; i++) {
            originalList.Remove(itemsToRemove[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void postNotification(string notificationMessage)
    {
        if (!Application.isPlaying) return;

        postNotification(notificationMessage, null);
    }

    public void postNotification(string notificationMessage, object notificationObject)
    {
        if (!Application.isPlaying) return;
        if (string.IsNullOrEmpty(notificationMessage)) return;

        List<Notification> theList;
        if (notifications.TryGetValue(notificationMessage, out theList)) {
            if (theList.Count > 0) {
                // NOTE 1: We need to make a copy of the list because it can change out from under us.
                // NOTE 2: We can't cache this list copy because 1 postNotification can call another postNotification and caching can introduce bugs. We just have to eat the GC Alloc.
                // NOTE 3: We can't just go backwards etc, since posting a notification may cause an object to remove itself from the observer pool.
                List<Notification> theListToNotify = new List<Notification>(theList);
                for (int i = 0; i < theListToNotify.Count; i++) {
                    if (theListToNotify[i] != null) theListToNotify[i].send(notificationObject);
                }
            }
        }
    }
}
