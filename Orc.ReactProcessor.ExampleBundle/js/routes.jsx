var React = require("react/addons");
var Router = require("react-router");
var Route = Router.Route;
var Redirect = Router.Redirect;
var NotFoundRoute = Router.NotFoundRoute;
var DefaultRoute = Router.DefaultRoute;
var Link = Router.Link; 
var RouteHandler = Router.RouteHandler;

var App = React.createClass({ 
	componentWillMount: function() {
	},
  
	render: function () {	
		return (
			<div>
				{/* Render the right component for this url */}
				<RouteHandler {...this.props} />
			</div>
		);
	} 
});

var DebugHandler = React.createClass({
	render: function() {
		return (
			<div>
				<h1>{this.props.testString}</h1>
			</div>
		);
	}
});
var reactRoutesConfig = (
	<Route name="app" path="/" handler={App} {...this.props}>
		<Route name="testApp" path="/tester" handler={DebugHandler} {...this.props} />
	</Route>
);
global.reactRoutesConfig = reactRoutesConfig;
