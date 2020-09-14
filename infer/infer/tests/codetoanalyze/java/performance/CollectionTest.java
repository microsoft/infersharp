/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import java.util.Collection;
import java.util.concurrent.ConcurrentLinkedQueue;

public class CollectionTest {

  interface MyCollection<E> extends Collection<E> {}

  void iterate_over_mycollection(MyCollection<Integer> list) {
    for (int i = 0, size = list.size(); i < size; ++i) {}
  }

  void iterate_over_some_java_collection(
      ConcurrentLinkedQueue<MyCollection<Integer>> mSubscribers) {
    for (MyCollection<Integer> list : mSubscribers) {}
  }

  // Expected |mSubscribers| * |list| but we get |mSubscribers|
  // because we are not tracking elements of collections
  void iterate_over_mycollection_quad_FN(
      ConcurrentLinkedQueue<MyCollection<Integer>> mSubscribers) {
    for (MyCollection<Integer> list : mSubscribers) {
      iterate_over_mycollection(list);
    }
  }

  // expected: same as iterate_over_mycollection(list)
  void ensure_call(MyCollection<Integer> list) {
    iterate_over_mycollection(list);
  }

  // expected: O (|size| . |list|)
  void loop_over_call(int size, MyCollection<Integer> list) {
    for (int i = 0; i < size; i++) {
      iterate_over_mycollection(list);
    }
  }

  // expected: O (|list|^2)
  void iterate_over_call_quad(int size, MyCollection<Integer> list) {
    for (Integer i : list) {
      iterate_over_mycollection(list);
    }
  }

  // expected O (|list|^3)
  void nested_iterator_qubic(int size, MyCollection<Integer> list1, MyCollection<Integer> list2) {
    for (Integer i : list1) {
      for (Integer j : list2) {
        iterate_over_mycollection(list1);
        iterate_over_mycollection(list1);
      }
    }
  }
}
