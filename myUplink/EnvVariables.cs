using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUplinkSmartConnect
{
    public class EnvVariables
    {
        public Dictionary<string, string> _machine;
        public EnvVariables()
        {
            _machine = new Dictionary<string, string>();
            GetVariables(ref _machine, EnvironmentVariableTarget.Process);
            GetVariables(ref _machine, EnvironmentVariableTarget.User);
            GetVariables(ref _machine, EnvironmentVariableTarget.Machine);
        }
        public string GetValue(string keyName)
        {
            keyName = keyName.ToLower();
            if (_machine.ContainsKey(keyName))
            {
                Log.Logger.Debug("Environmental variable {KeyName} - {KeyValue}",keyName,_machine[keyName]);
                return _machine[keyName];
            }
            
            Log.Logger.Debug("Failed to find environmental variable {KeyName}",keyName);
            return string.Empty;
        }

        public T GetValueEnum<T>(string keyName, T defaultValue) where T : Enum
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
                return defaultValue;

            try
            {
                return (T)Enum.Parse(typeof(T), strValue);
            }
            catch
            {
                Log.Logger.Error("{KeyName} has invalid value and cannot be read as a {Type}: {KeyValue}",keyName,typeof(T),strValue);
                return defaultValue;
            }            
        }

        public int GetValueInt(string keyName)
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
                return 0;

            try
            {
                return int.Parse(strValue);                
            }
            catch
            {
                Log.Logger.Error("{KeyName} has invalid value and cannot be read as a int: {Value}",keyName,strValue);
                return 0;
            }
        }

        void GetVariables(ref Dictionary<string, string> dic, EnvironmentVariableTarget target)
        {
            var envs = Environment.GetEnvironmentVariables(target);
            foreach (System.Collections.DictionaryEntry item in envs)
            {
                var keyName = item.Key?.ToString()?.ToLower();
                if (string.IsNullOrEmpty(keyName) || dic.ContainsKey(keyName))
                    continue;

                string value = item.Value?.ToString() ?? "";
                if(!string.IsNullOrEmpty(value))
                    dic.Add(keyName, value);
            }
        }
    }
}
