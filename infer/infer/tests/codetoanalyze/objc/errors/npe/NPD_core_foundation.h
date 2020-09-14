/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import <Foundation/NSObject.h>
#import <UIKit/UIKit.h>

@interface NullDeref : NSObject

@property(strong) UIView* backgroundCoveringView;
@property(strong) UIView* attachmentContainerView;

@end
