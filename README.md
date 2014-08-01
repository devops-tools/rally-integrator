rally-integrator
================

Updates Rally with version control changeset and continuous integration build information and links.

To use it:
 - download the latest release from https://github.com/devops-tools/rally-integrator/releases
 - modify the ri.exe.config file with credentials and settings specific to your environment
 - run like so: ```ri.exe -c [changeset-number]```
 - if you also want to reprocess changesets whose builds may have completed since the last run add ```-i`` to the command arguments

To continuously integrate changesets and build info into Rally, run the application as a TeamCity job, triggered after each build of interest.
