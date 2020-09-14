// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StelemParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // This instruction carries an operand describing the type of the value to be added
                // to the array.
                case Code.Stelem_Any:
                case Code.Stelem_I:
                case Code.Stelem_I1:
                case Code.Stelem_I2:
                case Code.Stelem_I4:
                case Code.Stelem_I8:
                case Code.Stelem_R4:
                case Code.Stelem_R8:
                case Code.Stelem_Ref:
                    // The corresponding code would look like: array[arrayIndex] = valueToStore
                    (var valueToStore, var type) = state.Pop();
                    (var arrayIndex, _) = state.Pop();
                    (var array, _) = state.Pop();
                    var store = new Store(lvalue: new LindexExpression(array, arrayIndex),
                                          rvalue: valueToStore,
                                          type: type,
                                          location: state.CurrentLocation);
                    state.PreviousNode.Instructions.Add(store);
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
