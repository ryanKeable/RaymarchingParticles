#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>
#import <StoreKit/StoreKit.h>

#if TARGET_OS_IOS
#import <CoreTelephony/CTCarrier.h>
#import <CoreTelephony/CTTelephonyNetworkInfo.h>
#endif

#include "UnityAppController.h"
#include "UI/UnityView.h"

char* cStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;

    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);

    return res;
}

extern "C" {

#if TARGET_IPHONE_SIMULATOR

    bool isSimulator()
    {
        return true;
    }

#else

    bool isSimulator()
    {
        return false;
    }

#endif

    bool isMusicPlaying()
    {
        BOOL isPlaying = NO;
        AVAudioSession *session = [AVAudioSession sharedInstance];
        if (session != nil) {
            isPlaying = session.secondaryAudioShouldBeSilencedHint;
        }
        return isPlaying;
    }

    bool requestAppStoreReview()
    {
#if TARGET_OS_IOS
        if ([SKStoreReviewController class]) {
            [SKStoreReviewController requestReview];
            return true;
        }
        return false;
#else
        // tvOS devices don't support app store reviews
        return false;
#endif
    }

    bool supportsAppStoreReview()
    {
#if TARGET_OS_IOS
        if ([SKStoreReviewController class]) {
            return true;
        }
        return false;
#else
        // tvOS devices don't support app store reviews
        return false;
#endif
    }

    bool supportsURL(const char *url)
    {
        NSString *urlString = [NSString stringWithUTF8String:url];
        if ([urlString length] == 0) return false;
        
        NSURL *nsurl = [NSURL URLWithString:urlString];
        
        return ([[UIApplication sharedApplication] canOpenURL:nsurl]);
    }

    char* getMobileCountryCode()
    {
#if TARGET_OS_IOS
        CTTelephonyNetworkInfo *netInfo = [[CTTelephonyNetworkInfo alloc] init];
        if (netInfo == nil) {
            return nil;
        }

        CTCarrier *carrier = [netInfo subscriberCellularProvider];
        if (carrier == nil) {
            return nil;
        }

        NSString *mcc = [carrier mobileCountryCode];
        if (mcc != nil) {
            return cStringCopy([mcc UTF8String]);
        } else {
            return nil;
        }
#else
        // tvOS devices never have mobile network connections
        return nil;
#endif
    }

    char* getDeviceLocaleCode()
    {
        if (@available(iOS 10.0, tvOS 10.0, *)) {
            NSLocale *locale = [NSLocale currentLocale];
            if (locale == nil) {
                return nil;
            }

            NSString *cc = [locale countryCode];
            if (cc != nil) {
                return cStringCopy([cc UTF8String]);
            }
        }

        return nil;
    }

    CGRect CustomComputeSafeArea(UIView* view)
    {
        CGSize screenSize = view.bounds.size;
        CGRect screenRect = CGRectMake(0, 0, screenSize.width, screenSize.height);
        
        UIEdgeInsets insets = UIEdgeInsetsMake(0, 0, 0, 0);
        if (@available(iOS 11, *)) {
            insets = [UIApplication sharedApplication].delegate.window.safeAreaInsets;
        }
        
        screenRect.origin.x += insets.left;
        screenRect.origin.y += insets.bottom; // Unity uses bottom left as the origin
        screenRect.size.width -= insets.left + insets.right;
        screenRect.size.height -= insets.top + insets.bottom;
        
        float scale = view.contentScaleFactor;
        screenRect.origin.x *= scale;
        screenRect.origin.y *= scale;
        screenRect.size.width *= scale;
        screenRect.size.height *= scale;
        return screenRect;
    }
    
    void GetScreenSafeInsets(float* left, float* right, float* top, float* bottom)
    {
        if (@available(iOS 11, *)) {
            UIView* view = GetAppController().unityView;
            UIEdgeInsets insets = [UIApplication sharedApplication].delegate.window.safeAreaInsets;
            
            *left = insets.left * view.contentScaleFactor;
            *right = insets.right * view.contentScaleFactor;
            *top = insets.top * view.contentScaleFactor;
            *bottom = insets.bottom * view.contentScaleFactor;
        }
    }

    void GetSafeAreaImpl(float* x, float* y, float* w, float* h)
    {
        UIView* view = GetAppController().unityView;
        CGRect area = CustomComputeSafeArea(view);
        *x = area.origin.x;
        *y = area.origin.y;
        *w = area.size.width;
        *h = area.size.height;
    }

    int64_t GetFreeStorageSpace()
    {
        NSError *error = nil;
        NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
        NSDictionary *dictionary = [[NSFileManager defaultManager] attributesOfFileSystemForPath:[paths lastObject] error: &error];
        
        if (dictionary) {
            NSNumber *freeFileSystemSizeInBytes = [dictionary objectForKey:NSFileSystemFreeSize];
            return [freeFileSystemSizeInBytes longLongValue];
        } else {
            NSLog(@"[MiOSPluginUtilities] Error Obtaining System Memory Info: Domain = %@, Code = %ld", [error domain], (long)[error code]);
            return -1ll;
        }
    }

    int64_t GetFreeStorageSpaceMB()
    {
        int64_t freeSpace = GetFreeStorageSpace();
        if (freeSpace < 0ll) {
            return -1ll;
        } else {
            return freeSpace / 1048576ll;
        }
    }
}
