using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orc.SuperchargedReact.Core
{
    public interface IRenderSettings
    {

        /// <summary>
        /// The properties object that you want to have passed down into the React component, this will be passed in as a JSON object at the time of rendering
        /// </summary>
        object Props { get; }

        /// <summary>
        /// The name of the React component you want to render
        /// </summary>
        string ComponentNameToRender { get; }
        
        /// <summary>
        /// The HTML Id of the element that we want to inject our React output into
        /// </summary>
        string ContainerId { get; }
    }
}
