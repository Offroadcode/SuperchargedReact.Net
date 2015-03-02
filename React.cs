using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orc.ReactProcessor.Core;

namespace Orc.ReactProcessor.Mvc
{
    public static class ReactHtmlExtensions
    {
        private static ReactRunner _runner = null;
        private static object lockObj = new object();

        public static ReactRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    lock (lockObj)
                    {
                        if (_runner == null)
                        {
                            _runner = new ReactRunner(ReactConfiguration.Current.FilePath,ReactConfiguration.Current.EnableFileWatcher, ReactConfiguration.Current.EnableCompilation, ReactConfiguration.Current.DisableGlobalMembers);
                        }
                    }
                }
                return _runner;
            }
        }

        public static string Render(this HtmlHelper helper,string containerId, object props)
        {
            var ctx = HttpContext.Current;
            string url = ctx.Request.Path;
            
            string inBrowserScript;

            var result = _runner.Execute(containerId, url, props, out inBrowserScript);
            
            return new MvcHtmlString(result);

        }

        public static string RenderAssets()
        {

        }
    }
}