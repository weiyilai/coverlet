﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Coverlet.Core;
using Coverlet.Core.CoverageSamples.Tests;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Tmds.Utils;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void AsyncAwait()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncAwait>(async instance =>
                  {
                    instance.SyncExecution();

                    int res = await (Task<int>)instance.AsyncExecution(true);
                    res = await (Task<int>)instance.AsyncExecution(1);
                    res = await (Task<int>)instance.AsyncExecution(2);
                    res = await (Task<int>)instance.AsyncExecution(3);
                    res = await (Task<int>)instance.ContinuationCalled();
                    res = await (Task<int>)instance.ConfigureAwait();

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // AsyncExecution(bool)
                            (10, 1), (11, 1), (12, 1), (14, 1), (16, 1), (17, 0), (18, 0), (19, 0), (21, 1), (22, 1),
                            // Async
                            (25, 9), (26, 9), (27, 9), (28, 9),
                            // SyncExecution
                            (31, 1), (32, 1), (33, 1),
                            // Sync
                            (36, 1), (37, 1), (38, 1),
                            // AsyncExecution(int)
                            (41, 3), (42, 3), (43, 3), (46, 1), (47, 1), (48, 1), (51, 1),
                            (52, 1), (53, 1), (56, 1), (57, 1), (58, 1), (59, 1),
                            (62, 0), (63, 0), (64, 0), (65, 0), (68, 0), (70, 3), (71, 3),
                            // ContinuationNotCalled
                            (74, 0), (75, 0), (76, 0), (77, 0), (78, 0),
                            // ContinuationCalled -> line 83 should be 1 hit some issue with Continuation state machine
                            (81, 1), (82, 1), (83, 2), (84, 1), (85, 1),
                            // ConfigureAwait
                            (89, 1), (90, 1)
                            )
        .AssertBranchesCovered(BuildConfiguration.Debug, (16, 0, 0), (16, 1, 1), (43, 0, 3), (43, 1, 1), (43, 2, 1), (43, 3, 1), (43, 4, 0))
        // Real branch should be 2, we should try to remove compiler generated branch in method ContinuationNotCalled/ContinuationCalled
        // for Continuation state machine
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_669_1()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_1>(async instance =>
                  {
                    await (Task)instance.Test();
                  },
                  persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
        (97, 1), (98, 1), (99, 1), (101, 1), (102, 1), (103, 1),
        (110, 1), (111, 1), (112, 1), (113, 1),
        (116, 1), (117, 1), (118, 1), (119, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact(Skip = "Unhandled exception: System.InvalidOperationException: Sequence contains more than one matching element, InstrumenterHelper.cs:line 139 ")]
    public void AsyncAwait_Issue_669_2()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_2>(async instance =>
                  {
                    await (ValueTask<System.Net.Http.HttpResponseMessage>)instance.SendRequest();
                  },
                  persistPrepareResultToFile: pathSerialize[0],
                  assemblyLocation: Assembly.GetExecutingAssembly().Location);

          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AsyncAwait.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, (7, 1), (10, 1), (11, 1), (12, 1), (13, 1), (15, 1))
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1177()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1177>(async instance =>
                      {
                        await (Task)instance.Test();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCovered(BuildConfiguration.Debug, (133, 1), (134, 1), (135, 1), (136, 1), (137, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 134);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1233()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1233>(async instance =>
                      {
                        await (Task)instance.Test();
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCovered(BuildConfiguration.Debug, (150, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 150);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void AsyncAwait_Issue_1275()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_1275>(async instance =>
                      {
                        using var cts = new CancellationTokenSource();
                        await (Task)instance.Execute(cts.Token);
                      },
                      persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, [path]);

        Core.Instrumentation.Document document = TestInstrumentationHelper.GetCoverageResult(path).Document("Instrumentation.AsyncAwait.cs");
        document.AssertLinesCoveredFromTo(BuildConfiguration.Debug, 170, 176);
        document.AssertBranchesCovered(BuildConfiguration.Debug, (171, 0, 1), (171, 1, 1));
        Assert.DoesNotContain(document.Branches, x => x.Key.Line == 176);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
