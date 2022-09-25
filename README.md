
# Studious Backup Package
 
Is a small very simple library, that helps manage your project backups. 

I wrote this because I was seeing so many people using sync services like OneDrive, to store their projects. The problem with this is that it doesn't work with Unity well.

Once the package has been installed, you have two options to make a backup, the first is from the tools menu along the top, and under Studious Backup, there is an option to run a backup.

The package is configurable, by going to the preferences section, by going to the Edit menu and selecting Preferences.

## Support Me

If you want to help support me, for the price of a coffee, it would be gratefully accepted.

[Buy me a coffee](https://www.paypal.com/donate/?business=B6CJ9P4LUCDR8&amount=3.99&no_recurring=0&item_name=I+am+a+retired+developer%2C+who+likes+to+create+useful+tools.+And+I+hope+that+you+would+help+fund+more+projects+like+this+one.&currency_code=USD)

## Installation

Studious Backup Package is best installed as a package, this can be done in a number of ways, the best is to use git and install via the Package Manager.

###### **Via Git Hub** 

We highly recommend installing it this way, if you have git installed on your machine, then you can simply go to main page of this repository and select the code button. Then copy the HTTPS URL using the button next to the URL.

You can then open up the Package Manager within the Unity Editor, and select the drop down on the plus icon along the top left of the Package Manager. You can then select add by Git and paste the URL from the GitHub repository here.

###### **Add Package from Disk** 

There are two ways to do this, one would be to unzip the download to an area on your hard drive, and another would be to unzip the download into the actual project in the packages folder.

Both are workable solutions, however we would suggest if you would like to use this for multiple projects, that you install somewhere on your hard drive that you will remember. And then use the Package Manager to add the package from Disk.

Installing or unzipping into the projects package folder, will work out of the box when you open the project up.

## Usage

Usage is very simple, as explained above you can run a backup from the menus along the top, as well as being able to run a Back Up from the preferences section.

## **Settings**

###### Log To Console

Fairly straight forward option, that logs what is happening to the console.

###### Backup On Exit

This option allows the package to automatically create a Back UP when you exit the Editor, please be aware that the larger the project, the longer it will take to do a Back Up. When using this option, it might appear that the Editor has hung.

We are working on a way to let the user know that something is happening.

###### Number of Backups to keep

This option will allow the package to keep that specific number of backups, therefore not filling up the hard drive with backups.

###### Custom backups folder

By default the package will create a backup folder inside the root of your project, this might be ideal for most users. This option allows for users to select the place where Back Ups will be stored, across all projects.

###### Auto Backup

This section allows for the package to auto Back Up while you are working, and as it runs in the background, it will not interfere with your work flow. If logging is enabled to the console, the package will display when it has started and finished in the console.

###### Folders for Backup

Allows you to add or remove folders from the backup, the default folders are the recommended folders, which you can remove or add to. 

## Important

When the package is in the middle of a backup, or you have Backup on Exit selected. Unity might appear to have hung, please do not kill Unity and just allow it to finish doing what it is doing. The bigger the project, the longer the backup will take in the background.

Killing Unity will create a corrupt backup.