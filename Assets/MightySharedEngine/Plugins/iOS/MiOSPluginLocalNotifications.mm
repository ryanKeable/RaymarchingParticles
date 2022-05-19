#import <Foundation/Foundation.h>

extern "C" {

    void resetNotifications()
    {
        [[UIApplication sharedApplication] cancelAllLocalNotifications];
        [UIApplication sharedApplication].applicationIconBadgeNumber = 0;
    }

}
