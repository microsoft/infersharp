// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class NewobjParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                // Object construction in SIL is represented as follows:
                // n${i} = _fun___new (sizeof (ExampleClass))
                // n${i+1} = _fun_ExampleClass.ctor(ni)
                // x = n${i}, where x is the program variable
                case Code.Newobj:
                    var constructorMethod = instruction.Operand as MethodReference;
                    var objectTypeReference = constructorMethod.DeclaringType;

                    if (!state.ProgramStackIsEmpty())
                    {
                        (var value, var type) = state.Peek();

                        // In this case, a delegate wrapper is being created. We simply disregard this
                        // wrapper creation and directly push the function pointer.
                        if ((value is ConstExpression expr && expr.ConstValue is ProcedureName) &&
                            type.StripPointer() is Tfun &&
                            MethodDeclaringTypeIsDelegateType(constructorMethod) &&
                            constructorMethod.Parameters.Count == 2)
                        {
                            (var functionValue, var functionType) = state.Pop();
                            // Discard the second parameter of the delegate constructor.
                            state.Pop();
                            // We push the function pointer back onto the stack. 
                            state.PushExpr(functionValue, functionType);
                            state.PushInstruction(instruction.Next);
                            return true;
                        }
                    }
                    (var memoryAllocationCall, var objectVariable) = CreateMemoryAllocationCall(
                        objectTypeReference, state);
                    state.PushExpr(objectVariable, Typ.FromTypeReference(objectTypeReference));

                    // Represents constructor call; we discard the return var as it's not needed.
                    CreateMethodCall(state,
                                     false,
                                     constructorMethod,
                                     out _,
                                     out _,
                                     out _,
                                     out var constructorCall,
                                     isConstructorCall: true);

                    var newNode = new StatementNode(location: state.CurrentLocation,
                                                    kind: StatementNode.StatementNodeKind.Call,
                                                    proc: state.ProcDesc,
                                                    comment: constructorMethod
                                                                .GetCompatibleFullName());
                    newNode.Instructions.Add(memoryAllocationCall);
                    newNode.Instructions.Add(constructorCall);
                    RegisterNode(state, newNode);

                    // The first copy of this stack item was popped in the invocation of the
                    // constructor, so we push another on.
                    state.PushExpr(objectVariable, Typ.FromTypeReference(objectTypeReference));

                    state.PushInstruction(instruction.Next, newNode);
                    // Append the next instruction (which should be stloc, for representing
                    // storage of the constructed object into a local variable) to this new node.
                    state.AppendToPreviousNode = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
