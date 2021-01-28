// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdtokenParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldtoken:
                    var token = instruction.Operand;
                    string runtimeHandleTypeString;

                    if (token is TypeReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeTypeHandle";
                    }
                    else if (token is FieldReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeFieldHandle";
                    }
                    else if (token is MethodReference)
                    {
                        runtimeHandleTypeString = "System.RuntimeMethodHandle";
                    }
                    else
                    {
                        Log.WriteError($"Unexpected instruction operand {instruction.Operand}");
                        return false;
                    }
                    var runtimeHandleType = new Tptr(Tptr.PtrKind.Pk_pointer,
                                                     new Tstruct(runtimeHandleTypeString));

                    // In keeping with how we handle structs when translating Initobj, we
                    // initialize an object in the SIL to represent the value type.
                    (var node, var runtimeHandleVariable) = CreateObjectAllocationNode(
                        runtimeHandleType, state);

                    RegisterNode(state, node);

                    state.PushExpr(runtimeHandleVariable, runtimeHandleType);
                    state.PushInstruction(instruction.Next, node);
                    state.AppendToPreviousNode = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
