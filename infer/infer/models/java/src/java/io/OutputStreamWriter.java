/*
 * Copyright (c) 2013-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package java.io;

import com.facebook.infer.builtins.InferBuiltins;
import com.facebook.infer.builtins.InferUndefined;
import com.facebook.infer.builtins.InferUtils;

public class OutputStreamWriter extends Writer {

  public OutputStreamWriter(OutputStream out, String charsetName)
      throws UnsupportedEncodingException {
    if (charsetName == null) throw new NullPointerException("charsetName");
    else if (InferUtils.isValidCharset(charsetName)) {
      InferBuiltins.__set_file_attribute(this);
    } else throw new UnsupportedEncodingException();
  }

  public void flush() throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }

  public void write(char cbuf[]) throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }

  public void write(char cbuf[], int off, int len) throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }

  public void write(int c) throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }

  public void write(String str) throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }

  public void write(String str, int off, int len) throws IOException {
    InferUndefined.can_throw_ioexception_void();
  }
}
