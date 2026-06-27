using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}

namespace TourPlanner.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        [ModuleInitializer]
        public static void InitializeAssembly()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}

