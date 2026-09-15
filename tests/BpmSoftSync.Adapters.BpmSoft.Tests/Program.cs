try
{
    await BpmSoftSync.Adapters.BpmSoft.Tests.ReadEndpointAllowlistTests.ExactMatrixRejectsBeforeSendAsync();
    Console.WriteLine("PASS ReadEndpointAllowlistTests.ExactMatrixRejectsBeforeSendAsync");
    BpmSoftSync.Adapters.BpmSoft.Tests.ReadEndpointAllowlistTests.TargetPolicyAllowsExplicitHttpsAndLoopbackOnly();
    Console.WriteLine("PASS ReadEndpointAllowlistTests.TargetPolicyAllowsExplicitHttpsAndLoopbackOnly");
    await BpmSoftSync.Adapters.BpmSoft.Tests.ReadEndpointAllowlistTests.RedirectAndAlternateResponseOriginAreRejectedAsync();
    Console.WriteLine("PASS ReadEndpointAllowlistTests.RedirectAndAlternateResponseOriginAreRejectedAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.LoginSuccessCreatesCookieAndCsrfSessionAsync();
    Console.WriteLine("PASS SessionTests.LoginSuccessCreatesCookieAndCsrfSessionAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.LoginFailureAndMissingCsrfFailClosedWithoutSecretLeakAsync();
    Console.WriteLine("PASS SessionTests.LoginFailureAndMissingCsrfFailClosedWithoutSecretLeakAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.ParseHttpAndEnvelopeFailuresAreSafelyTypedAsync();
    Console.WriteLine("PASS SessionTests.ParseHttpAndEnvelopeFailuresAreSafelyTypedAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.MissingSessionAndFailedReloginCannotReuseStateAsync();
    Console.WriteLine("PASS SessionTests.MissingSessionAndFailedReloginCannotReuseStateAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.BoundedReadTimeoutCancellationAndDisposalFailClosedAsync();
    Console.WriteLine("PASS SessionTests.BoundedReadTimeoutCancellationAndDisposalFailClosedAsync");
    await BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.DelayedResponseStreamTimeoutIsTypedAsync();
    Console.WriteLine("PASS SessionTests.DelayedResponseStreamTimeoutIsTypedAsync");
    BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.MaximumResponseBoundRejectsOverflowBeforeSend();
    Console.WriteLine("PASS SessionTests.MaximumResponseBoundRejectsOverflowBeforeSend");
    BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.TerminalPromptIsInteractiveOnlyAndDoesNotEchoOrSerializeSecrets();
    Console.WriteLine("PASS SessionTests");
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.UnknownShapeUsesOnlyStructuralEnvelope();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.MissingUnknownShapeEnvelopeFailsClosedWithNamedBlocker();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.IndexesUseOnlyColumnUIdAndRemainReadOnly();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.WorkspaceFixtureHasCompleteTypedStatusCoverage();
    Console.WriteLine("PASS UnknownShapeTests");
    await BpmSoftSync.Adapters.BpmSoft.Tests.WorkspaceInventoryAdapterTests.FullTraversalUsesAcceptedTransportAndPreservesObjectModelAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.WorkspaceInventoryAdapterTests.MalformedSchemaFailsClosedWithScopedBlockerAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.WorkspaceInventoryAdapterTests.SchemaIdentityMismatchFailsClosedBeforeAnotherTraversalAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.WorkspaceInventoryAdapterTests.SchemaTransportFailureMarksRequestedItemUnreadableAndReturnsSafeBlockerAsync();
    Console.WriteLine("PASS WorkspaceInventoryAdapterTests");
    await BpmSoftSync.Adapters.BpmSoft.Tests.LookupCatalogSourceTests.FullRegistryAndEveryDiscoveredCollectionAreLosslessAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.LookupCatalogSourceTests.EveryPagingAndLimitFailureIsTerminalAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.LookupCatalogSourceTests.UnsupportedAndMalformedValuesFailClosedWithoutRawValueLeakAsync();
    await BpmSoftSync.Adapters.BpmSoft.Tests.LookupCatalogSourceTests.TimeoutAndCancellationRemainBoundedAndSafeAsync();
    Console.WriteLine("PASS LookupCatalogSourceTests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
