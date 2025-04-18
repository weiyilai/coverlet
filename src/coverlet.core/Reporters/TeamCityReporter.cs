﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Reporters
{
  internal class TeamCityReporter : IReporter
  {
    public ReporterOutputType OutputType => ReporterOutputType.Console;

    public string Format => "teamcity";

    public string Extension => null;

    public string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
    {
      if (result.Parameters.DeterministicReport)
      {
        throw new NotSupportedException("Deterministic report not supported by teamcity reporter");
      }

      // Calculate coverage
      CoverageDetails overallLineCoverage = CoverageSummary.CalculateLineCoverage(result.Modules);
      CoverageDetails overallBranchCoverage = CoverageSummary.CalculateBranchCoverage(result.Modules);
      CoverageDetails overallMethodCoverage = CoverageSummary.CalculateMethodCoverage(result.Modules);

      // Report coverage
      var stringBuilder = new StringBuilder();
      OutputLineCoverage(overallLineCoverage, stringBuilder);
      OutputBranchCoverage(overallBranchCoverage, stringBuilder);
      OutputMethodCoverage(overallMethodCoverage, stringBuilder);

      // Return a placeholder
      return stringBuilder.ToString();
    }

    private static void OutputLineCoverage(CoverageDetails coverageDetails, StringBuilder builder)
    {
      // The number of covered lines
      OutputTeamCityServiceMessage("CodeCoverageAbsLCovered", coverageDetails.Covered, builder);

      // Line-level code coverage
      OutputTeamCityServiceMessage("CodeCoverageAbsLTotal", coverageDetails.Total, builder);
    }

    private static void OutputBranchCoverage(CoverageDetails coverageDetails, StringBuilder builder)
    {
      // The number of covered branches
      OutputTeamCityServiceMessage("CodeCoverageAbsBCovered", coverageDetails.Covered, builder);

      // Branch-level code coverage
      OutputTeamCityServiceMessage("CodeCoverageAbsBTotal", coverageDetails.Total, builder);
    }

    private static void OutputMethodCoverage(CoverageDetails coverageDetails, StringBuilder builder)
    {
      // The number of covered methods
      OutputTeamCityServiceMessage("CodeCoverageAbsMCovered", coverageDetails.Covered, builder);

      // Method-level code coverage
      OutputTeamCityServiceMessage("CodeCoverageAbsMTotal", coverageDetails.Total, builder);
    }

    private static void OutputTeamCityServiceMessage(string key, double value, StringBuilder builder)
    {
      builder.AppendLine($"##teamcity[buildStatisticValue key='{key}' value='{value.ToString("0.##", new CultureInfo("en-US"))}']");
    }
  }
}
