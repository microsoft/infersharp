// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Extensions
{
    internal static class StackExtensions
    {
        /// <summary>
        /// Returns a shallow copy of the stack.
        /// </summary>
        /// <typeparam name="T">The type of element stored on the stack.</typeparam>
        /// <param name="stack">The stack to be cloned.</param>
        /// <returns>The shallow clone.</returns>
        public static Stack<T> Clone<T>(this Stack<T> stack)
        {
            var newStack = new Stack<T>();
            foreach (var item in stack.Reverse())
            {
                newStack.Push(item);
            }
            return newStack;
        }

        /// <summary>
        /// Determines whether the input stack begins with the elements comprising this instance.
        /// </summary>
        /// <typeparam name="T">The type of the stack elements.</typeparam>
        /// <param name="stack">Refers to this instance.</param>
        /// <param name="other">The other stack, to determine if this is instance is a subset 
        /// of.</param>
        /// <returns>
        ///   <c>true</c> if this instance is a subset of the other; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSubStackOf<T>(this Stack<T> stack, Stack<T> other)
        {
            var stackEnumerator = stack.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (stackEnumerator.MoveNext())
            {
                if (!otherEnumerator.MoveNext()) { return false; }

                if (!stackEnumerator.Current.Equals(otherEnumerator.Current))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
