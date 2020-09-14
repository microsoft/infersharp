/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
import java.util.ArrayList;

class Test {

  private int a = 0;
  static Integer[] global_arr;

  void Test(int size) {
    global_arr = new Integer[size];
  }

  void set_bad(int x, int y) {
    a = x + y;
  }

  void global_array_set_bad(int x, int y) {
    global_arr[0] = x + y;
  }

  int local_write_ok(int x, int y) {
    int k = x + y;
    k++;
    return k;
  }

  void call_pure_ok(int size) {
    for (int i = 0; i < size; i++) {
      local_write_ok(i, size);
    }
  }

  void call_impure_bad(int size) {
    int d = 0;
    for (int i = 0; i < size; i++) {
      set_bad(i, size);
    }
  }

  // no change to outside state, the local allocation is ok.
  int local_alloc_ok(int x, int y) {
    ArrayList<Integer> list = new ArrayList<Integer>(x + y);
    for (Integer el : list) {
      call_pure_ok(el);
    }
    return list.size();
  }

  void parameter_field_write_bad(Test test, boolean b) {
    int c = b ? 0 : 1;
    test.a = c;
  }

  int parameter_field_access_ok(Test test) {
    return test.a;
  }

  // expected to be impure since y points to x
  void local_field_write_bad(Test x) {
    Test y = x;
    y.a = 0;
  }

  void swap_bad(int[] array, int i, int j) {
    int tmp = array[i];
    array[i] = array[j];
    array[j] = tmp;
  }

  void alias_bad(int[] array, int i, int j) {
    int[] a = array;
    a[j] = i;
  }

  // Currently, we can't distinguish between returning new Objects or
  // creating new Objects locally. Ideally, the latter should be fine
  // as long as it doesn't leak to the result.
  public ArrayList<Integer> emptyList_bad_FP() {
    return new ArrayList<Integer>();
  }

  // All unmodeled calls will be marked as modifying global state
  static long systemNanoTime_bad() {
    return System.nanoTime();
  }
}
