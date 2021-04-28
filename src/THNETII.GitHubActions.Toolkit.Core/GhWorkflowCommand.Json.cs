using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace THNETII.GitHubActions.Toolkit.Core
{
    partial class GhWorkflowCommand
    {
        public static string ToCommandValue<T>([MaybeNull] T input,
            JsonSerializerOptions? options = null)
        {
            return input switch
            {
                null => string.Empty,
                string s => s,
                T inst => JsonSerializer.Serialize(inst, options),
            };
        }

        public static GhWorkflowCommand Create<T>(string command,
            IEnumerable<KeyValuePair<string, object?>>? properties, [MaybeNull] T message,
            JsonSerializerOptions? serializerOptions = null)
        {
            var serializedProperties = properties?.Select(kvp =>
                new KeyValuePair<string, string?>(
                    kvp.Key, ToCommandValue(kvp.Value, serializerOptions))
                ).ToList();
            var serializedMessage = ToCommandValue(message, serializerOptions);
            return new GhWorkflowCommand(command, serializedProperties, serializedMessage);
        }
    }
}
