// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class LdftnParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // TODO: should later add case Code.Ldvirtftn.
                case Code.Ldftn:
                    // We handle these similarly to how they are handled in the clang frontend. We
                    // push the function pointer onto the stack. It can be later stored into a
                    // register if necessary, which would then function similarly to a clang
                    // function pointer.
                    var method = instruction.Operand as Mono.Cecil.MethodReference;
                    state.PushExpr(
                        new ConstExpression(new ProcedureName(method)), 
                                            new Tptr(Tptr.PtrKind.Pk_pointer, new Tfun(method)));
                    state.PushInstruction(instruction.Next);
                    return true;
                default:
                    return false;
            }
        }
    }
}
