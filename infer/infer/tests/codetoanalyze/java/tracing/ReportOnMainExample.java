/*
 * Copyright (c) 2015-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package codetoanalyze.java.tracing;

public class ReportOnMainExample {

  T2 t;

  native boolean test();

  void foo() {
    if (test() && t == null) {
      return;
    }
    t.f();
  }

  public static void main(String[] args) {
    ReportOnMainExample example = new ReportOnMainExample();
    example.foo();
  }
}
