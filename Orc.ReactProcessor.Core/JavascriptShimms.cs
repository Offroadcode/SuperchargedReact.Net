using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orc.ReactProcessor.Core
{
    public static class JavascriptShimms
    {
        public const string ConsoleShim = @"var global = global || {};

            // Basic console shim. Caches all calls to console methods.
            function MockConsole() {
	            this._calls = [];
	            ['log', 'error', 'warn', 'debug', 'info', 'dir', 'group', 'groupEnd', 'groupCollapsed'].forEach(function (methodName) {
		            this[methodName] = this._handleCall.bind(this, methodName);
	            }, this);
            }
            MockConsole.prototype = {
	            _handleCall: function(methodName/*, ...args*/) {
		            var serializedArgs = [];
		            for (var i = 1; i < arguments.length; i++) {
		            	try {
			        	    serializedArgs.push(JSON.stringify(arguments[i]));
			        	} catch(err){
			        		 serializedArgs.push('ServerSideError:'+err);
			        	}
		            }
		            this._calls.push({
			            method: methodName,
			            args: serializedArgs
		            });
	            },
	            _formatCall: function(call) {
		            return 'console.' + call.method + '(""[.NET]"", ' + call.args.join(', ') + ');';
	            },
	            getCalls: function() {
		            return this._calls.map(this._formatCall).join('\n');
	            }
            };
            var console = new MockConsole();";
    }
}
