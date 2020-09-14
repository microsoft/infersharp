/*
 * Copyright (c) 2015-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package codetoanalyze.java.performance;

public class Cost_test_deps {

  // Loop's execution count doesn't depend on values of p,t,k
  private static int loop_no_dep1(int k) {
    int p = 0;
    int t = 2 + k;
    for (int i = 0; i < 100; i++) {
      p++;
    }
    return p;
  }

  private static int foo(int i, int j) {
    return i + j;
  }

  // Loop's execution count doesn't depend on values of p,t,k
  private static int loop_no_dep2(int k) {
    int p = 0;
    int t = foo(p, k);
    for (int i = 0; i < 100; i++) {
      p++;
    }
    return p;
  }

  private static void if_bad(int j) {
    int p = 10;
    if (p < 10 + j) {
      p++;
    } else {
      p = j + 3;
      for (int k = 0; k < 100; k++) {
        j += 3;
      }
    }
  }

  private static int if_bad_loop() {
    int p = 10;
    for (int j = 0; j < 5; j++) {
      if (j < 2) {
        p++;
      } else {
        p = 3;
        for (int k = 0; k < 10; k++) {
          int m = 0;
        }
      }
    }
    return p;
  }

  private static int two_loops() {
    int p = 10;
    int k = 3;
    int t = 2 + k;
    for (int j = 0; j < 6; j++) {
      k++;
    }
    for (int i = 0; i < 100; i++) {
      p = 3;
    }
    return p;
  }

  private static int loop_despite_inferbo(int p) {

    int k = 100;
    for (int i = 0; i < k; i++) {
      int m = p + 3;
      if (m < 14) {
        p += 9;
      }
    }
    return p;
  }

  /* Expected: 5 * 100 */
  private static int nested_loop() {
    int k = 0;
    for (int i = 0; i < 5; i++) {
      A:
      k = 0;
      for (int j = 0; j < 100; j++) {
        k = 3;
      }
    }
    return k;
  }

  private static int real_while() {
    int i = 0;
    int j = 3 * i;
    while (i < 30) {
      j = j + i;
      i++;
    }
    return j;
  }
}
