/*
 * Copyright (c) 2016-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#import "NSMapTable.h"

@implementation NSMapTable

- (id)objectForKey:(id)aKey {
  if (aKey == NULL)
    return NULL;
  return [NSObject alloc];
}

@end
