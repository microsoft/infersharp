/*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package com.facebook.infer.annotation;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/** Annotation to indicate that when the parameter is null, the result is also null. */
@Retention(RetentionPolicy.CLASS)
@Target({ElementType.PARAMETER})
public @interface PropagatesNullable {}
