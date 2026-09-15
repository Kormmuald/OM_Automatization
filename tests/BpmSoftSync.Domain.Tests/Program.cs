try
{
    BpmSoftSync.Domain.Tests.DomainContractTests.RequiredContractsExist();
    Console.WriteLine("PASS DomainContractTests.RequiredContractsExist");
    BpmSoftSync.Domain.Tests.CatalogReaderPagingTests.EveryAdversarialFixtureTerminatesWithNamedBlocker();
    BpmSoftSync.Domain.Tests.CatalogReaderPagingTests.ValidPagingHasCanonicalSafeManifests();
    Console.WriteLine("PASS CatalogReaderPagingTests");
    BpmSoftSync.Domain.Tests.FixtureManifestTests.EveryApprovedFixtureHasMatchingSha256Digest();
    Console.WriteLine("PASS FixtureManifestTests");
    BpmSoftSync.Domain.Tests.WorkspaceInventoryTests.PackageLayerIdentityDoesNotMergeDisplayNames();
    BpmSoftSync.Domain.Tests.WorkspaceInventoryTests.EverySourceItemHasExactlyOneSupportStatus();
    BpmSoftSync.Domain.Tests.WorkspaceInventoryTests.TypedInventoryRetainsSchemaCandidateAndDisplayCollision();
    Console.WriteLine("PASS WorkspaceInventoryTests");
    BpmSoftSync.Domain.Tests.TargetFingerprintTests.PropertyOrderDoesNotChangeDigestButContractDataDoes();
    BpmSoftSync.Domain.Tests.TargetFingerprintTests.SemanticallyEquivalentJsonPropertyOrdersProduceSameFingerprint();
    BpmSoftSync.Domain.Tests.TargetFingerprintTests.StructuredSchemaIdentityAndMetadataDigestAreCanonicalAndSensitive();
    Console.WriteLine("PASS TargetFingerprintTests");
    BpmSoftSync.Domain.Tests.LegacyDispositionTests.ManifestClassifiesEveryLegacySemantic();
    BpmSoftSync.Domain.Tests.LegacyDispositionTests.InvalidDispositionIsRejected();
    Console.WriteLine("PASS LegacyDispositionTests");
    BpmSoftSync.Domain.Tests.CatalogQualificationTests.ExactlyTwoPassesReconcileOrSealTerminalChange();
    BpmSoftSync.Domain.Tests.CatalogQualificationTests.FullReconciliationCoversEverySafeComponentAndSnapshotGate();
    BpmSoftSync.Domain.Tests.CatalogQualificationTests.ScopeDigestIsCanonicalAndSensitiveToOrderedContract();
    BpmSoftSync.Domain.Tests.CatalogQualificationTests.WorkbookScaleLimitIncludesHeaderRow();
    BpmSoftSync.Domain.Tests.WorkbookScaleForecastTests.IsDiagnosticOnlyAndFailsClosedForUnknownLimits();
    Console.WriteLine("PASS S04 qualification and forecast tests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
