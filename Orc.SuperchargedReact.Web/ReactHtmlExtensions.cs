using System;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Orc.SuperchargedReact.Core;

namespace Orc.SuperchargedReact.Web
{
    public static class ReactHtmlExtensions
    {
        private static ReactRunner _runner = null;
        private static object lockObj = new object();
        private const string ItemsKey = "Orc.SuperchargedReact.Scripts";
        private const string PerformanceKey = "Orc.SuperchargedReact.Performance";

        /// <summary>
        /// Our lazy loaded React Runner, this manages all the V8 goodies for us. Reuse this for maximum performance
        /// </summary>
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
                                ReactConfiguration.Current.BootStrapperCommand,
                                ReactConfiguration.Current.SerializerSettings);
                        }
                    }
                }
                return _runner;
                /*
                return new ReactRunner(
                              HttpContext.Current.Server.MapPath(ReactConfiguration.Current.FilePath),
                              ReactConfiguration.Current.EnableFileWatcher,
                              ReactConfiguration.Current.EnableCompilation,
                              ReactConfiguration.Current.DisableGlobalMembers,
                              ReactConfiguration.Current.BootStrapperCommand,
                              ReactConfiguration.Current.SerializerSettings);
                 */ 
            }
        }

        /// <summary>
        /// Use this call if you have routes defined in your bundle and so don't know which component you want to render
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="containerId"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public static MvcHtmlString Render(this HtmlHelper helper, string componentsToRender, object props)
        {
            return helper.Render(componentsToRender, String.Empty, props);
        }

        /// <summary>
        /// Renders the named React component as a string
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="componentToRender"></param>
        /// <param name="containerId"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        public static MvcHtmlString Render(this HtmlHelper helper, string componentToRender, string containerId,
            object props)
        {
            if (String.IsNullOrEmpty(containerId))
            {
                containerId = Guid.NewGuid().ToString();
            }

            var ctx = HttpContext.Current;
            string url = ctx.Request.Path;

            string inBrowserScript;
            ReactPerformaceMeasurements measurements;

            var settings = new RenderSettings(componentToRender, containerId, props, url);
            var result = Runner.Execute(settings, out inBrowserScript, out measurements);

            ctx.Items[ItemsKey] = inBrowserScript;
            ctx.Items[PerformanceKey] = measurements;
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
        public static ReactPerformaceMeasurements GetLastReactPerformance(this HtmlHelper helper)
        {
            var ctx = HttpContext.Current;
            if (ctx.Items.Contains(PerformanceKey))
            {
                var str = ctx.Items[PerformanceKey] as ReactPerformaceMeasurements;
                return str;
            }

            return null;
        }
    }
}