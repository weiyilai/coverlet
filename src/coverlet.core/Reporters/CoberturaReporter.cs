﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Reporters
{
  internal class CoberturaReporter : IReporter
  {
    public ReporterOutputType OutputType => ReporterOutputType.File;

    public string Format => "cobertura";

    public string Extension => "cobertura.xml";

    public string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
    {
      var summary = new CoverageSummary();

      CoverageDetails lineCoverage = CoverageSummary.CalculateLineCoverage(result.Modules);
      CoverageDetails branchCoverage = CoverageSummary.CalculateBranchCoverage(result.Modules);

      var xml = new XDocument();
      var coverage = new XElement("coverage");
      coverage.Add(new XAttribute("line-rate", (CoverageSummary.CalculateLineCoverage(result.Modules).Percent / 100).ToString(CultureInfo.InvariantCulture)));
      coverage.Add(new XAttribute("branch-rate", (CoverageSummary.CalculateBranchCoverage(result.Modules).Percent / 100).ToString(CultureInfo.InvariantCulture)));
      coverage.Add(new XAttribute("version", "1.9"));
      coverage.Add(new XAttribute("timestamp", (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds));

      var sources = new XElement("sources");

      var absolutePaths = new List<string>();
      if (!result.Parameters.DeterministicReport)
      {
        absolutePaths = [.. GetBasePaths(result.Modules, result.Parameters.UseSourceLink)];
        absolutePaths.ForEach(x => sources.Add(new XElement("source", x)));
      }

      var packages = new XElement("packages");
      foreach (KeyValuePair<string, Documents> module in result.Modules)
      {
        var package = new XElement("package");
        package.Add(new XAttribute("name", Path.GetFileNameWithoutExtension(module.Key)));
        package.Add(new XAttribute("line-rate", (CoverageSummary.CalculateLineCoverage(module.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
        package.Add(new XAttribute("branch-rate", (CoverageSummary.CalculateBranchCoverage(module.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
        package.Add(new XAttribute("complexity", CoverageSummary.CalculateCyclomaticComplexity(module.Value)));

        var classes = new XElement("classes");
        foreach (KeyValuePair<string, Classes> document in module.Value)
        {
          foreach (KeyValuePair<string, Methods> cls in document.Value)
          {
            var @class = new XElement("class");
            @class.Add(new XAttribute("name", cls.Key));
            string fileName;
            if (!result.Parameters.DeterministicReport)
            {
              fileName = GetRelativePathFromBase(absolutePaths, document.Key, result.Parameters.UseSourceLink);
            }
            else
            {
              fileName = sourceRootTranslator.ResolveDeterministicPath(document.Key);
            }
            @class.Add(new XAttribute("filename", fileName));
            @class.Add(new XAttribute("line-rate", (CoverageSummary.CalculateLineCoverage(cls.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
            @class.Add(new XAttribute("branch-rate", (CoverageSummary.CalculateBranchCoverage(cls.Value).Percent / 100).ToString(CultureInfo.InvariantCulture)));
            @class.Add(new XAttribute("complexity", CoverageSummary.CalculateCyclomaticComplexity(cls.Value)));

            var classLines = new XElement("lines");
            var methods = new XElement("methods");

            foreach (KeyValuePair<string, Method> meth in cls.Value)
            {
              // Skip all methods with no lines
              if (meth.Value.Lines.Count == 0)
                continue;

              var method = new XElement("method");
              method.Add(new XAttribute("name", meth.Key.Split(':').Last().Split('(').First()));
              method.Add(new XAttribute("signature", "(" + meth.Key.Split(':').Last().Split('(').Last()));
              method.Add(new XAttribute("line-rate", (CoverageSummary.CalculateLineCoverage(meth.Value.Lines).Percent / 100).ToString(CultureInfo.InvariantCulture)));
              method.Add(new XAttribute("branch-rate", (CoverageSummary.CalculateBranchCoverage(meth.Value.Branches).Percent / 100).ToString(CultureInfo.InvariantCulture)));
              method.Add(new XAttribute("complexity", CoverageSummary.CalculateCyclomaticComplexity(meth.Value.Branches)));

              var lines = new XElement("lines");
              foreach (KeyValuePair<int, int> ln in meth.Value.Lines)
              {
                bool isBranchPoint = meth.Value.Branches.Any(b => b.Line == ln.Key);
                var line = new XElement("line");
                line.Add(new XAttribute("number", ln.Key.ToString()));
                line.Add(new XAttribute("hits", ln.Value.ToString()));
                line.Add(new XAttribute("branch", isBranchPoint.ToString()));

                if (isBranchPoint)
                {
                  var branches = meth.Value.Branches.Where(b => b.Line == ln.Key).ToList();
                  CoverageDetails branchInfoCoverage = CoverageSummary.CalculateBranchCoverage(branches);
                  line.Add(new XAttribute("condition-coverage", $"{branchInfoCoverage.Percent.ToString(CultureInfo.InvariantCulture)}% ({branchInfoCoverage.Covered.ToString(CultureInfo.InvariantCulture)}/{branchInfoCoverage.Total.ToString(CultureInfo.InvariantCulture)})"));
                  var conditions = new XElement("conditions");
                  var byOffset = branches.GroupBy(b => b.Offset).ToDictionary(b => b.Key, b => b.ToList());
                  foreach (KeyValuePair<int, List<BranchInfo>> entry in byOffset)
                  {
                    var condition = new XElement("condition");
                    condition.Add(new XAttribute("number", entry.Key));
                    condition.Add(new XAttribute("type", entry.Value.Count > 2 ? "switch" : "jump")); // Just guessing here
                    condition.Add(new XAttribute("coverage", $"{CoverageSummary.CalculateBranchCoverage(entry.Value).Percent.ToString(CultureInfo.InvariantCulture)}%"));
                    conditions.Add(condition);
                  }

                  line.Add(conditions);
                }

                lines.Add(line);
                classLines.Add(line);
              }

              method.Add(lines);
              methods.Add(method);
            }

            @class.Add(methods);
            @class.Add(classLines);
            classes.Add(@class);
          }
        }

        package.Add(classes);
        packages.Add(package);
      }

      coverage.Add(new XAttribute("lines-covered", lineCoverage.Covered.ToString(CultureInfo.InvariantCulture)));
      coverage.Add(new XAttribute("lines-valid", lineCoverage.Total.ToString(CultureInfo.InvariantCulture)));
      coverage.Add(new XAttribute("branches-covered", branchCoverage.Covered.ToString(CultureInfo.InvariantCulture)));
      coverage.Add(new XAttribute("branches-valid", branchCoverage.Total.ToString(CultureInfo.InvariantCulture)));

      coverage.Add(sources);
      coverage.Add(packages);
      xml.Add(coverage);

      using var stream = new MemoryStream();
      using var streamWriter = new StreamWriter(stream, new UTF8Encoding(false));
      xml.Save(streamWriter);

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IEnumerable<string> GetBasePaths(Modules modules, bool useSourceLink)
    {
      /*
           Workflow

           Path1 c:\dir1\dir2\file1.cs
           Path2 c:\dir1\file2.cs
           Path3 e:\dir1\file2.cs

           1) Search for root dir 
              c:\ ->	c:\dir1\dir2\file1.cs
                      c:\dir1\file2.cs
              e:\ ->	e:\dir1\file2.cs

           2) Split path on directory separator i.e. for record c:\ ordered ascending by fragment elements
               Path1 = [c:|dir1|file2.cs]
               Path2 = [c:|dir1|dir2|file1.cs]

           3)  Find longest shared path comparing indexes		 
               Path1[0]    = Path2[0], ..., PathY[0]     -> add to final fragment list
               Path1[n]    = Path2[n], ..., PathY[n]     -> add to final fragment list
               Path1[n+1] != Path2[n+1], ..., PathY[n+1] -> break, Path1[n] was last shared fragment 		 

           4) Concat created fragment list
      */
      if (useSourceLink)
      {
        return [string.Empty];
      }

      return modules.Values.SelectMany(k => k.Keys).GroupBy(Directory.GetDirectoryRoot).Select(group =>
      {
        var splittedPaths = group.Select(absolutePath => absolutePath.Split(Path.DirectorySeparatorChar))
                                       .OrderBy(absolutePath => absolutePath.Length).ToList();
        if (splittedPaths.Count == 1)
        {
          return group.Key;
        }

        var basePathFragments = new List<string>();
        bool stopSearch = false;
        splittedPaths[0].Select((value, index) => (value, index)).ToList().ForEach(fragmentIndexPair =>
              {
                if (stopSearch)
                {
                  return;
                }

                if (splittedPaths.All(sp => fragmentIndexPair.value.Equals(sp[fragmentIndexPair.index])))
                {
                  basePathFragments.Add(fragmentIndexPair.value);
                }
                else
                {
                  stopSearch = true;
                }
              });
        return string.Concat(string.Join(Path.DirectorySeparatorChar.ToString(), basePathFragments), Path.DirectorySeparatorChar);
      });
    }

    private static string GetRelativePathFromBase(IEnumerable<string> basePaths, string path, bool useSourceLink)
    {
      if (useSourceLink)
      {
        return path;
      }

      foreach (string basePath in basePaths)
      {
        if (path.StartsWith(basePath))
        {
          return path.Substring(basePath.Length);
        }
      }
      return path;
    }
  }
}
