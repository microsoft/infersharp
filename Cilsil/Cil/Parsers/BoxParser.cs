// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class BoxParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Box:
                    (var value, var type) = state.Pop();

                    var boxedObjectType = new BoxedValueType(Tptr.PtrKind.Pk_pointer,
                                                             new Tstruct("System.Object"),
                                                             value,
                                                             type);

                    // The value in question is boxed into a generic object, which we allocate.
                    (var node, var boxedObjectVariable) = CreateObjectAllocationNode(
                        boxedObjectType, state);

                    RegisterNode(state, node);

                    state.PushExpr(boxedObjectVariable, boxedObjectType);
                    state.PushInstruction(instruction.Next, node);
                    state.AppendToPreviousNode = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
