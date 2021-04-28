using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using static THNETII.GitHubActions.Toolkit.Core.GhWorkflowCommandName;

namespace THNETII.GitHubActions.Toolkit.Core
{
    public partial class GhWorkflowCommand
    {
        /// <summary>
        /// Creates or updates an environment variable for any actions running next in a job.
        /// The action that creates or updates the environment variable does not have access
        /// to the new value, but all subsequent actions in a job will have access. Environment
        /// variables are case-sensitive and you can include punctuation.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#setting-an-environment-variable"/>
        public static GhWorkflowCommand SetEnvironmentVariable(string name,
            string? value)
        {
            return new GhWorkflowCommand(ExportVariable,
                properties: new[] { new KeyValuePair<string, string?>("name", name) },
                escapedProperties: "name=" + EscapeProperty(name),
                message: EscapeData(value));
        }

        /// <summary>
        /// Sets an action's output parameter.
        /// <para>Optionally, you can also declare output parameters in an action's metadata file. For more information, see <a href="https://docs.github.com/en/articles/metadata-syntax-for-github-actions#outputs">Metadata syntax for GitHub Actions</a>.</para>
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#setting-an-output-parameter"/>
        public static GhWorkflowCommand SetOutputParameter(string name,
            string? value)
        {
            return new GhWorkflowCommand(SetOutput,
                properties: new[] { new KeyValuePair<string, string?>("name", name) },
                escapedProperties: "name=" + EscapeProperty(name),
                message: value);
        }

        /// <summary>
        /// Prepends a directory to the system <c>PATH</c> variable for all subsequent actions in the current job. The currently running action cannot access the new path variable.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#adding-a-system-path"/>
        public static GhWorkflowCommand AddSystemPath(string path) =>
            new GhWorkflowCommand(AddPath, path);

        /// <summary>
        /// Prints a debug message to the log. You must create a secret named <c>ACTIONS_STEP_DEBUG</c> with the value <c>true</c> to see the debug messages set by this command in the log. For more information, see <a href="https://docs.github.com/en/actions/configuring-and-managing-workflows/managing-a-workflow-run#enabling-debug-logging">Managing a workflow run</a>.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#setting-a-debug-message"/>
        public static GhWorkflowCommand DebugMessage(string message) =>
            new GhWorkflowCommand(GhWorkflowCommandName.Debug, message);

        /// <summary>
        /// Creates a warning message and prints the message to the log. You can optionally provide a filename (<paramref name="file"/>), line number (<paramref name="line"/>), and column (<paramref name="col"/>) number where the warning occurred.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#setting-a-warning-message"/>
        public static GhWorkflowCommand WarningMessage(string message, string? file = null,
            int? line = null, int? col = null)
        {
            var escapedProperties = EscapeFileLineColumnProperties(
                file, line, col, out var properties);
            return new GhWorkflowCommand(Warning, properties, escapedProperties,
                message);
        }

        /// <summary>
        /// Creates an error message and prints the message to the log. You can optionally provide a filename (<paramref name="file"/>), line number (<paramref name="line"/>), and column (<paramref name="col"/>) number where the error occurred.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#setting-an-error-message"/>
        public static GhWorkflowCommand ErrorMessage(string message, string? file = null,
            int? line = null, int? col = null)
        {
            var escapedProperties = EscapeFileLineColumnProperties(
                file, line, col, out var properties);
            return new GhWorkflowCommand(Error, properties, escapedProperties,
                message);
        }

        private static string EscapeFileLineColumnProperties(
            string? file, int? line, int? col,
            out List<KeyValuePair<string, string?>> properties)
        {
            properties = new List<KeyValuePair<string, string?>>(capacity: 3);
            if (!(string.IsNullOrEmpty(file)))
                properties.Add(new KeyValuePair<string, string?>("file", file));
            if (line.HasValue)
                properties.Add(new KeyValuePair<string, string?>("line",
                    line.Value.ToString(CultureInfo.InvariantCulture)));
            if (col.HasValue)
                properties.Add(new KeyValuePair<string, string?>("col",
                    col.Value.ToString(CultureInfo.InvariantCulture)));
            return EscapePropertyEnumerable(properties)!;
        }

        /// <summary>
        /// Masking a value prevents a string or variable from being printed in the log. Each
        /// masked word separated by whitespace is replaced with the * character.
        /// </summary>
        /// <seealso href="https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#masking-a-value-in-log"/>
        public static GhWorkflowCommand MaskValueInLog(string value) =>
            new GhWorkflowCommand(SetSecret, value);

        internal const string CMD_STRING = "::";

        private readonly string escapedCommand;
        private readonly string? escapedProperties;
        private readonly string? escapedMessage;

        [DebuggerStepThrough]
        public GhWorkflowCommand(string? command,
            string? message = null) : this(command, null, null, message) { }

        [DebuggerStepThrough]
        public GhWorkflowCommand(string? command,
            IEnumerable<KeyValuePair<string, string?>>? properties,
            string? message) : this(command, properties, null, message) { }

        private GhWorkflowCommand(string? command,
            IEnumerable<KeyValuePair<string, string?>>? properties,
            string? escapedProperties,
            string? message) : base()
        {
            if (string.IsNullOrEmpty(command))
            {
                command = "missing.command";
            }

            escapedCommand = EscapeProperty(command);
            this.escapedProperties = escapedProperties
                ?? EscapePropertyEnumerable(properties);
            escapedMessage = EscapeData(message);

            Command = command!;
            Properties = properties
                ?? Enumerable.Empty<KeyValuePair<string, string?>>();
            Message = message ?? string.Empty;
        }

        public string Command { get; }
        public IEnumerable<KeyValuePair<string, string?>> Properties { get; }
        public string Message { get; }

        public override string ToString()
        {
            StringBuilder cmdStr = new StringBuilder();
            cmdStr.Append(CMD_STRING).Append(escapedCommand);
            if (string.IsNullOrEmpty(escapedProperties))
            {
                cmdStr.Append(' ');
                cmdStr.Append(escapedProperties);
            }
            cmdStr.Append(CMD_STRING).Append(escapedMessage);
            return cmdStr.ToString();
        }

        public void Issue() => Console.WriteLine(ToString());

        private static string? EscapePropertyEnumerable(
            IEnumerable<KeyValuePair<string, string?>>? properties)
        {
            if (properties is null)
                return null;
            return string.Join(",", properties.Select(escapePropertyKeyValuePair));
        }

        private static readonly Func<KeyValuePair<string, string?>, string>
            escapePropertyKeyValuePair = EscapePropertyPair;

        private static string EscapePropertyPair(KeyValuePair<string, string?> kvp)
        {
            var (key, val) = (kvp.Key, kvp.Value);
            if (val is null || (val is string valStr && string.IsNullOrEmpty(valStr)))
                return string.Empty;
            return key + "=" + EscapeProperty(val);
        }

        private static string EscapeData(string? s) =>
            (s ?? string.Empty)
            .ReplaceOrdinal("%", "%25")
            .ReplaceOrdinal("\r", "%0D")
            .ReplaceOrdinal("\n", "%0A")
            ;

        private static string EscapeProperty(string? s) =>
            (s ?? string.Empty)
            .ReplaceOrdinal("%", "%25")
            .ReplaceOrdinal("\r", "%0D")
            .ReplaceOrdinal("\n", "%0A")
            .ReplaceOrdinal(":", "%3A")
            .ReplaceOrdinal(",", "%2C")
            ;
    }

    internal static class StringReplaceExtensions
    {
        internal static string ReplaceOrdinal(this string str, string oldString, string newString)
        {
            return str.Replace(oldString, newString
#if NETSTANDARD2_1
                , StringComparison.Ordinal
#endif
                );
        }
    }
}
