/*
 * Copyright (c) 2019-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
struct X {
  int f;
};

void skip(struct X& x) {}
void skip_ptr(struct X* x) {}

int wraps_read_inner(struct X& x) { return x.f; }

int wraps_read(struct X& x) { return wraps_read_inner(x); }

void wraps_write_inner(struct X& x, int i) { x.f = i; }

void wraps_write(struct X& x, int i) { wraps_write_inner(x, i); }

void wraps_delete_inner(struct X* x) { delete x; }

void wraps_delete(struct X* x) { wraps_delete_inner(x); }

void FP_delete_then_skip_ok(struct X& x) {
  delete (&x);
  skip(x);
}

void FP_delete_then_skip_ptr_ok(struct X* x) {
  delete x;
  skip_ptr(x);
}

void delete_then_read_bad(struct X& x) {
  delete (&x);
  wraps_read(x);
}

void FN_delete_then_write_bad(struct X& x) {
  wraps_delete(&x);
  wraps_read(x);
}

void FN_delete_inner_then_write_bad(struct X& x) {
  wraps_delete_inner(&x);
  wraps_read(x);
}

void read_write_then_delete_good(struct X& x) {
  wraps_write(x, 10);
  wraps_read(x);
  wraps_delete(&x);
}

int two_cells(struct X* x, struct X* y) {
  x->f = 32;
  y->f = 52;
  return x->f * y->f;
}

void aliasing_call(struct X* x) { two_cells(x, x); }

struct Y {
  int* p;
};

void store(struct Y* y, int* p) { y->p = p; }

void call_store(struct Y* y) {
  int x = 42;
  store(y, &x);
}

extern bool nondet_choice();

struct Y* FP_may_return_invalid_ptr_ok() {
  struct Y* y = new Y();
  if (nondet_choice()) {
    delete y;
  }
  return y;
}

void FN_feed_invalid_into_access_bad() {
  struct Y* y = FP_may_return_invalid_ptr_ok();
  call_store(y);
}
