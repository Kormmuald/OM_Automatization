try
{
    await BpmSoftSync.Cli.Tests.CatalogValidateOfflineTests.FixtureOnlyCommandProducesSafeResultAsync();
    Console.WriteLine("PASS CatalogValidateOfflineTests.FixtureOnlyCommandProducesSafeResultAsync");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
