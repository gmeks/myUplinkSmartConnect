﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xElectricityPriceApiShared
{
    public class EnvVariables
    {
        readonly Dictionary<string, string> _machine;

        public EnvVariables()
        {
            _machine = new Dictionary<string, string>();
            GetVariables(EnvironmentVariableTarget.Process);
            GetVariables(EnvironmentVariableTarget.User);
            GetVariables(EnvironmentVariableTarget.Machine);
        }

        public bool HasSetting(string keyName)
        {
            keyName = keyName.ToLowerInvariant();
            if (_machine.ContainsKey(keyName))
            {
                return true;
            }

            return false;
        }

        public EnvVariables(ILogger<object> logger,Dictionary<string,object> tmpList)
        {
            _machine = new Dictionary<string, string>();

            foreach(var item in tmpList)
            {
                string? value = tmpList[item.Key]?.ToString();
                AddSettingsValue(item.Key, value);
            }
        }

        public string GetValue(string keyName,string defaultValue = "")
        {
            keyName = keyName.ToLowerInvariant();
            if (_machine.ContainsKey(keyName))
            {
                Console.WriteLine("Environmental variable {KeyName} - {KeyValue}",keyName,_machine[keyName]);
                return _machine[keyName];
            }

            Console.WriteLine("Failed to find environmental variable {KeyName}",keyName);
            return defaultValue;
        }

        public TEnum GetValueEnum<TEnum>(TEnum defaultValue, params string[] keyName) where TEnum : struct
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                Console.WriteLine("Environmental variable {KeyName} using default value of {defaultValue}", keyName, defaultValue);
                return defaultValue;
            }

            if (Enum.TryParse<TEnum>(strValue, true,out TEnum result))
            {
                return result;
            }

            Console.WriteLine("{KeyName} has invalid value and cannot be read as a {Type}: {KeyValue}", keyName, typeof(TEnum), strValue);
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

            Console.WriteLine("Environmental variable {KeyName}, failed to find value returning empty", keyName);
            return string.Empty;
        }

        public bool GetValueBool(string keyName, bool defaultValue = false)
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                Console.WriteLine("Environmental variable {KeyName} using default value of {defaultValue}", keyName, defaultValue);
                return defaultValue;
            }

            if (bool.TryParse(strValue, out bool result))
            {
                return result;
            }
                
            Console.WriteLine("{KeyName} has invalid value and cannot be read as a bool: {Value}", keyName, strValue);
            return defaultValue;
        }

        public int GetValueInt(string keyName, int defaultValue = 0)
        {
            var strValue = GetValue(keyName);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                Console.WriteLine("Environmental variable {KeyName} using default value of {defaultValue}", keyName, defaultValue);
                return defaultValue;
            }

            if (int.TryParse(strValue, out int result))
            {
                return result;
            }

            Console.WriteLine("{KeyName} has invalid value and cannot be read as a int: {Value}", keyName, strValue);
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

            Console.WriteLine("Environmental variable {KeyName} using default value of {defaultValue}", keyName);
            return defaultValue;
        }

        void GetVariables(EnvironmentVariableTarget target)
        {
            var envs = Environment.GetEnvironmentVariables(target);
            foreach (System.Collections.DictionaryEntry item in envs)
            {
                var keyName = item.Key?.ToString();
                var value = item.Value?.ToString();

                AddSettingsValue(keyName, value);
            }
        }

        void AddSettingsValue(string? keyName,string? value)
        {            
            if(string.IsNullOrEmpty(keyName))
            {
                Console.WriteLine("Cannot add setting with no keyvalue");
                return;
            }

            var tmpKeyName = keyName.ToLowerInvariant();
            if (_machine.ContainsKey(tmpKeyName))
            {
                Console.WriteLine("Will not add duplicate key values");
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine("{Key} was setting did not have a value, and will be ignored",keyName);
                return;
            }

            _machine.Add(tmpKeyName, value);
        }
    }
}
