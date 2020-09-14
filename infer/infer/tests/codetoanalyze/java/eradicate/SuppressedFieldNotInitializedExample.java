/*
 * Copyright (c) 2013-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package codetoanalyze.java.eradicate;

import android.annotation.SuppressLint;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.CLASS)
@Target({ElementType.TYPE, ElementType.FIELD, ElementType.METHOD})
@interface SuppressFieldNotInitialized {}

public class SuppressedFieldNotInitializedExample {

  @SuppressLint("eradicate-field-not-initialized")
  String iKnowBetter;

  @SuppressFieldNotInitialized String annotationSuppressed;

  SuppressedFieldNotInitializedExample() {}
}
