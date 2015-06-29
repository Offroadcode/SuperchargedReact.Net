# SuperchargedReact.Net

Inspired by React.NET this is a React engine hardcoded for running the V8 javascript engine via ClearScript.

By targetting just one engine we can tune it for performance and leveraging all the best practise tweaks to get the most out of the V8 engine.

These tweaks can be freely found on the ClearScript forums but it can be diffcult to implement them all (especially without some rework of React.Net) so we rolled them into one implementation.

This implementation of React for .net doesnt pool engines. With the performance we've seen in our testing we have not seen a need to use pooling as yet. You needs may differ so please do your own testing but do not start with the assumption that pooling is a must have, test it on real need first. 

## V8 optimisations utilized

* Single Instance of V8 used to spawn a new context per request - fastest way to get a new context
* Re-uses Compiled scripts which skips the compilation of your js code for each request
* Clears global scope before disposing of the js engine, this greatly speeds up release of unmanaged memory
* Forced garbage collection before discarding the js engine, again freeing up unmanaged memory as fast as possible
* Disable calling .net from within JS by default, gives a big performance improvement

## React goodies baked in

* Build with React Router in mind but not essential, can render a simple component
* Pass in settings into your js engine context
* Re-compiles scripts when files change (via a file watcher), speeds up development time and no reboot required for deployments
* Assumes you are bundling React and/or ReactRouter into your code so you can always run the latest versions
* Happy to run with one bundle that you control, no recompiles required to include addiitonal files

 




##Nuget
Coming soon!
