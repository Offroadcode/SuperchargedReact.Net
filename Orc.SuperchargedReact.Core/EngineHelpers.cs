using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript.Windows;
using Orc.SuperchargedReact.Core.TypeHelpers;
using OriginalJsException = Microsoft.ClearScript.ScriptEngineException;
using OriginalUndefined = Microsoft.ClearScript.Undefined;
using Undefined = Orc.SuperchargedReact.Core.TypeHelpers.Undefined;

namespace Orc.SuperchargedReact.Core
{
    public static class EngineHelpers
    {
        public static bool HasVariable(this V8ScriptEngine engine, string variableName)
        {
            string expression = string.Format("(typeof {0} !== 'undefined');", variableName);
            var result = engine.Evaluate<bool>(expression);
            
            return result;
        }

        public static T Evaluate<T>(this V8ScriptEngine engine, string expression)
        {
            object result = engine.Evaluate(expression);
            result = MapToHostType(result);
            return TypeConverter.ConvertToType<T>(result);
        }

        /// <summary>
        /// Executes a mapping from the ClearScript type to a host type
        /// </summary>
        /// <param name="value">The source value</param>
        /// <returns>The mapped value</returns>
        private static object MapToHostType(object value)
        {
            if (value is OriginalUndefined)
            {
                return Undefined.Value;
            }

            return value;
        }

        private static object GetVariableValue(V8ScriptEngine engine, string variableName)
        {
            object result = engine.Script[variableName];
            result = MapToHostType(result);

            return result;
        }

        public static T GetVariableValue<T>(this V8ScriptEngine engine, string variableName)
        {
            object result = GetVariableValue(engine, variableName);

            return TypeConverter.ConvertToType<T>(result);
        }
    }
}
