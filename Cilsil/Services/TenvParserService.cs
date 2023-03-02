// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Extensions;
using Cilsil.Services.Results;
using Cilsil.Sil;
using Cilsil.Sil.Types;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cilsil.Services
{
    internal class TenvParserService : IService
    {
        public IEnumerable<TypeDefinition> Types { get; private set; }

        public IEnumerable<ModuleDefinition> ModuleDefinitions { get; private set; }

        public bool WriteConsoleProgress { get; private set; }

        public bool LoadTypeEnvironment { get; private set; }

        public TenvParserService(bool writeConsoleProgress,
                                 bool loadTenv,
                                 IEnumerable<TypeDefinition> types = null,
                                 IEnumerable<ModuleDefinition> moduleDefinitions = null)
        {
            WriteConsoleProgress = writeConsoleProgress;
            LoadTypeEnvironment = loadTenv;
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

        private static TypeEnvironment LoadIDisposableTypes()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                                       .Single(str => str.EndsWith("idisposableTenv.json"));
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return TypeEnvironment.FromJson(reader.ReadToEnd());
            }
        }

        private TypeEnvironment ComputeTypeEnvironment()
        {
            var tenv = LoadTypeEnvironment ? LoadIDisposableTypes() : new TypeEnvironment();
            Log.WriteLine("Translation stage 2/3: Computing type environment.");
            var i = 0;
            var total = Types.Count();
            using (var bar = new ProgressBar())
            {
                foreach (var t in Types)
                {
                    try
                    {
                        RegisterCilType(t, tenv);
                    }
                    catch (NotImplementedException e)
                    {
                        Log.WriteWarning($"Could not parse type {t.FullName}.");
                        Log.WriteWarning($"{e.Message}");
                        continue;
                    }
                    i++;
                    bar.Report((double)i / total);
                    if (WriteConsoleProgress)
                    {
                        Log.WriteProgressLine(i, total);
                    }
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
            var baseSupers = new List<CsuTypeName>();
            var baseTypes = new List<CsuTypeName>();

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
                            baseSupers.AddRange(baseTypeEntry.TypeStruct.Supers);
                            baseTypes.Add(new CsuTypeName(CsuKind.Class, baseClass.GetCompatibleFullName()));
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
                                    .Concat(baseInstanceFields)
                                    .ToList();
            var staticFields = allFields
                                    .Where(f => f.IsStatic)
                                    .Select(f => f.Field)
                                    .Concat(baseStaticFields)
                                    .ToList();
            var procNames = type.Methods.Select(m => new ProcedureName(m)).ToList();

            var typeStruct = new Struct(instanceFields,
                                        staticFields,
                                        baseSupers.Concat(baseTypes).ToList(),
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
