/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
class GlobalTest {
  public static int s = 0;
  public static Foo foo;

  class Foo {

    int x = 0;
    // modifies global var 's' hence impure
    void set_bad() {
      s = 10;
    }
  }

  void incr(Foo foo, int i) {
    foo.x += i;
  }

  // calls foo which modifies global var
  void call_set_bad() {
    Foo f = new Foo();
    f.set_bad();
  }

  // foo is global which is modified by incr.
  void global_mod_via_argument_passing_bad(int size, Foo f) {
    for (int i = 0; i < size; i++) {
      incr(foo, i);
    }
  }

  // aliased_foo is aliasing a global and then is modified by incr.
  void global_mod_via_argument_passing_bad_aliased(int size, Foo f) {
    Foo aliased_foo = foo; // Inferbo can't recognize aliasing here
    // and assumes aliased_foo is in [-oo,+oo] not in foo
    for (int i = 0; i < size; i++) {
      incr(aliased_foo, i);
    }
  }
}
