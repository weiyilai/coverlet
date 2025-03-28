﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;
using Moq;
using Xunit;

namespace Coverlet.MSbuild.Tasks.Reporters.Tests
{
  public class Reporters
  {
    // we use lcov with extension .info and cobertura with extension .cobertura.xml
    // to have all possible extension format
    // empty coverletOutput is not possible thanks to props default
    [Theory]
    // single tfm
    [InlineData("", "/folder/reportFolder/", "lcov", "/folder/reportFolder/coverage.info")]
    [InlineData(null, "/folder/reportFolder/", "cobertura", "/folder/reportFolder/coverage.cobertura.xml")]
    [InlineData(null, "/folder/reportFolder/file.ext", "cobertura", "/folder/reportFolder/file.ext")]
    [InlineData(null, "/folder/reportFolder/file.ext1.ext2", "cobertura", "/folder/reportFolder/file.ext1.ext2")]
    [InlineData(null, "/folder/reportFolder/file", "cobertura", "/folder/reportFolder/file.cobertura.xml")]
    // multiple tfm
    [InlineData("netcoreapp2.2", "/folder/reportFolder/", "lcov", "/folder/reportFolder/coverage.netcoreapp2.2.info")]
    [InlineData("netcoreapp2.2", "/folder/reportFolder/", "cobertura", "/folder/reportFolder/coverage.netcoreapp2.2.cobertura.xml")]
    [InlineData("net472", "/folder/reportFolder/file.ext", "cobertura", "/folder/reportFolder/file.net472.ext")]
    [InlineData("net472", "/folder/reportFolder/file.ext1.ext2", "cobertura", "/folder/reportFolder/file.ext1.net472.ext2")]
    [InlineData("netcoreapp2.2", "/folder/reportFolder/file", "cobertura", "/folder/reportFolder/file.netcoreapp2.2.cobertura.xml")]
    public void Msbuild_ReportWriter(string? coverletMultiTargetFrameworksCurrentTFM, string coverletOutput, string reportFormat, string expectedFileName)
    {
      var fileSystem = new Mock<IFileSystem>();

      var reportWriter = new ReportWriter(
          coverletMultiTargetFrameworksCurrentTFM,
          // mimic code inside CoverageResultTask.cs
          Path.GetDirectoryName(coverletOutput),
          coverletOutput,
          new ReporterFactory(reportFormat).CreateReporter(),
          fileSystem.Object,
          new CoverageResult() { Modules = new Modules(), Parameters = new CoverageParameters() }, null);

      string path = reportWriter.WriteReport();
      //remove drive for windows paths
      if (path.Contains(':'))
      {
        path = path[2..];
      }
      // Path.Combine depends on OS so we can change only win side to avoid duplication
      Assert.Equal(path.Replace('/', Path.DirectorySeparatorChar), expectedFileName.Replace('/', Path.DirectorySeparatorChar));
    }
  }
}
