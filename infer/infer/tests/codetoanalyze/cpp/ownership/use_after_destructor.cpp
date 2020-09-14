/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

#include <iostream>
#include <memory>
#include <string>

struct S {
  int* f;
  S(int i) {
    f = new int;
    *f = i;
  }

  // missing: operator= to copy the pointer. double delete can happen if
  // operator= is called

  ~S() { delete f; }
};

// destructor called at end of function, no issues
void normal_scope_destructor_ok() { S s(1); }

void nested_scope_destructor_ok() {
  { S s(1); }
}

int reinit_after_explicit_destructor_ok() {
  S s(1);
  s.~S();
  s = S(2);
  return *s.f;
}

void placement_new_explicit_destructor_ok() {
  char buf[sizeof(S)];
  {
    S* s = new (buf) S(1);
    s->~S();
  }
  {
    // this use of [buf] shouldn't be flagged
    S* s = new (buf) S(2);
    s->~S();
  }
}

void double_destructor_bad() {
  S s(1);
  s.~S();
  // destructor will be called again after S goes out of scope, which is
  // undefined behavior
}

int use_after_destructor_bad() {
  S s(1);
  s.~S();
  int ret = *s.f;
  s = S{2};
  return ret;
}

// can't get this yet because we assume operator= copies resources correctly
// (but this isn't true for S)
void FN_use_after_scope1_bad() {
  S s(1);
  {
    S tmp(2);
    s = tmp; // translated as operator=(s, tmp)
  } // destructor for tmp runs here
  // destructor for s here; second time the value held by s has been destructed
}

void FN_use_after_scope2_bad() {
  S s(1);
  {
    s = S(1);
  } // destructor runs here, but our frontend currently doesn't insert it
}

struct POD {
  int f;
};

// this code is ok since double-destructing POD struct is ok
void destruct_twice_ok() {
  POD p{1};
  {
    POD tmp{2};
    p = tmp;
  } // destructor for tmp
} // destructor for p runs here, but it's harmless

class Subclass : virtual POD {
  int* f;
  Subclass() { f = new int; }

  /** frontend code for this destructor will be:
   * ~Subclass:
   *  __infer_inner_destructor_~Subclass(this)
   *  __infer_inner_destructor_~POD(this)
   *
   * __infer_inner_destructor_~Subclass:
   *  delete f;
   *
   * We need to be careful not to warn that this has been double-destructed
   */
  ~Subclass() { delete f; }
};

void basic_placement_new_ok() {
  S* ptr = new S(1);
  S* tptr = new (ptr) S(1);
  tptr->~S();
  delete[] ptr;
}

S* destruct_pointer_contents_then_placement_new1_ok(S* s) {
  s->~S();
  new (s) S(1);
  return s;
}

// need better heap abstraction to catch this example and the next
S* FN_placement_new_aliasing1_bad() {
  S* s = new S(1);
  s->~S();
  auto alias = new (s) S(2);
  delete alias; // this deletes s too
  return s; // bad, returning freed memory
}

S* FN_placement_new_aliasing2_bad() {
  S* s = new S(1);
  s->~S();
  auto alias = new (s) S(2);
  delete s; // this deletes alias too
  return alias; // bad, returning freed memory
}

void placement_new_non_var_ok() {
  struct M {
    S* s;
  } m;
  m.s = new S(1);
  m.s->~S();
  new (m.s) S(2);
  delete m.s;
}

void return_placement_new_ok() {
  auto mem = new S(1);
  return new (mem) S(2);
}

void destructor_in_loop_ok() {
  for (int i = 0; i < 10; i++) {
    S s(1);
  }
}

int FN_use_after_scope3_bad() {
  int* p;
  {
    int value = 3;
    p = &value;
  } // we do not know in the plugin that value is out of scope
  return *p;
}

struct C {
  C(int v) : f(v){};
  ~C();
  int f;
};

int use_after_scope4_bad() {
  C* pc;
  {
    C c(3);
    pc = &c;
  }
  return pc->f;
}

struct B {
  ~B();
};

struct A {
  ~A() { (void)*f; }
  const B* f;
};

void destructor_order_bad() {
  A a;
  B b;
  a.f = &b;
}

struct A2 {
  ~A2() {}
  const B* f;
};

// need interprocedural analysis to fix this
void FP_destructor_order_empty_destructor_ok() {
  A2 a;
  B b;
  a.f = &b;
}
