/*
 * Copyright (c) 2019-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
public class A {}

class B {

  void error() {
    A a1 = new A();
    A a2 = new A();
    A a3 = new A();
    A a4 = new A();
  }

  void ok() {
    A a1 = new A();
  }

  class BArray {

    void error() {
      A[] ar1 = new A[5];
      A[] ar2 = new A[6];
      A[] ar3 = new A[7];
      A[] ar4 = new A[5];
      A[] ar5 = new A[4];
    }

    void ok() {
      A[] ar1 = new A[5];
      A[] ar2 = new A[5];
    }
  }
}
