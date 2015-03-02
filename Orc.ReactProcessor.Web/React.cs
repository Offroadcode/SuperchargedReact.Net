using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Orc.ReactProcessor.Core;

namespace Orc.ReactProcessor.Web
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
                            _runner = new ReactRunner(
                                HttpContext.Current.Server.MapPath(ReactConfiguration.Current.FilePath),
                                ReactConfiguration.Current.EnableFileWatcher, 
                                ReactConfiguration.Current.EnableCompilation,
                                ReactConfiguration.Current.DisableGlobalMembers,
                                ReactConfiguration.Current.SerializerSettings);
                        }
                    }
                }
                return _runner;
            }
        }

        private const string ItemsKey = "Orc.ReactProcessor.Scripts";
        public static MvcHtmlString Render(this HtmlHelper helper, string containerId, object props)
        {
            var ctx = HttpContext.Current;
            string url = ctx.Request.Path;
            
            string inBrowserScript;

            var result = Runner.Execute(containerId, url, props, out inBrowserScript);

            ctx.Items[ItemsKey] = inBrowserScript;

            return new MvcHtmlString(result);

        }

        public static MvcHtmlString RenderReactAssets(this HtmlHelper helper)
        {
            var ctx = HttpContext.Current;
            if (ctx.Items.Contains(ItemsKey))
            {
                var str = ctx.Items[ItemsKey] as string;

                str = "<script>" + str + "</script>";

                return new MvcHtmlString(str);
            }

            return new MvcHtmlString("");
        }
    }
}