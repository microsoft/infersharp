/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import <Foundation/NSObject.h>

@interface EqualNamesA : NSObject {
 @public
  int x;
}

+ (EqualNamesA*)meth;

@property(nonatomic, readonly) EqualNamesA* meth;

@end

@implementation EqualNamesA

+ (EqualNamesA*)meth {
  return [EqualNamesA new];
}

- (EqualNamesA*)meth {
  return nil;
}

@end

int EqualNamesTest() {
  EqualNamesA* para = [EqualNamesA new];
  EqualNamesA* a = [para meth];
  return a->x;
}

int EqualNamesTest2(EqualNamesA* para) {
  EqualNamesA* a = [EqualNamesA meth];
  return a->x;
}
