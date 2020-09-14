/*
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

package codetoanalyze.java.quandary;

import android.content.ClipboardManager;
import android.text.Html;
import android.text.Spanned;
import android.widget.EditText;
import com.facebook.infer.builtins.InferTaint;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

public class UserControlledStrings {
  ClipboardManager clipboard;

  void readClipboardSourcesBad() {
    InferTaint.inferSensitiveSink(clipboard.getText());
    InferTaint.inferSensitiveSink(clipboard.getPrimaryClip());
    InferTaint.inferSensitiveSink(clipboard.getPrimaryClip().getItemAt(5));
    InferTaint.inferSensitiveSink(clipboard.getPrimaryClip().getItemAt(5).getText());
    InferTaint.inferSensitiveSink(clipboard.getPrimaryClip().toString());
    // 5 reports
  }

  Spanned clipboardToHtmlBad() {
    return Html.fromHtml(clipboard.getText().toString());
  }

  EditText mEditText;

  Spanned editTextToHtmlBad() {
    return Html.fromHtml(mEditText.getText().toString());
  }

  void clipboardToShellDirectBad() throws IOException {
    Runtime.getRuntime().exec(clipboard.getText().toString());
  }

  void clipboardToShellArrayBad() throws IOException {
    String[] cmds = new String[] {"ls", clipboard.getText().toString()};
    Runtime.getRuntime().exec(cmds);
  }

  ProcessBuilder clipboardToProcessBuilder1Bad() {
    return new ProcessBuilder(clipboard.getText().toString());
  }

  ProcessBuilder clipboardToProcessBuilder2Bad() {
    return new ProcessBuilder("sh", clipboard.getText().toString());
  }

  ProcessBuilder clipboardToProcessBuilder3Bad(ProcessBuilder builder) {
    return builder.command(clipboard.getText().toString());
  }

  ProcessBuilder clipboardToProcessBuilder4Bad(ProcessBuilder builder) {
    List<String> cmds = new ArrayList();
    cmds.add(clipboard.getText().toString());
    return builder.command(cmds);
  }
}
