# Offline operator runbook

1. Build `BpmSoftSync.sln`.
2. Run `catalog validate-offline --fixture <sanitized-fixture>`.
3. Review `HUMAN_REVIEW_REQUIRED` as an offline evidence decision point only.
4. After successful offline checks, the operator may manually start `catalog qualify` for one exact target and declared read scope. An agent may act only on a direct current user request in an available chat. No `AuthorizationReference` is required; credentials are requested only in the terminal prompt of that manual/directly requested invocation.

This runbook never permits Write/Manage, Excel, compare, Apply, browser actions, or Git actions.
