/*
 * Copyright (c) 2018-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

/* while loop that contains && in the guard. It gives the correct bound.
 * Expected: Theta(m)  */
int compound_while(int m) {
  int i = 0;
  int j = 3 * i;
  while (j == 0 && i < m) {
    i++;
  }
  return j;
}

/* this should give Theta(100) once we have extract_post in the range */
int simplified_simulated_while_with_and(int p) {
  int k = 0;
  int j = 0;
B:
  j++;
  if (k == 0 && j < 100) {
    goto B; // continue;
  }
  return k;
}

/* simulated goto that contains && */
int simulated_while_with_and(int p) {
  int i = 0;
  int k = 0;
LOOP_COND:
  if (k == 0 && i < p) { // k == 0 always true
    goto INCR;
  } else {
    goto RETURN;
  }
INCR:
  i++;
  goto LOOP_COND;
RETURN:
  return i;
}

/* shortcut in the conditional, hence we won't loop, and get constant cost */
int simulated_while_shortcut(int p) {
  int k = 0;
  int j = 0;
B:
  j++;
  if (k == 1 && j < 100) {
    goto B; // continue;
  }
  return k;
}

/* p should be in control vars */
void while_and_or(int p) {
  int i = 0;
  while (p == 1 || (i < 30 && i >= 0)) {
    i++;
  }
}

// should be constant cost
int nested_while_and_or(int p) {
  int i = 0;
  int j = 3 * i;
  while (p == 1 || (i < 30 && i >= 0)) {
    while (p == 1 || (j < 5 && j >= 0)) {

      return j;
    }
    i++;
  }
  return j;
}

/* j and i will be control variables for B  */
/* Expected: 5 + 100 */
int simulated_nested_loop_with_and(int p) {
  int k = 0;
  int t = 5;
  int j = 0;
  for (int i = 0; i < 5; i++) {
  B:
    t = 3;
    j++;
    if (k == 0 && j < 100) { // k == 0 always true
      goto B; // continue;
    }
  }
  return k;
}
