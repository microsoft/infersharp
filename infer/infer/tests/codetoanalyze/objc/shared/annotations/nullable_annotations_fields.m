/*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
#import <Foundation/NSObject.h>

@interface A : NSObject

@end

A* getA();

@implementation A {
  int x;
  A* _Nullable _child;
}
- (int)nullable_field {
  A* a = getA();
  A* child = a->_child;
  return child->x;
}

@end
