try
{
    await BpmSoftSync.Adapters.BpmSoft.Tests.ReadEndpointAllowlistTests.ExactMatrixRejectsBeforeSendAsync();
    Console.WriteLine("PASS ReadEndpointAllowlistTests.ExactMatrixRejectsBeforeSendAsync");
    BpmSoftSync.Adapters.BpmSoft.Tests.SessionTests.EphemeralStateIsClearedOnDispose();
    Console.WriteLine("PASS SessionTests.EphemeralStateIsClearedOnDispose");
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.UnknownShapeUsesOnlyStructuralEnvelope();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.MissingUnknownShapeEnvelopeFailsClosedWithNamedBlocker();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.IndexesUseOnlyColumnUIdAndRemainReadOnly();
    BpmSoftSync.Adapters.BpmSoft.Tests.UnknownShapeTests.WorkspaceFixtureHasCompleteTypedStatusCoverage();
    Console.WriteLine("PASS UnknownShapeTests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
