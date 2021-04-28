using System;

namespace THNETII.GitHubActions.Toolkit.Core
{
    internal class GhDeferredCommandIssueDisposable : IDisposable
    {
        private GhWorkflowCommand? issueOnDispose;

        public GhDeferredCommandIssueDisposable(GhWorkflowCommand issueOnDispose)
        {
            this.issueOnDispose = issueOnDispose;
        }

        public void Dispose()
        {
            GhWorkflowCommand? command;
            (command, issueOnDispose) = (issueOnDispose, null);
            command?.Issue();
        }
    }
}
