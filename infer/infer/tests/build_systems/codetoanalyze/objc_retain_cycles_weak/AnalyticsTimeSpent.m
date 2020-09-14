/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
#import "AnalyticsTimeSpent.h"
#import "TimeSpent.h"

@implementation AnalyticsTimeSpent

@synthesize delegate;

- (instancetype)initWithDelegate:(id)delegateObject {
  [self setDelegate:delegateObject];
  return self;
}

- (instancetype)initWithStrongDelegate:(id)delegateObject {
  _strong_delegate = delegateObject;
  return self;
}

@end
