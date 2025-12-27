#nullable enable

using System.Collections.Generic;

namespace Assets.Scripts.Loading
{
    /// <summary>
    /// Collects warnings and errors during loading.
    /// </summary>
    public sealed class SolarSystemLoadReport
    {
        public readonly List<string> Errors = new(32);
        public readonly List<string> Warnings = new(64);

        public bool HasErrors => Errors.Count > 0;

        public void AddError(string _message)
        {
            Errors.Add(_message);
        }

        public void AddWarning(string _message)
        {
            Warnings.Add(_message);
        }
    }
}