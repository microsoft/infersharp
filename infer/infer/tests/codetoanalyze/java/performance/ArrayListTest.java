/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

import java.util.ArrayList;
import java.util.Iterator;

public class ArrayListTest {

  public void iterate_over_arraylist(ArrayList<Integer> list) {
    for (int i = 0, size = list.size(); i < size; ++i) {}
  }

  public void iterate_over_local_arraylist(ArrayList<Integer> list) {
    ArrayList<Integer> local_list = list;
    for (int i = 0, size = local_list.size(); i < size; ++i) {}
  }

  public void arraylist_empty_underrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(-1, 42);
  }

  public void arraylist_empty_ok() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0, 42);
  }

  public void arraylist_empty_overrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(1, 42);
  }

  public void arraylist_add3_overrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(42);
    list.add(1337);
    list.add(1984);
    list.add(4, 666);
  }

  // we can't set the size of the list to 10 because it depends on how
  // many times the loop is executed.Should be fixed once we have
  // relational domain working.
  public void arraylist_add_in_loop_FP() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    for (int i = 0; i < 10; ++i) {
      list.add(i);
    }
    for (int i = 0, size = list.size(); i < size; ++i) {}
  }

  public void arraylist_add_in_loop_ok() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.add(1);
    list.add(2);
    list.add(3);
    list.add(4);
    list.add(5);
    list.add(6);
    list.add(7);
    list.add(8);
    list.add(9);
    list.add(10);
    list.add(11);
    list.add(12);
    list.add(13);
    list.add(14);
    list.add(15);

    for (int i = 0, size = list.size(); i < size; ++i) {}
  }

  public void arraylist_addAll_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(2);
    list.add(3);

    ArrayList<Integer> list2 = new ArrayList<Integer>();
    list2.add(0);
    list2.add(1);

    list2.addAll(0, list);
    list2.addAll(5, list);
  }

  public void arraylist_get_underrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.get(0);
  }

  public void arraylist_get_overrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.get(2);
  }

  public void arraylist_get_ok() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.add(1);
    list.add(2);

    for (int i = 0, size = list.size(); i < size; ++i) {
      list.get(i);
    }
  }

  public void arraylist_set_ok() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.add(1);
    list.add(2);
    for (int i = 0, size = list.size(); i < size; ++i) {
      list.set(i, i);
    }
  }

  public void arraylist_set_underrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.set(0, 10);
  }

  public void arraylist_set_overrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.set(1, 10);
  }

  public void arraylist_remove_overrun_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.remove(1);
  }

  public void arraylist_remove_ok() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.add(1);
    list.remove(0);
    list.get(0);
  }

  public void arraylist_remove_bad() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    list.add(0);
    list.add(1);
    list.remove(0);
    list.get(1);
  }

  // we can't set the size of the list to 10 because it depends on how
  // many times the loop is executed. Should be fixed once we have
  // relational domain working.
  public void arraylist_remove_in_loop_Good_FP() {
    ArrayList<Integer> list = new ArrayList<Integer>();
    for (int i = 0; i < 10; ++i) {
      list.add(i);
    }
    for (int i = 0, size = list.size(); i < size; ++i) {
      list.remove(i);
    }
  }

  public void iterate_with_iterator(ArrayList<Integer> list) {
    for (Integer element : list) {}
  }

  public void iterate_while_has_next(ArrayList<Integer> list) {
    Iterator itr = list.iterator();

    while (itr.hasNext()) {
      System.out.println(itr.next());
    }
  }

  public void iterate_over_arraylist_with_inner(ArrayList<Integer> list1) {
    ArrayList<Integer> list2 = new ArrayList<Integer>();

    for (Integer element : list1) {
      list2.size();
    }
  }

  // Control vars include element which is some intValue and list
  // length. Hence, we get quadratic bound.
  // Simplified version of real code https://fburl.com/a3gge1b7
  public boolean iterate_over_arraylist_shortcut_FP(ArrayList<Integer> list) {
    for (Integer element : list) {
      if (element > 10) {
        return false;
      }
    }
    return true;
  }

  public static void sortArrayList(ArrayList<Integer> list) {
    java.util.Collections.sort(list);
  }

  private static void call_sortArrayList(ArrayList<Integer> list) {
    sortArrayList(list);
  }

  private ArrayList<String> list = new ArrayList<>();

  // Since we know the maximum value of boolean [iterator.next().equals(s)] is 1, it is better to be
  // excluded from the cost.
  // Simplified version of real code https://fburl.com/traceview/tpc0grh2
  public boolean remove_string_from_list(String s) {
    Iterator<String> iterator = list.iterator();
    while (iterator.hasNext()) {
      if (iterator.next().equals(s)) {
        iterator.remove();
        return true;
      }
    }
    return false;
  }

  void constructor_linear(ArrayList<String> list) {
    ArrayList<String> slist = new ArrayList<>(list);
    for (int i = 0; i < slist.size(); i++) {}
  }

  void constructor_modify(ArrayList<String> list) {
    // copying the reference here, so any change made to slist will
    // affect list
    ArrayList<String> slist = new ArrayList<>(list);
    slist.add("a");
    slist.add("b");
    slist.add("c");
    slist.add("d");
    for (int i = 0; i < list.size(); i++) {}
  }

  void constructor_add_all(ArrayList<String> list, ArrayList<String> l) {
    ArrayList<String> slist = new ArrayList<>(list);
    slist.addAll(l); // increments the size of both list and slist by
    // l.length
    for (int i = 0; i < list.size(); i++) {}
  }

  void constructor_add_all_sym(ArrayList<String> list, ArrayList<String> l) {
    ArrayList<String> slist = new ArrayList<>(list);
    list.addAll(l); // increments the size of both list and slist by
    // l.length
    for (int i = 0; i < slist.size(); i++) {}
  }
}
