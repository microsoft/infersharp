// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AnalysisResultParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputPath = args.ElementAtOrDefault(0);
            var outputPath = args.ElementAtOrDefault(1);
            var changedFilePaths = args.ElementAtOrDefault(2);

            try
            {
                var issueList = GetIssueList(inputPath);
                var filterRules = GetFilterRules();

                var filteredIssues = issueList.Where(
                    x => filterRules.Any(y => x.Contains(y.SupportedIssueType)
                    && (string.IsNullOrEmpty(y.TextMustContain) || x.Contains(y.TextMustContain))))
                    .Select(s => s.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                if (!string.IsNullOrEmpty(changedFilePaths))
                {
                    var changedFilePathList = changedFilePaths
                        .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Split(",").Select(p => p.Trim()).ToList();
                    filteredIssues = filteredIssues.Where(x => changedFilePathList.Any(y => x.Contains(y)));
                }

                using (var streamWriter = new StreamWriter(outputPath))
                {
                    if (filteredIssues.Count() == 0)
                    {
                        streamWriter.WriteLine("No issues were found.");
                    }
                    else
                    {
                        foreach (string filteredIssue in filteredIssues)
                        {
                            streamWriter.WriteLine(filteredIssue + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong...");
                Console.WriteLine(e.Message);
            }
        }

        private static IEnumerable<string> GetIssueList(string path)
        {
            using (var streamReader = new StreamReader(path))
            {
                var content = streamReader.ReadToEnd().Split("\n\n").Select(p => p.Trim()).ToList();
                var issueList = content.Take(content.IndexOf("Summary of the reports")).ToList();
                issueList.Sort();
                return issueList;
            }
        }

        private static IEnumerable<FilterRule> GetFilterRules()
        {
            using (var streamReader = new StreamReader(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FilterRules.json")))
            {
                var content = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject<IEnumerable<FilterRule>>(content);
            }
        }
    }
}
