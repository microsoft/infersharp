/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import "Car.h"

int main() {
  Car* honda = [[Car alloc] init];
  honda.running = YES;
  NSLog(@"%d", honda.running);
  return 0;
}
