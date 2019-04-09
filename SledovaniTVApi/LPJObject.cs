using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVAPI
{
    /// <summary>
    /// Live Player JObject
    /// </summary>
    public class LPJObject
    {
        private JObject _jObject;

        public LPJObject(string jsonString)
        {
            _jObject = JObject.Parse(jsonString);
        }

        public string GetStringValue(string key)
        {
            return _jObject[key].ToString();
        }

        public JToken GetValue(string key)
        {
            return _jObject[key];
        }

        public bool HasValue(string key)
        {
            // ContainsKey method of JObject is not implemented in Xamarine Live Player!
            // Error in Xamarine Live Player:  'JObject' does not contain a definition for 'ContainsKey' and no extension method 'ContainsKey' accepting a first argument of type 'JObject' could be found(are you missing a using directive or an assembly reference ?)		Z:\SledovaniTVPlayer\SledovaniTVApi\ParsableJObject.cs  1

            JToken value;
            return _jObject.TryGetValue(key, StringComparison.CurrentCulture, out value);
        }
    }
}
