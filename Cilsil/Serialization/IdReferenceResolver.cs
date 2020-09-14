// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Cilsil.Sil;
using Newtonsoft.Json.Serialization;
using System;

namespace Cilsil.Serialization
{
    internal class IdReferenceResolver : IReferenceResolver
    {
        public void AddReference(object context, string reference, object value)
        {
        }

        public string GetReference(object context, object value)
        {
            switch (value)
            {
                case CfgNode cfgNode:
                    return cfgNode.Id.ToString();
                case ProcedureDescription procDesc:
                    return procDesc.PdId.ToString();
                default:
                    return value.ToString();
            }
        }

        public bool IsReferenced(object context, object value)
        {
            return value is CfgNode ||
                   value is ProcedureDescription;
        }

        public object ResolveReference(object context, string reference) =>
            throw new NotImplementedException();
    }
}
