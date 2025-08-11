using System;
using Unity.Plastic.Newtonsoft.Json;

namespace Database.Runtime
{
    [Serializable]
    public class SerializableFact
    {
        // stock values as strings from our Json file
        public string Value;
        public string ValueType;
        public bool IsPersistent;

    }
}