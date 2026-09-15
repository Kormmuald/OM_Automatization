try
{
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.ExactlyTwoIndependentFullReadsProduceBOnlySnapshotAndSafeEvidenceAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.CacheReuseIsRejectedWithoutPassCAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.TargetMutationIsTerminalAndEmitsFailureEvidenceAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.SecondPassBlockerStopsWithoutRetryAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.WorkbookScaleDecisionBlocksSnapshotAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.SealedCollectionContractTamperingIsTerminalAsync();
    Console.WriteLine("PASS CatalogQualificationServiceTests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
