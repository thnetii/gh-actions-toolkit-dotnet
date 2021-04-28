// MSBuild.Logger.LoggerParameters
using System;
using System.Collections.Generic;

namespace THNETII.GitHubActions.MSBuild.Logger
{
    internal sealed class LoggerParameters
    {
        private const char nameValueDelimiter = '=';

        private const char nameValuePairDelimiter = '|';

        private readonly IDictionary<string, string> m_parameters;

        public string this[string name]
        {
            get
            {
                if (m_parameters.TryGetValue(name, out var value))
                {
                    return value;
                }
                return string.Empty;
            }
        }

        public LoggerParameters(IDictionary<string, string> parameters)
        {
            m_parameters = parameters;
        }

        public static LoggerParameters Parse(string? paramString)
        {
            if (string.IsNullOrEmpty(paramString))
            {
                return new LoggerParameters(new Dictionary<string, string>());
            }
            string[] array = paramString!.Split(nameValuePairDelimiter);
            Dictionary<string, string> dictionary = new(StringComparer.OrdinalIgnoreCase);
            string[] array2 = array;
            foreach (string text in array2)
            {
                int delimIdx = text.IndexOf(nameValueDelimiter);
                if (delimIdx >= 0)
                {
                    string key = text.Substring(0, delimIdx);
                    string value = text.Substring(delimIdx + 1);
                    dictionary.Add(key.Trim(), value.Trim());
                }
            }
            return new LoggerParameters(dictionary);
        }
    }

}
