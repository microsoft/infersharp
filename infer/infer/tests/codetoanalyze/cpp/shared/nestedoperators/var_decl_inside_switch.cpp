/*
 * Copyright (c) 2015-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

int get(int a) {
  switch (int x = a) {
    case 0:
    case 1:
      return 0;
    case 2:
      return 1;
    default:
      return x;
  }
}
