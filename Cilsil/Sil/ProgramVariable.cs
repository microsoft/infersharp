// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Mono.Cecil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cilsil.Sil
{
    /// <summary>
    /// Type for program variables. There are 4 kinds of variables:
    ///    1) Local variables, used for local variables and formal parameters
    ///    2) Callee program variables, used to handle recursion
    ///    ([x | callee] is distinguished from [x]) [TODO]
    ///    3) Global variables
    ///    4) Seed variables, used to store the initial value of formal parameters [TODO]
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ProgramVariable
    {
        /// <summary>
        /// The name of the program variable.
        /// </summary>
        [JsonProperty]
        public string PvName { get; protected set; }

        /// <summary>
        /// The <see cref="System.Type"/> of the program variable.
        /// </summary>
        [JsonProperty]
        public string PvKind => GetType().Name;

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this
        /// instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this
        /// instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) =>
            obj is ProgramVariable variable &&
            PvName == variable.PvName &&
            PvKind == variable.PvKind;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(PvName, PvKind);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ProgramVariable left, ProgramVariable right) =>
            EqualityComparer<ProgramVariable>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ProgramVariable left, ProgramVariable right) =>
            !(left == right);
    }

    /// <summary>
    /// Local variable belonging to a function.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class LocalVariable : ProgramVariable
    {
        /// <summary>
        /// The name of the procedure in which the local variable is instantiated.
        /// </summary>
        [JsonProperty]
        public ProcedureName ProcName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalVariable"/> class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="proc">The procedure in which the local variable is instantiated.</param>
        public LocalVariable(string name, MethodDefinition proc)
        {
            PvName = name;
            ProcName = new ProcedureName(proc);
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => PvName;

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this
        /// instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this
        /// instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) =>
            obj is LocalVariable variable &&
            base.Equals(obj) &&
            EqualityComparer<ProcedureName>.Default.Equals(ProcName, variable.ProcName);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ProcName);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(LocalVariable left, LocalVariable right) =>
            EqualityComparer<LocalVariable>.Default.Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(LocalVariable left, LocalVariable right) =>
            !(left == right);
    }

    /// <summary>
    /// Global variable. Can be used to represent classes, for example when storing into and 
    /// loading from a static field.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GlobalVariable : ProgramVariable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalVariable"/> class.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        public GlobalVariable(string name)
        {
            PvName = name;
        }
    }
}
