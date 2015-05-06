// We recommend webpack for combining these file, we also recommend including React and React-Router in your bundle
global.React = require("react/addons");
Router = require("react-router");
global.Router = Router; 
/* 

You MUST include this file in your bundle.

This is the entry point to rendering your root React application/component.

settings = An object built up in .net containing some of the data you might need to get React to play nice

settings = {
	requestdUrl : "string",		// Needed by ReactRouter to know which route to render
	routes : {},				// Your ReactRouter routes that you should pass in when you called Html.Render assuming you are using ReactRouter, if not then ignore
	form : {},					// The forms collection as a dictionary
	querystring : {},			// The querystring as a dictionary
	routerOutputVar : "string",	// The name of the variable to store the ReactRouter output into, used only in your server-side rendering code and only if you are using ReactRouter
	props : {},					// Any props object passed in, this will be the Model you passed in when calling Html.Render in your template
	componentName: "string",	// Name of the component to render if not using ReactRouter
	containerId: "string"		// The name of the HTML element to inject your components output into, only used client-side
}

*/

// One render function to rule them all...
function Render( settings ) {   
	if ( settings == null ) {
		console.error("Settings object is missing/empty for _ReactRunnerRender function");
	}
	
	// You can edit this to your hearts desire if you like or just leave it as it is and it will work out the box for ReactRouter and React components
	if ( typeof window == "undefined" ) {
		// Server-side rendering
		if ( settings.routes != null ) {
			// If we have a routes object then we assume its a ReactRouter request, modify this if you are using a different Router you crazy cat
			Router.run( settings.routes, settings.requestedUrl, function( Handler ) { 
				// In this call back we render the right component for the given url to a string 
				// and store it in our global variable so the host can pick it up and render it
				settings.routerOutputVar = React.renderToString( React.createElement( Handler, props ));
			});
		} else {
			// Plane old React component request
			// Render your requested component to a string and store it in our global variable so the host can pick it up and render it
			settings.routerOutputVar = React.renderToString( React.createElement( settings.componentName, props ));
		}
	} else {
		// Client-side rendering
		if ( settings.routes != null ) {
			Router.run( reactRoutesConfig, Router.HistoryLocation, function( Handler ) { 
				React.render(
					React.createElement( Handler, settings.props ), 
					document.getElementById( settings.containerId )
				);
			});
		} else {
			// Plane old React component request
			// Render your requested component to a string and store it in our global variable so the host can pick it up and render it
			React.render( 
				React.createElement( settings.componentName, props ), 
				document.getElementById( settings.containerId )
			);
		}
	}        
}

// Stash our entry point in a namespaced global variable so our server-side code can use it
global._ReactRunner.entryPoint = Render;