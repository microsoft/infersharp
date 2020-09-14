/*
 * Copyright (c) 2015-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package hello;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.util.Random;

class Hello {

  void doesNotCauseNPE() {
    Pointers.A a = Pointers.mayReturnNull(10);
    a.method();
  }

  void mayCauseNPE() {
    Random rng = new Random();
    Pointers.A a = Pointers.mayReturnNull(rng.nextInt());
    // FIXME: should check for null before calling method()
    a.method();
  }

  void mayLeakResource() throws IOException {
    OutputStream stream = Resources.allocateResource();
    if (stream == null) {
      return;
    }

    try {
      stream.write(12);
    } finally {
      // FIXME: should close the stream
    }
  }

  /**
   * This method should be rewritten with nested try { ... } finally { ... } statements, or the
   * possible exception raised by fis.close() should be swallowed.
   */
  void twoResources() throws IOException {
    FileInputStream fis = null;
    FileOutputStream fos = null;
    try {
      fis = new FileInputStream(new File("whatever.txt"));
      fos = new FileOutputStream(new File("everwhat.txt"));
      fos.write(fis.read());
    } finally {
      if (fis != null) {
        fis.close();
      }
      if (fos != null) {
        fos.close();
      }
    }
  }
}
