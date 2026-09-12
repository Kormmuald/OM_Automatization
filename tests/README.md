# Offline test rules

All tests are BCL-only and use sanitized fixtures plus fake or capture-only transport. Tests must never contact a target, prompt for credentials, persist a password/cookie/CSRF/raw lookup value, or reference Excel, browser, Git, or external BPMSoft client libraries.
