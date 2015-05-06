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
        public RenderSettings( HttpContext context)
        {
            if ( context != null ) {
//                RequestedUrl = context
            }
        }

        public string RequestUrl { get; set; }

        public string RoutesVariableName { get; set; }

        public NameValueCollection Form { get; set; }

        public string QueryStringRaw { get; set; }

        public NameValueCollection QueryString { get; set; }

        public string RouterOutputVarName { get; set; }

        public object Props { get; set; }

        public string ComponentNameToRender{ get; set; }

        public string ContainerId { get; set; }

        public string ToJavascript() {
            return String.Format(
                @"{{
                    requestedUrl: '{0}',
                    routes: {1},
                    form: {2},
                    queryString: {3},
                    queryStringRaw: {4},
                    routerOutputVar: {5},
                    props: {6},
                    componentName: '{7}',
                    containerId: '{8}'
                }};",
                RequestedUrl,
                RoutesVariableName,
                JsonConvert.SerializeObject(Form, SerializationSettings),
                JsonConvert.SerializeObject(QueryString, SerializationSettings),
                QueryStringRaw,
                RouterOutputVarName,
                JsonConvert.SerializeObject(Props, SerializationSettings),
                ComponentNameToRender,
                ContainerId
            );
        }        
    }
}
