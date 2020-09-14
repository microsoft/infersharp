/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import "MySubClass.h"

@implementation MySubclass : MyClass {
}

- (int)myNumber {

  int subclassNumber = [super myNumber] + 1;
  return subclassNumber;
}

@end
