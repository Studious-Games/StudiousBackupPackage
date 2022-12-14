# Changelog

## [1.0.3] - 2022-09-26

Added a warning when all folders where removed from the folder list, so the user knows that at least one folder needs to be present. 

Added a popup window to let the user know if a backup is in progress while trying to exit Unity.

Removed 7Zip and replaced it with IO.Compression in C#, which allows for the package to work on Mac's now.

Added the ability keep a defined number of backups, anything older than this number will be removed from the backups.

## [1.0.2] - 2022-09-23

Added the ability to add and remove folders to the backup, as well as switching to the new UI Toolkit. When backing up to a selected location, the system will now create a folder based on the project names name to hold all the zip backups.


## [1.0.1] - 2022-09-09

Fixed an issue with creating zip backups over 2GB, and removed the full compression down to normal compression so that people with slower computers weren't waiting an hour for their large projects to backup.


## [1.0.0] - 2022-09-09

This is the initial release of Studious Back Up package.

