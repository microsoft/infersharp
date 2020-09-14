// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Cil.Parsers
{
    internal class CallParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            bool isVirtual;
            switch (instruction.OpCode.Code)
            {
                case Code.Call:
                    isVirtual = false;
                    break;
                case Code.Callvirt:
                    isVirtual = true;
                    break;
                default:
                    return false;
            }

            var calledMethod = instruction.Operand as MethodReference;
            CreateMethodCall(state,
                             isVirtual,
                             calledMethod,
                             out var retTypeRef,
                             out var retId,
                             out var callArgs,
                             out var callInstr);

            var instrs = new List<SilInstruction> { callInstr };

            // Deref on the object calling in the case of instance method, for null
            // validation on it by Infer. Object "this" is the first argument in the argument list
            // for instance methods.
            if (calledMethod.HasThis && calledMethod.Name != Identifier.ConstructorIdentifier)
            {
                var thisArg = callArgs.First();
                if (thisArg.Expression is VarExpression varExpression)
                {
                    instrs.Insert(0, CreateDereference(varExpression, thisArg.Type, state));
                }
            }

            var callNode = new StatementNode(location: state.CurrentLocation,
                                             kind: StatementNode.StatementNodeKind.Call,
                                             proc: state.ProcDesc,
                                             comment: calledMethod.GetCompatibleFullName());
            callNode.Instructions.AddRange(instrs);
            RegisterNode(state, callNode);

            var returnType = Typ.FromTypeReference(retTypeRef);
            if (!(returnType is Tvoid))
            {
                state.PushExpr(new VarExpression(retId), returnType);
            }
            state.PushInstruction(instruction.Next, callNode);
            state.AppendToPreviousNode = true;
            return true;
        }
    }
}
