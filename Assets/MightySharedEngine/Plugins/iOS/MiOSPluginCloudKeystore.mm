#import <Foundation/Foundation.h>

@interface MiOSPluginCloudKeystore : NSObject
{
}

+ (id)sharedPluginCloudKeystore;
+ (void)sendMessageToUnityObject:(NSString *)objectName methodName:(NSString *)methodName content:(NSString *)content;
+ (NSString *)CreateNSString:(const char *)string;
@end

@implementation MiOSPluginCloudKeystore

+ (id)sharedPluginCloudKeystore
{
    static MiOSPluginCloudKeystore *sharedMyManager = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
      sharedMyManager = [[self alloc] init];
    });
    return sharedMyManager;
}

// ******************************
// Key Value Store
// ******************************

- (void)startupKVStore
{
    // Register to observe notifications from the store
    [[NSNotificationCenter defaultCenter]
        addObserver:self
           selector:@selector(storeChanged:)
               name:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
             object:[NSUbiquitousKeyValueStore defaultStore]];

    NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];

    if (store)
    {
        [store setObject:[NSString stringWithFormat:@"%f", [[NSDate date] timeIntervalSince1970]] forKey:@"ztimeIntervalSince1970"];
    }
    // Get changes that might have happened while this instance of your app wasn't running
    [[NSUbiquitousKeyValueStore defaultStore] synchronize];
}

- (void)storeChanged:(NSNotification *)notification
{
    NSDictionary *userInfo = [notification userInfo];
    NSNumber *reason = [userInfo
        objectForKey:NSUbiquitousKeyValueStoreChangeReasonKey];

    if (reason)
    {
        NSInteger reasonValue = [reason integerValue];
        NSLog(@"[MiOSPluginCloudKeystore] storeChanged with reason %d", reasonValue);

        if ((reasonValue == NSUbiquitousKeyValueStoreServerChange) ||
            (reasonValue == NSUbiquitousKeyValueStoreInitialSyncChange))
        {
            [MiOSPluginCloudKeystore sendKVStoreToUnity];
        }
    }
}

+ (void)sendKVStoreToUnity
{
    NSLog(@"[MiOSPluginCloudKeystore] sendKVStoreToUnity");
    NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];
    if (store == nil)
    {
        [MiOSPluginCloudKeystore sendMessageToUnityObject:@"cloudStoreDidChange" content:@""];
        return;
    }
    NSDictionary *keysDict = [store dictionaryRepresentation];
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:keysDict options:NSJSONWritingPrettyPrinted error:nil];
    NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    [MiOSPluginCloudKeystore sendMessageToUnityObject:@"cloudStoreDidChange" content:jsonString];
}

// ******************************
// Misc
// ******************************

+ (void)sendMessageToUnityObject:(NSString *)methodName content:(NSString *)content
{
    //this will just call back into unity
    NSString *objectName = @"iOSListenerCloudKeystore";
    UnitySendMessage([objectName cStringUsingEncoding:NSUTF8StringEncoding], [methodName cStringUsingEncoding:NSUTF8StringEncoding], [content cStringUsingEncoding:NSUTF8StringEncoding]);
}

+ (NSString *)CreateNSString:(const char *)string
{
    if (string)
        return [NSString stringWithUTF8String:string];
    else
        return [NSString stringWithUTF8String:""];
}

@end

extern "C" {

    // ******************************
    // Key Value Store
    // ******************************

    void clearUbiquitousStore()
    {
        NSLog(@"[MiOSPluginCloudKeystore] clearUbiquitousStore");
        NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];
        if (store)
        {
            NSArray *keys = [[store dictionaryRepresentation] allKeys];
            for (NSString *key in keys)
            {
                [store setObject:@"" forKey:key];
            }
            [store synchronize];
        }
    }

    void setUbiquitousString(const char *key, const char *stringValue)
    {
        NSString *theKey = [MiOSPluginCloudKeystore CreateNSString:key];
        NSString *theValue = [MiOSPluginCloudKeystore CreateNSString:stringValue];

        NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];
        if (store)
        {
            [store setObject:theValue forKey:theKey];
            [store synchronize];
        }
        else
        {
            NSLog(@"[MiOSPluginCloudKeystore] setUbiquitousString: NO STORE!!");
        }
    }

    void requestUbiquitousStore()
    {
        [MiOSPluginCloudKeystore sendKVStoreToUnity];
    }

    void startupiCloudIntegration(const char *stringValue)
    {
        NSLog(@"[MiOSPluginCloudKeystore] Bootstrapping iCloud integration");
        [[MiOSPluginCloudKeystore sharedPluginCloudKeystore] startupKVStore];
    }

}
