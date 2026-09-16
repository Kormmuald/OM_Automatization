if (args is ["--emit", var output])
{
    var pair = await new BpmSoftSync.Adapters.Excel.WorkbookPairMaterializer().StageValidatedPairAsync(BpmSoftSync.Adapters.Excel.Tests.SyntheticSnapshot.CreateQualification(), output);
    Console.WriteLine($"EMITTED {pair.RunId:D} {pair.PairId:D} {pair.ModelSha256} {pair.LookupSha256} {pair.PairDigest}");
    return 0;
}

try
{
    await BpmSoftSync.Adapters.Excel.Tests.CompareWorkbookReaderTests.SyntheticBestEffortPairBindsStrictLookupColumnIdentityAsync();
    Console.WriteLine("PASS CompareWorkbookReaderTests");
    BpmSoftSync.Adapters.Excel.Tests.WorkbookContractTests.QualifiedSnapshotProjectsExactWorkbookContracts();
    await BpmSoftSync.Adapters.Excel.Tests.WorkbookValidationTests.GeneratedPairIsClosedDeterministicAndBoundOneToOneAsync();
    await BpmSoftSync.Adapters.Excel.Tests.WorkbookValidationTests.FormulaExternalAndVbaTamperingIsRejectedAsync();
    await BpmSoftSync.Adapters.Excel.Tests.WorkbookValidationTests.PublicationFaultsNeverExposePartialPairAsync();
    await BpmSoftSync.Adapters.Excel.Tests.WorkbookValidationTests.NonQualifiedInputsAreRejectedWithoutOutputAsync();
    await BpmSoftSync.Adapters.Excel.Tests.WorkbookValidationTests.ManifestScaleOverflowIsRejectedBeforeWorkbookGenerationAsync();
    Console.WriteLine("PASS Excel S05 tests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}
