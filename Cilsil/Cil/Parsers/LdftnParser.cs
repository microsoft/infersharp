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
            Mono.Cecil.MethodReference method;
            switch (instruction.OpCode.Code)
            {
                case Code.Ldvirtftn:
                    // The object reference corresponding to the method is popped from the stack.
                    // We discard it because it seems like in almost all scenarios it won't be
                    // relevant for Infer's analysis (in the CIL, it is used to look up the
                    // address of the entry point to the method); often, the function pointer will
                    // just get passed as an intptr to a newobj command after, which is meaningless
                    // to Infer anyway.
                    _ = state.Pop();
                    method = instruction.Operand as Mono.Cecil.MethodReference;
                    state.PushExpr(
                        new ConstExpression(new ProcedureName(method)),
                                            new Tptr(Tptr.PtrKind.Pk_pointer, new Tfun(method)));
                    state.PushInstruction(instruction.Next);
                    return true;
                case Code.Ldftn:
                    // We handle these similarly to how they are handled in the clang frontend. We
                    // push the function pointer onto the stack. It can be later stored into a
                    // register if necessary, which would then function similarly to a clang
                    // function pointer.
                    method = instruction.Operand as Mono.Cecil.MethodReference;
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
