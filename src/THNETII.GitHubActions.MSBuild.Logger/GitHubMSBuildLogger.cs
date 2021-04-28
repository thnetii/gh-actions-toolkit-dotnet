using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Microsoft.Build.Framework;

using THNETII.GitHubActions.Toolkit.Core;

namespace THNETII.GitHubActions.MSBuild.Logger
{
    public class GitHubMSBuildLogger : Microsoft.Build.Utilities.Logger
    {
        private Uri? solutionDirectoryUri;

        public GitHubMSBuildLogger() : base() { }

        public override void Initialize(IEventSource eventSource)
        {
            _ = eventSource ?? throw new ArgumentNullException(nameof(eventSource));

            LoggerParameters loggerParameters = LoggerParameters.Parse(Parameters);
            if (loggerParameters[nameof(Verbosity)] is { Length: > 0 } verbosityParam &&
                Enum.TryParse(verbosityParam, out LoggerVerbosity verbosity))
                Verbosity = verbosity;

            solutionDirectoryUri = loggerParameters["SolutionDir"] switch
            {
                null => null,
                string emptyString when string.IsNullOrEmpty(emptyString) =>
                    null,
                string solutionDirParam => new UriBuilder
                {
                    Scheme = "file",
                    Path = Path.GetFullPath(Path.Combine(solutionDirParam, ".")) + Path.DirectorySeparatorChar
                }.Uri,
            };

            eventSource.WarningRaised += OnWarningRaised;
            eventSource.ErrorRaised += OnErrorRaised;
            //eventSource.MessageRaised += OnMessageRaised;
        }

        private void OnWarningRaised(object sender, BuildWarningEventArgs e)
        {
            string? file = GetFilePathRelativeToSolutionDir(e.File);
            var (lineNumber, columnNumber) = GetLineColumnPosition(e.LineNumber, e.ColumnNumber);
            string message = FormatWarningEvent(e);

            var loggingCommand = GhWorkflowCommand.WarningMessage(
                message, file, lineNumber, columnNumber);
            GhActionsCore.IssueCommand(loggingCommand);
        }

        private void OnErrorRaised(object sender, BuildErrorEventArgs e)
        {
            string? file = GetFilePathRelativeToSolutionDir(e.File);
            var (lineNumber, columnNumber) = GetLineColumnPosition(e.LineNumber, e.ColumnNumber);
            string message = FormatErrorEvent(e);

            var loggingCommand = GhWorkflowCommand.WarningMessage(
                message, file, lineNumber, columnNumber);
            GhActionsCore.IssueCommand(loggingCommand);
        }

        private void OnMessageRaised(object sender, BuildMessageEventArgs e)
        {
            switch (e.Importance)
            {
                case MessageImportance.High
                when (int)Verbosity < (int)LoggerVerbosity.Minimal:
                case MessageImportance.Normal
                when (int)Verbosity < (int)LoggerVerbosity.Detailed:
                case MessageImportance.Low
                when (int)Verbosity < (int)LoggerVerbosity.Diagnostic:
                    return;
            }

            Dictionary<string, string?> properties = new(capacity: 15, StringComparer.OrdinalIgnoreCase);
            if (e.Code is { Length: > 0 } code)
                properties[nameof(code)] = code;
            if (e.ColumnNumber >= 0)
                properties["col"] = e.ColumnNumber.ToString(CultureInfo.InvariantCulture);
            if (e.File is { Length: > 0 } file)
                properties[nameof(file)] = file;
            if (e.LineNumber > 0)
                properties["line"] = e.LineNumber.ToString(CultureInfo.InvariantCulture);

            GhActionsCore.IssueCommand(new GhWorkflowCommand("info", properties, e.Message));
        }

        private string? GetFilePathRelativeToSolutionDir(string? path)
        {
            switch (path)
            {
                case null:
                case string emptyString when string.IsNullOrEmpty(emptyString):
                    return null;
                case string relPath when !Path.IsPathRooted(relPath):
                    path = Path.GetFullPath(relPath);
                    break;
            }

            if (solutionDirectoryUri is null)
                return path;

            var pathUri = new UriBuilder { Scheme = "file", Path = path }.Uri;
            var pathRelUri = solutionDirectoryUri.MakeRelativeUri(pathUri);
            return pathRelUri.ToString();
        }

        private static (int? LineNumber, int? ColumnNumber)
            GetLineColumnPosition(int lineNumber, int columnNumber)
        {
            int? lineNumberResult = lineNumber > 0 ? lineNumber : null;
            int? columnNumberResult = columnNumber >= 0 ? columnNumber : null;
            return (lineNumberResult, columnNumberResult);
        }
    }
}
