using BpmSoftSync.Adapters.FileSystem;

var root = Path.Combine(Path.GetTempPath(), "BpmSoftSync-S03-" + Guid.NewGuid().ToString("N"));
try
{
    BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.RootsAreDatePartitionedUniqueAndNonOverwriting(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.SameRunIdCannotRaceOrOverwriteAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.SeparateStoreInstancesCannotClaimSameRunIdAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.SealedRunRejectsFurtherEvidenceAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.PersistentStableKeysAndSealDefendAcrossStoreInstancesAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.EvidenceEnvelopeTests.SchemaFailurePreventsDurableWriteAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.EvidenceEnvelopeTests.TypedCanaryAndSealScanPreventEveryDurableWriteAsync(root);
    BpmSoftSync.Adapters.FileSystem.Tests.EvidenceEnvelopeTests.OrdinaryUnmarkedLookupValueIsRejectedInEveryStringBucket();
    BpmSoftSync.Adapters.FileSystem.Tests.EvidenceEnvelopeTests.FailedShapeEvidenceAcceptsOnlyClosedEnums();
    await BpmSoftSync.Adapters.FileSystem.Tests.SchemaDiagnosticEvidenceTests.TerminalRecordUsesOnlyTheClosedContractAndReadBackSealAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.SchemaDiagnosticEvidenceTests.UniqueRootsAndClosedSchemaRejectCanariesAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.SchemaDiagnosticEvidenceTests.FailedOrCancelledStagingNeverPublishesAnUnsealedTerminalAsync(root);
    await BpmSoftSync.Adapters.FileSystem.Tests.SchemaDiagnosticEvidenceTests.CompanionGuidStatusIsClosedAndRestrictedToSchemaPackageIdAsync(root);
    BpmSoftSync.Adapters.FileSystem.Tests.RunStoreTests.SameRunIdCannotBeClaimedOnAnotherDateByAnotherStore(root);
    BpmSoftSync.Adapters.FileSystem.Tests.SecretValueScannerTests.CanaryReportsOnlyCategoryLocationAndDigest();
    BpmSoftSync.Adapters.FileSystem.Tests.AuditMetadataTests.AllowlistAcceptsOnlyDeclaredMetadata();
    await BpmSoftSync.Adapters.FileSystem.Tests.OutputEvidenceBoundaryTests.RawLookupValueExistsOnlyInsidePublishedLookupWorkbookAsync(root);
    Console.WriteLine("PASS FileSystem S04/S05 tests"); return 0;
}
catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
