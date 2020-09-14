/*
 * Copyright (c) 2013-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package com.google.common.base;

import javax.annotation.Nullable;

public abstract class Optional<T> {

  @Nullable
  public abstract T orNull();
}
