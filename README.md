rally-integrator
================

Updates Rally with version control changeset and continuous integration build information and links

To use it, just modify the app.release.config (or app.debug.config) file in the console application with credentials and settings specific to your environment. Compile, and run.

To continuously integrate changesets and build info into Rally, run the application as a TeamCity job, triggered after each build of interest.
