/*
 * Copyright (c) 2016-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

extern void some_undefined_function();

struct SomeNonPODObject {
  virtual void some_method();
  SomeNonPODObject() {
    // make constructor non-constexpr
    some_undefined_function();
  }
};

class SomeConstexprObject {
 public:
  int foo;
  virtual SomeConstexprObject& someMethod();
  static SomeConstexprObject instance_;
  static SomeConstexprObject& singletonMethod() {
    return instance_.someMethod();
  }

 private:
  constexpr SomeConstexprObject() : foo(42) {}
};

template <typename T>
struct SomeTemplatedNonPODObject {
  virtual T some_method();
  SomeTemplatedNonPODObject() {
    // make constructor non-constexpr
    some_undefined_function();
  }
};

template <typename T>
class SomeTemplatedConstexprObject {
 public:
  int foo;
  virtual SomeTemplatedConstexprObject<T>& someMethod();
  static SomeTemplatedConstexprObject<T> instance_;
  static SomeTemplatedConstexprObject<T>& singletonMethod() {
    return instance_.someMethod();
  }

 private:
  constexpr SomeTemplatedConstexprObject() : foo(42) {}
};

int access_to_templated_non_pod();
int access_to_non_pod();
SomeNonPODObject& getFunctionStaticNonPOD();
SomeNonPODObject& getGlobalNonPOD();
SomeNonPODObject& getGlobalNonPODWhitelisted();

namespace whitelisted {
SomeNonPODObject& getGlobalNonPOD();

template <typename T>
struct TemplatedObject {
  static SomeNonPODObject& getGlobalNonPOD();
};
} // namespace whitelisted
