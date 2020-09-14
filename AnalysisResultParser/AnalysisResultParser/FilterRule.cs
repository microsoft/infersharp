// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace AnalysisResultParser
{
    public class FilterRule
    {
        [JsonProperty("supportedIssueType", Required = Required.Always)]
        public string SupportedIssueType { get; set; }

        [JsonProperty("textMustContain")]
        public string TextMustContain { get; set; }
    }
}
