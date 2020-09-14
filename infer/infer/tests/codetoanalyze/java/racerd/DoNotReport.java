/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package com.racerd.donotreport;

import com.facebook.infer.annotation.ThreadSafe;

@ThreadSafe
class DoNotReport {

  int mFld;

  // normally we would report this, but we won't because com.racerd.donotreport is blacklisted in
  // .inferconfig
  void obviousRaceBad(int i) {
    mFld = i;
  }
}
