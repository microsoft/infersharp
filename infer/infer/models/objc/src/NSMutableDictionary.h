/*
 * Copyright (c) 2015-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import <Foundation/NSObject.h>

@interface NSMutableDictionary : NSObject

- (void)removeObjectForKey:(id)aKey;
+ (NSMutableDictionary*)dictionaryWithSharedKeySet:(id)keyset;

@end
