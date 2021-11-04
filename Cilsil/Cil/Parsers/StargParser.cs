// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cilsil.Cil.Parsers
{
    internal class StargParser : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            int index;
            switch (instruction.OpCode.Code)
            {
                case Code.Starg:
                case Code.Starg_S:
                    // Sequence accounts for the implict "this" argument, if applicable.
                    try
                    {
                        index = (instruction.Operand as ParameterDefinition).Sequence;
                    }
                    catch (System.InvalidCastException e)
                    {
                        Log.instance.Warn(e.Message);
                        return false;
                    }
                    break;
                default:
                    return false;
            }

            (var value, var type) = state.Pop();
            var variable = new LocalVariable(ArgumentName(index, state.Method), state.Method);
            var instr = new Store(lvalue: new LvarExpression(variable),
                                  rvalue: value,
                                  type: type,
                                  location: state.CurrentLocation);
            var node = AddMethodBodyInstructionsToCfg(state, instr);
            state.PushInstruction(instruction.Next, node);
            return true;
        }
    }
}
