#nullable enable

using System.Collections.Generic;
using Assets.Scripts.Data;

namespace Assets.Scripts.Loading
{
    /// <summary>
    /// Simple container for the loaded dataset and lookup table.
    /// </summary>
    public sealed class SolarSystemDatabaseRuntime
    {
        public SolarSystemData Data { get; }
        public IReadOnlyDictionary<string, SolarObjectData> ObjectsById { get; }

        public SolarSystemDatabaseRuntime(SolarSystemData _data, Dictionary<string, SolarObjectData> _objectsById)
        {
            Data = _data;
            ObjectsById = _objectsById;
        }
    }
}