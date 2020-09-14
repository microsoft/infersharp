/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonDeserializer;
import java.io.IOException;
import java.util.List;

class HoistModeled {

  int list_contains_hoist(List<String> list, String s) {
    int d = 0;
    for (int i = 0; i < 10; i++) {
      if (list.contains(s.substring(0, 1))) {
        d++;
      }
    }
    return d;
  }

  void deserialize_hoist(
      final JsonDeserializer<?> specDeserializer,
      final JsonParser p,
      final DeserializationContext ctx)
      throws IOException {
    int d = 0;
    Object o;
    for (int i = 0; i < 10; i++) {
      o = specDeserializer.deserialize(p, ctx);
    }
  }
}
