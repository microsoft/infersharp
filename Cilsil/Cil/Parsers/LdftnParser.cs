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
                // Treat it like new object creation, create type of System.Intptr
                // seems like it usually gets followed by a newobj command.
                // In the event that this gets followed by a "calli" command, the function this refers to gets invoked
                // TODO: should later add case Code.Ldvirtftn.
                case Code.Ldftn:
                    var method = instruction.Operand as Mono.Cecil.MethodReference;
                    var funcExp = new ConstExpression(new ProcedureName(method));
                    var funcPointer = new Tptr(Tptr.PtrKind.Pk_pointer, new Tfun());
                    state.PushExpr(funcExp, funcPointer);
                    state.PushInstruction(instruction.Next);
                    return false;
                default:
                    return false;
            }
        }
    }
}
