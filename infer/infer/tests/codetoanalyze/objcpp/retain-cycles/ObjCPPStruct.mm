/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
#import <Foundation/NSObject.h>

@interface A : NSObject
@end

struct WeakHolder {
  NSObject* ref;
  __weak NSObject* weakRef;
};

@implementation A {
  WeakHolder* _weakHolder;
}

- (instancetype)aFnGood {
  _weakHolder = new WeakHolder();
  _weakHolder->weakRef = self;
  return self;
}

- (instancetype)aFnBad {
  _weakHolder = new WeakHolder();
  _weakHolder->ref = self;
  return self;
}

@end
