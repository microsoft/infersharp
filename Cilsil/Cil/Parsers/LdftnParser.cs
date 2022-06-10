using System;
using System.Linq;
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
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
                case Code.Ldftn:
                    // At the moment, we only want to handle the Ldftn opcode,
                    //  For now, we only want to do this if we are in the middle of adding or removing a event handler
                    //  This seems to appear as a newobj followed by a call/callvirt to add_<event name> or remove_<event name>
                    //  (Assuming the event handler being added is a named function, not an anonymous function)
                    var functionLoaded = instruction.Operand as MethodDefinition;
                    // may want nullity checks on these two vars...
                    var newobjInstr = instruction.Next;
                    var callInstr = instruction.Next.Next;
                    var newobjOperand = newobjInstr.Operand as MethodReference;
                    var callOperand = callInstr.Operand as MethodReference;
                    if (// make sure casts above are fine
                           instruction.Operand is MethodDefinition
                        && callInstr.Operand is MethodReference
                        && newobjInstr.Operand is MethodReference
                        // check the opcodes are as expected 
                        && newobjInstr.OpCode.Code is Code.Newobj
                        && (callInstr.OpCode.Code is Code.Call || callInstr.OpCode.Code is Code.Callvirt)
                        // check names (calling an add_ or remove_) and types (the call takes an argument of the newobj)
                        // checking types is overkill, isn't it? If it didn't line up, there is a bigger problem...
                        && (callOperand.Name.StartsWith("add_") || callOperand.Name.StartsWith("remove_"))
                        && newobjOperand.DeclaringType.FullName.Contains("Event") // should be the case
                        && callOperand.Parameters.Any(p => p.ParameterType.Resolve() == newobjOperand.DeclaringType.Resolve())
                        )
                    { // fairly confident this is adding or removing an event handler.
                        // but this is also too strict, for example adding a lambda function as a handler
                        // or creating the event handler separate from the actual adding of the event handler
                    } 
                    else {
                        return false;
                    }

                    // options for what to push onto the stack:
                    //  a global variable 
                    //  a var expression
                    //  a lfield?
                    //  perhaps best is the constexpression (give it a ProcedureName [makable from a MethodReference {MethodDefinition extends MethodReference}])
                    // whatever it is, it should be consumed by the following newobj
                    state.PushExpr(
                        new ConstExpression(new ProcedureName(functionLoaded)),
                        // still need to fix/confirm the type... (Object, IntPtr, TypedReference, Void)
                        //new Tptr(Tptr.PtrKind.Pk_pointer, new Tint(Tint.IntKind.IInt))
                        Typ.FromTypeReference(state.Method.Module.TypeSystem.IntPtr)
                        //Typ.FromTypeReference(state.Method.Module.TypeSystem.Object)
                        );
                    //Log.Write(instruction.Operand)
                    // no need to add any explicit instructions, just load the next for parsing
                    state.PushInstruction(instruction.Next);
                    break;
                case Code.Ldvirtftn:
                // something something pop the object reference from the stack, then do similar to above...
                default:
                    return false;
            }
            return true;
        }
    }
}
