// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Cilsil.Sil.Expressions;
using Cilsil.Sil.Instructions;
using Cilsil.Sil.Types;
using Cilsil.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Cilsil.Cil.Parsers
{
    internal class Initobj : InstructionParser
    {
        protected override bool ParseCilInstructionInternal(Instruction instruction,
                                                            ProgramState state)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Initobj:
                    var structTypeDefinition = instruction.Operand as TypeDefinition;
                    var structInitializationInstructions = new List<SilInstruction>();

                    (_, var pointerType) = state.Pop();

                    if (!(pointerType is Address address))
                    {
                        Log.WriteError("Unexpected pointer type " +
                                       $"{pointerType.GetType()} on stack.", debug: true);
                        return false;
                    }
                    // Is null when operand refers to a struct which doesn't have fields (for
                    // example: nullable types, immutable array, and anonymous types). In this
                    // case, we simply allocate an object which represents the value type -- this
                    // would only cause false positive warnings in Infer were it possible to assign
                    // null to the type, which will never occur in the CIL and which will therefore
                    // never occur in the SIL.
                    if (structTypeDefinition == null)
                    {
                        if (instruction.Operand is TypeReference typeReference)
                        {
                            (var memoryAllocationCall, var anonymousTypeVariable) =
                                CreateMemoryAllocationCall(typeReference, state);
                            structInitializationInstructions.Add(memoryAllocationCall);
                            structInitializationInstructions.Add(
                                new Store(address.Value,
                                          anonymousTypeVariable,
                                          pointerType.StripPointer(),
                                          state.CurrentLocation));
                        }
                        else
                        {
                            Log.WriteError($"Unexpected initobj type {instruction.Operand}", debug: true);
                            return false;
                        }
                    }
                    else
                    {
                        (var memoryAllocationCall, var structVariable) =
                            CreateMemoryAllocationCall(structTypeDefinition, state);
                        structInitializationInstructions.Add(memoryAllocationCall);

                        try
                        {
                            var parentTypeDefinitions = new HashSet<TypeDefinition>
                            {
                                structTypeDefinition
                            };
                            InitializeStruct(structVariable,
                                             structTypeDefinition,
                                             structInitializationInstructions,
                                             state,
                                             parentTypeDefinitions);
                        }
                        catch (Exception e)
                        {
                            Log.WriteError(e.Message, debug: true);
                            return false;
                        }
                        // Stores the initialized struct into the given address.
                        structInitializationInstructions.Add(
                            new Store(address.Value,
                                      structVariable,
                                      pointerType.StripPointer(),
                                      state.CurrentLocation));
                    }
                    var node = AddMethodBodyInstructionsToCfg(
                        state,
                        structInitializationInstructions.ToArray());
                    state.PushInstruction(instruction.Next, node);
                    return true;
                default:
                    return false;
            }
        }

        private void InitializeStruct(VarExpression fieldParentVariable,
                                      TypeDefinition parentTypeDefinition,
                                      List<SilInstruction> structFieldInitializationInstructions,
                                      ProgramState state,
                                      HashSet<TypeDefinition> parentTypeDefinitions)
        {
            foreach (var fieldReference in parentTypeDefinition.Fields)
            {
                var fieldExpression = CreateFieldExpression(fieldParentVariable, fieldReference);

                var defaultValue = GetDefaultValue(fieldReference.FieldType);

                // Struct field; the default value must be recursively filled.
                if (fieldReference.FieldType is TypeDefinition fieldTypeDefinition &&
                    fieldTypeDefinition.BaseType != null &&
                    fieldTypeDefinition.BaseType.FullName == "System.ValueType")
                {
                    if (parentTypeDefinitions.Contains(fieldTypeDefinition))
                    {
                        throw new Exception("Unable to handle cyclical struct type " +
                                            $"{fieldTypeDefinition.FullName}");
                    }

                    // Allocates the struct field and stores it into the parent.
                    (var structFieldMemoryAllocationCall, var structFieldVariable) =
                        CreateMemoryAllocationCall(fieldTypeDefinition, state);
                    structFieldInitializationInstructions.Add(structFieldMemoryAllocationCall);
                    structFieldInitializationInstructions.Add(
                        new Store(fieldExpression,
                                  structFieldVariable,
                                  Typ.FromTypeReferenceNoPointer(fieldReference.DeclaringType),
                                  state.CurrentLocation));

                    // Add the field to the parent definitions before recursing; remove after
                    // recursive call returns.
                    parentTypeDefinitions.Add(fieldTypeDefinition);
                    // Recursively initializes the struct field.
                    InitializeStruct(structFieldVariable,
                                     fieldTypeDefinition,
                                     structFieldInitializationInstructions,
                                     state,
                                     parentTypeDefinitions);
                    parentTypeDefinitions.Remove(fieldTypeDefinition);
                }
                else
                {
                    // In this case, there was a non-struct field for which we could not produce
                    // the correct default value.
                    if (defaultValue == null)
                    {
                        throw new Exception("Unhandled type in initobj struct.");
                    }
                    // Stores the default constant for non-struct fields.
                    structFieldInitializationInstructions.Add(
                        new Store(fieldExpression,
                                  defaultValue,
                                  Typ.FromTypeReferenceNoPointer(
                                      fieldReference.DeclaringType),
                                  state.CurrentLocation));
                }
            }
        }

        /// <summary>
        /// Gets the default value for the given type; handles the different types returned by 
        /// <see cref="Typ.FromTypeReferenceNoPointer(TypeReference)"/>.
        /// </summary>
        /// <param name="type">The type for which to produce the default value.</param>
        /// <returns>The <see cref="ConstExpression"/> representing the default value.</returns>
        private ConstExpression GetDefaultValue(TypeReference type)
        {
            if (type.IsGenericParameter)
            {
                throw new InvalidOperationException("Unhandled initobj generic parameter type.");
            }
            var silType = Typ.FromTypeReferenceNoPointer(type);
            if (silType is Tint silInt)
            {
                switch (silInt.Kind)
                {
                    // Cases represented using an unsigned integer.
                    case Tint.IntKind.IUChar:
                    case Tint.IntKind.IUInt:
                    case Tint.IntKind.IUShort:
                    case Tint.IntKind.IULong:
                    case Tint.IntKind.IULongLong:
                    case Tint.IntKind.IU128:
                        return new ConstExpression(new IntRepresentation(0, true, false));
                    // Cases represented using a signed integer.
                    // Booleans are represented as signed integers in LdcParser, because their
                    // values are pushed using ldc.i4.0/ldc.i4.1.
                    case Tint.IntKind.IBool:
                    case Tint.IntKind.IChar:
                    case Tint.IntKind.ISChar:
                    case Tint.IntKind.IInt:
                    case Tint.IntKind.IShort:
                    case Tint.IntKind.ILong:
                    case Tint.IntKind.ILongLong:
                    case Tint.IntKind.I128:
                        return new ConstExpression(new IntRepresentation(0, false, false));
                    default:
                        throw new ArgumentException("Unhandled initobj SIL integer type.");
                }
            }
            else if (silType is Tfloat silFloat)
            {
                switch (silFloat.Kind)
                {
                    case Tfloat.FloatKind.FFloat:
                    case Tfloat.FloatKind.FDouble:
                    case Tfloat.FloatKind.FLongDouble:
                        return new ConstExpression(0F);
                    default:
                        Log.WriteError("Unhandled initobj SIL float or double type.", debug: true);
                        return null;
                }
            }
            else if (silType is Tptr pointer)
            {
                Log.WriteError("Unhandled initobj tptr with underlying type " +
                               $"{pointer.StripPointer()}", debug: true);
                return null;
            }
            else if (silType is Tvoid)
            {
                Log.WriteError("Unexpected initobj void type.", debug: true);
                return null;
            }
            else if (type is TypeDefinition typeDefinition)
            {
                // Cannot compute default value in this case; BaseType must be non-null.
                if (typeDefinition.BaseType == null)
                {
                    Log.WriteError($"Type with base type {typeDefinition.FullName} is null.", debug: true);
                    return null;
                }
                // An expression representing null, used for reference types.
                if ((silType is Tarray ||
                     silType is Tstruct && typeDefinition.BaseType.FullName == "System.Object"))
                {
                    return new ConstExpression(new IntRepresentation(0, false, true));
                }
                // A null value is returned, which should not get used; the struct field must be
                // recursively initialized.
                else if ((silType is Tstruct &&
                          typeDefinition.BaseType.FullName == "System.ValueType"))
                {
                    return null;
                }
                else
                {
                    Log.WriteError($"Unhandled initobj silType: {silType}", debug: true);
                    return null;
                }
            }
            else
            {
                Log.WriteError($"Unhandled initobj type: {type.FullName}", debug: true);
                return null;
            }
        }
    }
}