try
{
    await BpmSoftSync.Cli.Tests.CatalogValidateOfflineTests.FixtureOnlyCommandProducesSafeResultAsync();
    await BpmSoftSync.Cli.Tests.CatalogValidateOfflineTests.MainDispatchesTheSharedFixtureWorkflowAsync();
    Console.WriteLine("PASS CatalogValidateOfflineTests.FixtureOnlyCommandProducesSafeResultAsync");
    await BpmSoftSync.Cli.Tests.AuthorizationGateTests.MissingAuthorizationNeverPromptsOrSendsAsync();
    Console.WriteLine("PASS AuthorizationGateTests.MissingAuthorizationNeverPromptsOrSendsAsync");
    await BpmSoftSync.Cli.Tests.AuthorizationGateTests.OnlyManualInvocationMayReachTerminalPromptAsync();
    await BpmSoftSync.Cli.Tests.CliContractTests.OutputContainsOnlySafeDiagnosticFieldsAsync();
    await BpmSoftSync.Cli.Tests.CliContractTests.DiagnoseReadsOnlyValidatedBoundedRecordsAsync();
    BpmSoftSync.Cli.Tests.ArchitectureTests.DomainAndApplicationDoNotDependOnForbiddenAdapters();
    BpmSoftSync.Cli.Tests.ArchitectureTests.ExactlyOneHttpSendBoundaryAndNoGetPackagesAllowance();
    Console.WriteLine("PASS ArchitectureTests.ExactlyOneHttpSendBoundaryAndNoGetPackagesAllowance");
    BpmSoftSync.Cli.Tests.HandoffPackageTests.HandoffIsFixtureOnlyAndSecretFree();
    await BpmSoftSync.Cli.Tests.ReadOnlyQualificationE2ETests.FakeTwoPassRunReachesHumanReviewAsync();
    await BpmSoftSync.Cli.Tests.ProductionWorkflowE2ETests.FullCompositionPublishesExactAcceptedPairAsync();
    await BpmSoftSync.Cli.Tests.ProductionWorkflowE2ETests.LiveAdmissionRequiresBothFlagsAndSafeRootAsync();
    await BpmSoftSync.Cli.Tests.ProgramMainOfflineCompositionTests.ProductionMainPublishesFixturePairWithoutLiveAdmissionAsync();
    await BpmSoftSync.Cli.Tests.ProgramMainOfflineCompositionTests.ProductionMainRejectsWorkspaceAndNestedRootsBeforeLivePromptAsync();
    await BpmSoftSync.Cli.Tests.ProgramMainOfflineCompositionTests.ProductionMainRejectsBareTempRootBeforeRunnerPromptOrHttpAsync();
    BpmSoftSync.Cli.Tests.ReadOnlyQualificationSecurityRegressionTests.TargetMutationIsTerminalWithoutRetry();
    await BpmSoftSync.Cli.Tests.WorkflowAdversarialTests.PagingFailureIsTerminalAfterExactlyTwoReadsAsync();
    await BpmSoftSync.Cli.Tests.WorkflowAdversarialTests.TargetMutationHasNoRetryOrThirdPassAsync();
    Console.WriteLine("PASS S06 CLI, full workflow E2E and security regression tests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
