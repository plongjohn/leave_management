using LeaveManagement.Api.Tests.Infrastructure;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(ApiTestFixture))]

// All tests share one test database that is reset before each test, so they must not run in parallel.
[assembly: Parallelization(Mode = ParallelMode.None)]
