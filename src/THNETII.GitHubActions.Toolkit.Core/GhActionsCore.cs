using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace THNETII.GitHubActions.Toolkit.Core
{
    public static class GhActionsCore
    {
        #region Variables
        /// <summary>
        /// Sets the environment variable for this action and future actions in the job
        /// </summary>
        /// <param name="name">the name of the variable to set</param>
        /// <param name="val">the value of the variable. Non-string values will be converted to a JSON-string</param>
        public static void ExportVariable<T>(string name, [MaybeNull] T val,
            JsonSerializerOptions? serializerOptions = null)
        {
            var convertedVal = GhWorkflowCommand.ToCommandValue(val, serializerOptions);
            Environment.SetEnvironmentVariable(name, convertedVal);
            IssueCommand(
                GhWorkflowCommand.SetEnvironmentVariable(name, convertedVal));
        }

        /// <summary>
        /// Registers a secret which will get masked from logs
        /// </summary>
        /// <param name="secret">value of the secret</param>
        public static void SetSecret(string secret) =>
            IssueCommand(GhWorkflowCommand.MaskValueInLog(secret));

        /// <summary>
        /// Gets the value of an input. The value is also trimmed.
        /// </summary>
        /// <param name="name">name of the input to get</param>
        /// <param name="required">Optional. Whether the input is required. If required and not present, will throw. Defaults to false</param>
        public static string? GetInput(string name, bool required = false)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            var envName = "INPUT_" + name.Replace(' ', '_').ToUpperInvariant();
            var val = Environment.GetEnvironmentVariable(envName);
            if (required && string.IsNullOrEmpty(val))
                throw new InvalidOperationException($"Input required and not supplied: {name}");
            return val.Trim();
        }

        /// <summary>
        /// Enables or disables the echoing of commands into stdout for the rest of the step.
        /// Echoing is disabled by default if <c>ACTIONS_STEP_DEBUG</c> is not set.
        /// </summary>
        [SuppressMessage("Globalization", "CA1303: Do not pass literals as localized parameters")]
        public static void SetCommandEcho(bool enabled) =>
            IssueCommand(new GhWorkflowCommand("echo", enabled ? "on" : "off"));
        #endregion

        #region Results
        /// <summary>
        /// Sets the action status to failed.
        /// When the action exits it will be with an exit code of 1
        /// </summary>
        /// <param name="message">add error issue message</param>
        public static void SetFailed(string message)
        {
            Environment.ExitCode = 1;
            Error(message);
        }

        /// <inheritdoc cref="SetFailed(string)"/>
        /// <param name="except">serialize exception into error issue message</param>
        public static void SetFailed(Exception except)
        {
            Environment.ExitCode = 1;
            Error(except);
        }
        #endregion

        #region Logging Commands
        /// <summary>
        /// Gets whether Actions Step Debug is on or not
        /// </summary>
        public static bool IsDebug()
        {
            var runnerDebug = Environment.GetEnvironmentVariable("RUNNER_DEBUG");
            if (int.TryParse(runnerDebug, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                return intValue == 1;
            return false;
        }

        /// <summary>
        /// Writes debug message to user log
        /// </summary>
        /// <param name="message">debug message</param>
        public static void Debug(string message) =>
            IssueCommand(GhWorkflowCommand.DebugMessage(message));

        /// <summary>
        /// Adds an error issue
        /// </summary>
        /// <param name="message">error issue message</param>
        public static void Error(string message) =>
            IssueCommand(GhWorkflowCommand.ErrorMessage(message));

        /// <inheritdoc cref="Error(string)"/>
        public static void Error(Exception exception,
            JsonSerializerOptions? serializerOptions = null)
        {
            var message = GhWorkflowCommand.ToCommandValue(exception,
                serializerOptions);
            IssueCommand(GhWorkflowCommand.ErrorMessage(message));
        }

        /// <summary>
        /// Adds an warning issue
        /// </summary>
        /// <param name="message">warning issue message</param>
        public static void Warning(string message) =>
            IssueCommand(GhWorkflowCommand.WarningMessage(message));

        /// <inheritdoc cref="Warning(string)"/>
        public static void Warning(Exception exception,
            JsonSerializerOptions? serializerOptions = null)
        {
            var message = GhWorkflowCommand.ToCommandValue(exception,
                serializerOptions);
            IssueCommand(GhWorkflowCommand.WarningMessage(message));
        }

        /// <summary>
        /// Writes info to log with <see cref="Console.WriteLine(string)"/>.
        /// </summary>
        /// <param name="message">info message</param>
        public static void Info(string message) =>
            Console.WriteLine(message);

        /// <summary>
        /// Begin an output group.
        /// <para>Output until the next `groupEnd` will be foldable in this group</para>
        /// </summary>
        /// <param name="name">The name of the output group</param>
        public static void StartGroup(string name) =>
            IssueCommand(new GhWorkflowCommand(
                GhWorkflowCommandName.StartGroup, name));

        /// <summary>
        /// End an output group.
        /// </summary>
        public static void EndGroup() =>
            IssueCommand(new GhWorkflowCommand(
                GhWorkflowCommandName.EndGroup));

        /// <summary>
        /// Constructs an <see cref="IDisposable"/> instance that can be used in a using block.
        /// </summary>
        /// <param name="name">The name of the output group</param>
        public static IDisposable Group(string name)
        {
            StartGroup(name);
            return new GhDeferredCommandIssueDisposable(
                new GhWorkflowCommand(GhWorkflowCommandName.EndGroup, name));
        }
        #endregion

        #region Wrapper action state
        /// <summary>
        /// Saves state for current action, the state can only be retrieved by this action's post job execution.
        /// </summary>
        /// <param name="name">name of the state to store</param>
        /// <param name="value">value to store. Non-string values will be converted to a JSON-string</param>
        public static void SaveState<T>(string name, [MaybeNull] T value,
            JsonSerializerOptions? serializerOptions = null) =>
            IssueCommand(GhWorkflowCommand.Create(
                GhWorkflowCommandName.SaveState, message: value, properties:
                new[] { new KeyValuePair<string, object?>("name", name) },
                serializerOptions: serializerOptions));

        /// <summary>
        /// Gets the value of an state set by this action's main execution.
        /// </summary>
        /// <param name="name">name of the state to get</param>
        public static string? GetState<T>(string name) =>
            Environment.GetEnvironmentVariable("STATE_" + name);
        #endregion

        #region Stopp Workflow commands
        /// <summary>
        /// Stops processing any workflow commands. This special command allows you to
        /// log anything without accidentally running a workflow command. For example, you
        /// could stop logging to output an entire script that has comments.
        /// </summary>
        /// <param name="resumeCommands">
        /// On return, receives a workflow command that can be issued to resume
        /// processing of workflow commands.
        /// </param>
        public static void StopCommands(out GhWorkflowCommand resumeCommands) =>
            StopCommands(Guid.NewGuid().ToString("N"), out resumeCommands);

        /// <inheritdoc cref="StopCommands(out GhWorkflowCommand)"/>
        public static void StopCommands(string endtoken,
            out GhWorkflowCommand resumeCommands)
        {
            if (string.IsNullOrEmpty(endtoken))
                throw endtoken is null
                    ? new ArgumentNullException(nameof(endtoken))
                    : new ArgumentException(message: null, nameof(endtoken));

            IssueCommand(new GhWorkflowCommand(
                GhWorkflowCommandName.StopProcessing, endtoken));
            resumeCommands = new GhWorkflowCommand(endtoken);
        }

        /// <inheritdoc cref="StopCommands(out GhWorkflowCommand)"/>
        public static IDisposable SuppressCommandProcessing() =>
            SuppressCommandProcessing(Guid.NewGuid().ToString("N"));

        /// <inheritdoc cref="SuppressCommandProcessing()"/>
        public static IDisposable SuppressCommandProcessing(string endtoken)
        {
            StopCommands(endtoken, out var resumeCommands);
            return new GhDeferredCommandIssueDisposable(resumeCommands);
        }
        #endregion

        public static void IssueCommand(GhWorkflowCommand command) =>
            command?.Issue();
    }
}
