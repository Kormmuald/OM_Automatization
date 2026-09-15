try
{
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.ExactlyTwoIndependentFullReadsProduceBOnlySnapshotAndSafeEvidenceAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.CacheReuseIsRejectedWithoutPassCAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.TargetMutationIsTerminalAndEmitsFailureEvidenceAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.OpaquePackageProvenanceChangeIsNotMergedAcrossPassesAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.EmptyOpaquePackageProvenanceFailsClosedBeforePassBAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.DuplicateSchemaAndPackagePrimaryIdentityFailsClosedBeforePassBAndOutputAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.SecondPassBlockerStopsWithoutRetryAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.WorkbookScaleDecisionBlocksSnapshotAsync();
    await BpmSoftSync.Application.Tests.CatalogQualificationServiceTests.SealedCollectionContractTamperingIsTerminalAsync();
    await BpmSoftSync.Application.Tests.BoundedSchemaDiagnosticWorkflowTests.ReadsOnceAndReturnsTheFirstSafeBlockerAsync();
    await BpmSoftSync.Application.Tests.BoundedSchemaDiagnosticWorkflowTests.CompletedSchemaPhaseIsAlwaysNonQualifyingAsync();
    Console.WriteLine("PASS CatalogQualificationServiceTests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
