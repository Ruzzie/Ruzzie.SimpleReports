using System;
using Ruzzie.Common.Types;

namespace Ruzzie.SimpleReports
{
    public static class RunParamsHelper
    {
        public static Option<string> GetParameterFromRunParams(ReadOnlySpan<(string Name, string Value)> runParams, string parameterName)
        {
            //Todo: check if we can optimize the runParams data structure; maybe a Dictionary...?
            var result = Option.None<string>();

            for (var i = 0; i < runParams.Length; i++)
            {
                (string name, string value) = runParams[i];

                if (name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    result = value;
                    break;
                }
            }

            return result;
        }
    }
}