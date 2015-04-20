module.exports = function(grunt) {

	var standardBanner = "/*\n\nCOMPILED CODE - DO NOT EDIT BY HAND - Built by Grunt\n\nLast built: " + new Date( Date.now() ) + "\n\nOffroadcode Ltd - Olympic Holidays javascript\n\n*/";
	var reactRelatedFiles = ["assets/js/Constants.js", "assets/js/Helpers.js", "assets/js/components/*.jsx", "assets/js/app.jsx", "./node_modules/react-router-component/index.js" ];

    // All configuration goes here
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),

        // Auto prefix any CSS3 properties in CSS file
        autoprefixer: {
            options: {
               browsers: ['last 2 versions']
            },
            dist: { // Target
                files: {
                    'assets/css/screen.css': 'assets/css/screen.css'
                }
            }
        },

        // Create fallback PNG's from any SVG files
        svg2png: {
            all: {
                files: [
                    {
                        cwd: 'assets/img/svg/',
                        src: ['**/*.svg'],
                        dest: 'assets/img/'
                    }
                ]
            }
        }
        ,

        // Compile Sass
        sass: {
            dist: {
                options: {
                    style: 'compressed'
                },
                files: {
                    'assets/css/screen.css': 'assets/sass/screen.scss'
                }
            }
        },

		replace: {
			dist: {
				options: {
					patterns: [
						{
							match: /require\(\"react\"\)/,
							replacement: 'global.React'
						}
					]
				},
				files: [
					{
						expand: false,
						flatten: true,
						src: reactRelatedFiles,
						dest: './build/'
					}
				]
			}
		},

		browserify: {
			options: {
				browserifyOptions: {
					debug: true,
					dedupe : true,
					detectGlobals: true,
					insertGlobals: false,
					bundleExternal: false
				},
				external: [ "react" ],
				//ignore: [ "react" ],
			    //exclude: ["react"],

				transform: [ require('grunt-react').browserify, "browserify-shim" ],
				banner: standardBanner
			},
			app: {
				//src:        [ "build/assets/js/**.js", "build/assets/js/**.jsx" ],
				src:		reactRelatedFiles,
				dest:       'assets/js/bundle.js'
			}
		},

		// Monitor these SASS files for any changes
        watch: {
            css: {
                files: ['assets/sass/**/*.scss'],
                tasks: ['sass'],
                options: {
                    spawn: false
                }
            },
			js: {
				files: ['assets/js/**/*.jsx'],
				tasks: ["compilejs" ],
				options: {
					spawn: false
				}
			}
        },

		// Check the syntax of our JS
        jshint: {
          	//Everything like in grunt-contrib-jshint
			all: reactRelatedFiles

		},

		jsdoc : {
			dist : {
				src: ["assets/js/**/*.jsx"],
				options: {
					destination: 'doc',
					configure: "assets/js/jsdoc-config.json"
				}
			}
		},

		// Minify our javascript for us
		uglify: {
			options: {
				mangle: false,
				sourceMap: true,
				sourceMapIncludeSources: true,
				banner: standardBanner
			},
			my_target: {
				files: {
					'assets/js/bundle.min.js': ['assets/js/bundle.js']
				}
			}
		},

		// Strip out our source maps from our browserify files
		exorcise: {
			bundle: {
				options: {},
				files: {
					'assets/js/bundle.js.map': ['assets/js/bundle.js'],
				}
			}
		}
    });

	 // Where we tell Grunt we plan to use this plug-in.
    grunt.loadNpmTasks('grunt-autoprefixer');
    grunt.loadNpmTasks('grunt-svg2png');
    grunt.loadNpmTasks('grunt-contrib-sass');
    grunt.loadNpmTasks('grunt-contrib-watch');
	grunt.loadNpmTasks('grunt-jsxhint'); // Syntax checker for our js
	grunt.loadNpmTasks('grunt-react'); // Compiles jsx files to js
	grunt.loadNpmTasks('grunt-browserify'); // combines CommonJS style js files
	grunt.loadNpmTasks('grunt-contrib-uglify');	 // Minifies our js
	grunt.loadNpmTasks('grunt-exorcise'); // Removes .map files from the bottom of browserify bundles
	grunt.loadNpmTasks('grunt-jsdoc');
	grunt.loadNpmTasks('grunt-replace'); // Allows us to do find/replace for stuff (handy for hacking require statements out for big librarys like react for instance)

    // Where we tell Grunt what to do when we type "grunt" into the terminal (register them in the order we want them to run).
    grunt.registerTask('default',
        [
            'autoprefixer',
            'sass',
            'watch'
        ]);

	// Build server tasks
    grunt.registerTask('compilejs',
        [
			//'clean',
			'jshint',
	//		'replace',
       //     'browserify',
		//	'exorcise',
		//	'uglify'
        ]);
	grunt.registerTask('buildcss',
        [
            'sass',
            'autoprefixer'
   		]);
	grunt.registerTask('buildsvg',
    [
        'svg2png'
	]);
	// Build server tasks
    grunt.registerTask('buildjs',
        [
			'jshint',
			'jsdoc',
			'browserify',
			'exorcise',
			'uglify'
        ]);

    // Build server tasks
    grunt.registerTask('js',
        [
			'jshint'
        ]);

	grunt.registerTask('getdlls', 'Pull the latest DLLs from ftp', function() {
		// Force task into async mode and grab a handle to the "done" function.
		var done = this.async();
		var Client = require('ftp');
		var fs = require('fs');
  		var path = require('path');

		var unzip = require('unzip');


  		var remDir = function(dirPath, removeSelf) {
      		if (removeSelf === undefined)
        		removeSelf = true;
      		try { var files = fs.readdirSync(dirPath); }
      		catch(e) { return; }
      		if (files.length > 0)
    			for (var i = 0; i < files.length; i++) {
          			var filePath = dirPath + '/' + files[i];
          			if (fs.statSync(filePath).isFile())
            				fs.unlinkSync(filePath);
          			else
            			remDir(filePath);
        		}
      		if (removeSelf)
        		fs.rmdirSync(dirPath);
    		};

		var config = {
			host: 'tools.devops.offroadcode.com',
			port: 21,
			user: 'olympicdlls',
			password: '4iC4cNMi25XIK0L'
		};

		grunt.log.writeln('Deleting current dlls!');
		remDir("bin/",false);


		var c = new Client();
		c.on('ready', function() {
			grunt.log.writeln('Starting download');
			c.get('bin.zip', function(err, stream) {
				if (err)
				{
					throw err;
				}
				stream.once('close', function() {
					c.end();
					grunt.log.writeln('Unzipping');

					var fstr = fs.createReadStream('bin.zip');
					fstr.once('close', function(){
						grunt.log.writeln('Deleting temporary file');
						fs.unlinkSync('bin.zip');

						done();

					});
					fstr.pipe(unzip.Extract({ path: '.' }));

				});
				stream.pipe(fs.createWriteStream('bin.zip'));
			});
		});
		grunt.log.writeln('Connecting to ftp');
		c.connect(config);
	});

	grunt.registerTask('uploadDlls', 'Zips and uploads the dlls to the server', function() {
		// Force task into async mode and grab a handle to the "done" function.
		var done = this.async();
		var Client = require('ftp');
		var fs = require('fs');
  		var path = require('path');

		var archiver = require('archiver');
		var output = fs.createWriteStream('latestDlls.zip');
		var archive = archiver('zip');


		var config = {
			host: 'tools.devops.offroadcode.com',
			port: 21,
			user: 'olympicdlls',
			password: '4iC4cNMi25XIK0L'
		};

		output.on('close', function() {
		  grunt.log.writeln(archive.pointer() + ' total bytes');
		  grunt.log.writeln('archiver has been finalized and the output file descriptor has closed.');
		  var c = new Client();
		  c.on('ready', function() {
		  	grunt.log.writeln('uploading dlls');
		    c.put('latestDlls.zip', 'bin.zip', function(err) {
		      if (err) throw err;
		      c.end();
		      fs.unlinkSync('latestDlls.zip');
		      grunt.log.writeln('Done!');
		      done();
		    });
		  });
		  grunt.log.writeln('connecting to ftp');
		  c.connect(config);
		});
		grunt.log.writeln('Zipping dlls!');
		archive.pipe(output);
		archive.bulk([
		  { cwd: 'bin/', src: ['**/*.dll'], expand: true },
		]);
		archive.finalize();
	});
};