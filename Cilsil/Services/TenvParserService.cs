﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cilsil.Services
{
    internal class TenvParserService : IService
    {
        public IEnumerable<TypeDefinition> Types { get; private set; }

        public IEnumerable<ModuleDefinition> ModuleDefinitions { get; private set; }

        public TenvParserService(IEnumerable<TypeDefinition> types = null,
                                 IEnumerable<ModuleDefinition> moduleDefinitions = null)
        {
            Types = types;
            ModuleDefinitions = moduleDefinitions;
        }

        public ServiceExecutionResult Execute()
        {
            if (Types == null)
            {
                if (ModuleDefinitions == null)
                {
                    throw new ServiceExecutionException(
                        "ModuleDefinitions must be specified to parser type environment.");
                }
                else
                {
                    Types = ModuleDefinitions
                            .SelectMany(m => m.Types)
                            .SelectMany(t => t.GetAllNestedTypes());
                }
            }
            var tenv = ComputeTypeEnvironment();
            return new TenvParserResult(tenv);
        }

        public ServiceExecutionResult ExecuteFromResult(ServiceExecutionResult result)
        {
            if (result.GetResult<DecompilationResult>() is DecompilationResult decompilationResult)
            {
                // For type environment, we want to parse all available modules and types whether 
                // we have symbols for them or not.
                Types = decompilationResult.TypesWithSymbols
                    .Concat(decompilationResult.TypesWithNoSymbols);
                ModuleDefinitions = decompilationResult.ModuleDefinitionsWithSymbols
                    .Concat(decompilationResult.ModuleDefinitionsWithNoSymbols);
                return Execute();
            }
            else
            {
                throw new ServiceExecutionException(
                    $"{nameof(TenvParserService)} must be executed after " +
                    nameof(DecompilationService));
            }
        }

        private TypeEnvironment ComputeTypeEnvironment()
        {
            var tenv = new TypeEnvironment();
            Log.WriteLine("Computing type environment.");
            var i = 0;
            var total = Types.Count();
            using (var progress = new ProgressBar()) {
                foreach (var t in Types)
                {
                    RegisterCilType(t, tenv);
                    i++;
                    progress.Report((double)i / total);
                }
            }
            return tenv;
        }

        private TypeEntry RegisterCilType(TypeDefinition type, TypeEnvironment typeEnvironment)
        {
            var typeFullName = type.GetCompatibleFullName();
            if (typeEnvironment.ContainsType(typeFullName))
            {
                return typeEnvironment[typeFullName];
            }

            // Gets all the classes from which the input class inherits.
            var baseClasses = type.Interfaces.Select(i => i.InterfaceType).Append(type.BaseType);
            var baseInstanceFields = new List<FieldIdentifier>();
            var baseStaticFields = new List<FieldIdentifier>();
            var baseSupers = new List<string>();
            var baseTypes = new List<string>();

            // Aggregates the instance and static fields, the super
            foreach (var baseClass in baseClasses)
            {
                if (baseClass != null)
                {
                    try
                    {
                        var resolvedBaseClass = baseClass.Resolve();
                        if (resolvedBaseClass != null)
                        {
                            var baseTypeEntry = RegisterCilType(resolvedBaseClass, typeEnvironment);

                            baseInstanceFields.AddRange(baseTypeEntry.TypeStruct.InstanceFields);
                            baseStaticFields.AddRange(baseTypeEntry.TypeStruct.StaticFields);
                            baseSupers.AddRange(baseTypeEntry.TypeStruct.Supers.Select(s => s.Name));
                            baseTypes.Add(baseClass.GetCompatibleFullName());
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            var allFields = type.Fields.Select(
                f => (f.IsStatic,
                      Field: new FieldIdentifier(f.GetCompatibleFullName(),
                      Typ.FromTypeReference(f.FieldType))));

            var instanceFields = allFields
                                    .Where(f => !f.IsStatic)
                                    .Select(f => f.Field)
                                    .Concat(baseInstanceFields);
            var staticFields = allFields
                                    .Where(f => f.IsStatic)
                                    .Select(f => f.Field)
                                    .Concat(baseStaticFields);
            var procNames = type.Methods.Select(m => new ProcedureName(m));

            var typeStruct = new Struct(instanceFields,
                                        staticFields,
                                        baseSupers.Concat(baseTypes),
                                        procNames);

            var typeEntry = new TypeEntry
            {
                TypeName = TypeName.FromTypeReference(type),
                TypeStruct = typeStruct,
            };
            typeEnvironment[typeFullName] = typeEntry;
            return typeEntry;
        }
    }
}
