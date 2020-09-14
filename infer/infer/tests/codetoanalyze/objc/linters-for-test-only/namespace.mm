/*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace FirstNameSpace {
namespace SecodNameSpace {
class C {
  void m();
};
}
void SecodNameSpace::C::m() {}
}
using namespace FirstNameSpace;
void foo() { SecodNameSpace::C(); }
