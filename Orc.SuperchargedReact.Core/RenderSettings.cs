using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Specialized;
using Orc.SuperchargedReact.Core;
using Newtonsoft.Json;

namespace Orc.SuperchargedReact.Web
{
    public class RenderSettings : IRenderSettings
    {
        public RenderSettings(string containerId, object props, string requestedUrl)
            : this("", containerId, props, requestedUrl)
        {
        }

        public RenderSettings(string componentToRender, string containerId, object props, string requestedUrl )
        {
            ComponentNameToRender = componentToRender;
            ContainerId = containerId;
            Props = props;
            RequestedUrl = requestedUrl;
            RouterOutputVarName = "SuperChargedReact.routerOutput";
            RoutesInputVarName = "SuperChargedReact.routes";
        }

        /// <summary>
        /// The url requested by the HTTP request, when loading on the server the React Router will need this (if you are using React Router that is)
        /// </summary>
        public string RequestedUrl { get; set; }

        /// <summary>
        /// The variable name we use to store the HTML from React Router for our dirty hack using the timeout call back
        /// </summary>
        public string RouterOutputVarName { get; set; }

        /// <summary>
        /// The javascript global variable name that the React Router routes are stored in
        /// </summary>
        public string RoutesInputVarName { get; set; }

        /// <summary>
        /// An object that your want passed down to the component, we will handle converting it to JSON for you
        /// </summary>
        public object Props { get; set; }

        /// <summary>
        /// The name of the actual React component you want to render
        /// </summary>
        public string ComponentNameToRender { get; set; }

        /// <summary>
        /// The Html ID of the container/wrapper you want to inject your component into
        /// </summary>
        public string ContainerId { get; set; }
        
    }
}
