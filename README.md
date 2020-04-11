# Backup Utility (.NET Core)

Backup utility to copy directories from multiple source locations to a common target directory.

An example usage would be to configure the target directory within a cloud directory (such as Dropbox/OneDrive etc.) in order to selectively backup files to your cloud provider of choice.
(Multiple configurations can be used/run if you wanted to backup to multiple cloud drives.)

Certain file types or directories may be excluded in order to save space on the target directory. For example, in a developer environment, build type folders (such as bin and obj) can be excluded as these are easily recreated by rebuilding a project/solution.

Supported configuration files are in YAML format.  
<br />
  
## Requirements
Requires .NET Core framework v3.1.  
<br />
  
## Help
Help info can be displayed in the console using the **--help** argument:  
  
Example:
```sh
$ dotnet backuputil --help
```
<br />
  
## Version
Version info can be displayed in the console using the **--version** argument:  
  
Example:
```sh
$ dotnet backuputil --version
```
<br />
  
## Creating a Default Config
A default YAML configuration file can be created using the **-c** argument followed by a config name:  
  
Example:
```sh
$ dotnet backuputil -c backup-config1.yaml
```
<br />

## Usage
The name of the configuration file should be passed as a command line argument.  
  
Example:  
```sh
$ dotnet backuputil backup-config1.yaml
```
