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

        public EnvVariables(Dictionary<string,object> tmpList)
        {
            _machine = new Dictionary<string, string>();

            foreach(var item in tmpList)
            {
                var value = tmpList[item.Key]?.ToString() ?? "";

                if(value == null)
                {
                    _machine.Add(item.Key.ToLower(), "");
                }
                else
                {
                    _machine.Add(item.Key.ToLower(), value);
                }                
            }
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

        public TEnum GetValueEnum<TEnum>(TEnum defaultValue, params string[] keyName) where TEnum : struct
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                Log.Logger.Debug("Environmental variable {KeyName} using default value of {defaultValue}", keyName, defaultValue);
                return defaultValue;
            }

            if (Enum.TryParse<TEnum>(strValue, true,out TEnum result))
            {
                return result;
            }

            Log.Logger.Error("{KeyName} has invalid value and cannot be read as a {Type}: {KeyValue}", keyName, typeof(TEnum), strValue);
            return defaultValue;
        }

        public string GetValue(params string[] keyName)
        {
            for(int i=0;i<keyName.Length;i++)
            {
                var tmpKey = keyName[i].ToLowerInvariant();

                if (_machine.ContainsKey(tmpKey))
                    return GetValue(tmpKey);
            }

            Log.Logger.Debug("Environmental variable {KeyName} using default value of {defaultValue}", keyName);
            return string.Empty;
        }

        public int GetValueInt(string keyName, int defaultValue = 0)
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                Log.Logger.Debug("Environmental variable {KeyName} using default value of {defaultValue}", keyName, defaultValue);
                return defaultValue;
            }

            if (int.TryParse(strValue, out int result))
            {
                return result;
            }

            Log.Logger.Error("{KeyName} has invalid value and cannot be read as a int: {Value}", keyName, strValue);
            return defaultValue;
        }

        public int GetValueInt(int defaultValue = 0,params string[] keyName)
        {
            for (int i = 0; i < keyName.Length; i++)
            {
                var tmpKey = keyName[i].ToLowerInvariant();

                if (_machine.ContainsKey(tmpKey))
                    return GetValueInt(tmpKey, defaultValue);
            }

            Log.Logger.Debug("Environmental variable {KeyName} using default value of {defaultValue}", keyName);
            return defaultValue;
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
