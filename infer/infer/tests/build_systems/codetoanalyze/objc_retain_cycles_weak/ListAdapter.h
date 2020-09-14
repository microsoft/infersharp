/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
#import <Foundation/NSObject.h>

@interface ListAdapter

@property(nonatomic, nullable, weak) id dataSource;
@property(nonatomic, nullable, strong) id dataSourceStrong;

@end
