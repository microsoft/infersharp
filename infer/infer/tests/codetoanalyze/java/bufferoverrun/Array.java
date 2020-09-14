/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
package codetoanalyze.java.bufferoverrun;

import java.util.ArrayList;

class Array {
  private ArrayList a = new ArrayList<>();

  void collection_add_zero_Good() {
    a.add(0, 100);
  }

  ArrayList collection_remove_from_empty_Bad() {
    ArrayList b = new ArrayList<>();
    b.remove(0);
    return b;
  }

  void null_pruning1_Good() {
    if (a == null) {
      if (a != null) {
        int[] arr = {1, 2, 3, 4, 5};
        arr[10] = 1;
      }
    }
  }

  void null_pruning1_Bad() {
    if (a == null) {
      if (a == null) {
        int[] arr = {1, 2, 3, 4, 5};
        arr[10] = 1;
      }
    }
  }

  void null_pruning2_Good_FP() {
    if (a != null) {
      if (a == null) {
        int[] arr = {1, 2, 3, 4, 5};
        arr[10] = 1;
      }
    }
  }

  void null_pruning2_Bad() {
    if (a != null) {
      if (a != null) {
        int[] arr = {1, 2, 3, 4, 5};
        arr[10] = 1;
      }
    }
  }

  void negative_alloc_Bad() {
    a = new ArrayList<>(-1);
  }

  void zero_alloc_Good() {
    a = new ArrayList<>(0);
  }

  void positive_alloc_Good() {
    a = new ArrayList<>(10);
  }
}
