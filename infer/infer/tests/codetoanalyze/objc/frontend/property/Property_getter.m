/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import <Foundation/NSObject.h>

@interface A : NSObject
@property int x;
@end

@implementation A

- (int)addTarget:(A*)target {
  return target.x;
}

@end
