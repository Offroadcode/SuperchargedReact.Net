using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Orc.SuperchargedReact.Core
{
    public interface IRenderSettings
    {
        /// <summary>
        /// ReactRouter goodies - The global variable name to find your ReactRouter routes
        /// </summary>
        string RoutesInputVarName { get; set; }

        /// <summary>
        /// ReactRouter goodies - The global variable name to store your ReactRouter HTML output (for server-side rendering)
        /// </summary>
        string RouterOutputVarName { get; set; }

        /// <summary>
        /// ReactRouter goodies - ReactRouter needs to know what url you requested so this is how we pass it in
        /// </summary>
        string RequestedUrl { get; set; }
        
        /// <summary>
        /// The properties object that you want to have passed down into the React component, this will be passed in as a JSON object at the time of rendering
        /// </summary>
        object Props { get; set; }

        /// <summary>
        /// The name of the React component you want to render
        /// </summary>
        string ComponentNameToRender { get; set; }
        
        /// <summary>
        /// The HTML Id of the element that we want to inject our React output into
        /// </summary>
        string ContainerId { get; set; }
    }
}
