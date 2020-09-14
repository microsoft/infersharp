/*
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import "MyProtocol.h"
#import <Foundation/NSObject.h>

@interface Test : NSObject<MyProtocol> {

  int numberOfFiles;
}

@end
