# Backup Utility (.NET Core)

Backup Utility is a console app that copies files from multiple source directories to a common target directory.  

The app is written as a .NET Core console app rather than a service in order to remain portable between Windows, macOS, and Linux.  
  
Typical cloud backup services only allow you to sync a single directory. Backup Utility can be used to selectively copy multiple source directories to the target directory of your chosen cloud provider. It may also be used to backup files to your own network or USB drive.  

Multiple configuration files can be configured with different target directories if you need to backup to multiple cloud drives.

Certain file types or directories may be excluded in order to save space on the target drive. For example, in a developer environment, build type folders (such as bin and obj) can be excluded as these may take up unnecessary space and are easily recreated by rebuilding a project/solution.

Supported configuration files are in YAML format.  
  
You are welcome to use/update this software under the terms of the **MIT license**.  
<br />
  
## Requirements
Executing the app requires [.NET Core](https://dotnet.microsoft.com/download/dotnet-core) framework v3.1 to be installed.  

Config files (YAML) can be edited using any text editor.
  
To build the project you will need the [.NET Core SDK](https://dotnet.microsoft.com/download).  

I recommend using [Visual Studio](https://visualstudio.microsoft.com). Visual Studio is an IDE with built-in support for C#, comes pre-packaged with the .NET Core SDK, and is available for both [Windows](https://visualstudio.microsoft.com/vs/) or [Mac](https://visualstudio.microsoft.com/vs/mac/). Other options include using a code editor such as [Visual Studio Code](https://code.visualstudio.com) with a C# extension installed.  
<br />

## Publishing
Once the .NET Core SDK has been installed, you can **publish** the app for usage via Visual Studio or the [command line](https://docs.microsoft.com/en-us/dotnet/core/deploying/).  
Note: When using Visual Studio, the Backup Utility project is configured to output a portable cross-platform dll by default.  

To publish via the command line, browse to the project directory and run one of the following commands:

**Cross-Platform dll:**
```sh
$ dotnet publish
```  

**Windows Executable:**
```sh
$ dotnet publish -r win-64
``` 

**Mac Executable:**
```sh
$ dotnet publish -r osx-64
``` 

**Linux Executable:**
```sh
$ dotnet publish -r linux-64
```  
For further options, refer to the [.NET Core publishing documentation](https://docs.microsoft.com/en-us/dotnet/core/deploying/).  
<br />

## Usage
The portable version of the app (**backuputil.dll**) can be executed on either **Windows**, **macOS**, or **Linux**.  

To execute the portable version, open a terminal window and change to the directory containing the app.  

The portable app (**backuputil.dll**) can be executed using the .NET Core framework using the following command:
```sh
$ dotnet backuputil.dll
```  

Alternatively, if the project has been [published](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-runtime-dependent) to target the local platform, the **publish** directory will also contain a native bootstrap file for executing the app.  

The specific command line to run the **executable** with vary depending on the OS:

**Windows:**
```sh
$ backuputil
```

**Mac / Linux:**
```sh
$ ./backuputil
```
<br />

## Help
Help info can be displayed in the console using the **--help**, **-h**, or **-?** argument:  
  
*Examples:*
```sh
$ dotnet backuputil.dll --help
```
```sh
$ dotnet backuputil.dll -h
```
```sh
$ dotnet backuputil.dll -?
```
<br />
  
## Version
Version info can be displayed in the console using the **--version** or **-v** argument:  
  
*Examples:*
```sh
$ dotnet backuputil.dll --version
```
```sh
$ dotnet backuputil.dll -v
```
<br />
  
## Creating a Default Config
A default YAML configuration file can be created using either the **--create** or **-c** argument followed by a config name.  
The config file path is considered relative to the current directory unless the full path is provided.  
  
*Examples:*
```sh
$ dotnet backuputil.dll --create config1.yaml
```
```sh
$ dotnet backuputil.dll -c config1.yaml
```
```sh
$ dotnet backuputil.dll -c C:\Configs\config1.yaml
```  

<br />

## Running a Backup
The name of the backup configuration file to be run should be provided as a command line argument after either the **--run** or **-r** argument.  
The config file path is considered relative to the current directory unless the full path is provided.  
  
*Examples:*  
```sh
$ dotnet backuputil.dll --run config1.yaml
```
```sh
$ dotnet backuputil.dll -r config1.yaml
```
```sh
$ dotnet backuputil.dll -r /Users/freedom35/Configs/config1.yaml
```

Note: For frequent use, you can also run the app via a shortcut or automated script with your configuration file specified in the shortcut settings or script as a command line arg.
  
<br />

## Configuration Settings
The following settings can be configured within the YAML configuration file. Most settings are required to be defined within the configuration file. If any critical setting is missing, or the value is inappropriate, then the settings will not validate and the backup will not be run and the app will exit with an error.

<br /> 

### ***backup_type***
Determines the type of backup to execute *(see table below)*.  

Setting is required. 


|Types|Description|
|:---:|-----|
|**copy**|Copies the contents of the source directory to the target directory. Any files later deleted from the source directory, will remain in the target directory.|
|**sync**|Keeps the target directory in-sync with the source directory. Files deleted from the source directory will also be deleted from the target directory.|
|**isolated**|Creates isolated backups within the target directory. I.e. Each time a backup is run, a new/separate backup copy is created.|

*Example config entries:*
```yaml
backup_type: copy
```
```yaml
backup_type: sync
```

<br />

### ***target_dir***
Defines the path of the root target backup directory, where the backup will take place.  

Setting is required: Must have a target directory in order to back-up.

*Example config entries:*
```yaml
target_dir: C:\Backups
```
```yaml
target_dir: /Users/freedom35/Backups
```

<br />

### ***source_dirs***
Determines the list of source directories that will be backed up.  

Setting is required: Must have at least one source directory to back-up.

*Example config entries:*
```yaml
source_dirs:
 - C:\Users\freedom35\Projects
 - C:\Users\freedom35\Documents\Specs
```
```yaml
source_dirs:
 - /Users/freedom35/Projects
 - /Users/freedom35/Documents/Specs
```

<br />  

### ***max_isolation_days***
Integer value determining the max number of days to keep existing backups.  
This setting is only used when ***isolated*** is configured as the backup type.  

Set to zero for no max limit (default value).  

*Example config entries:*
```yaml
max_isolation_days: 0
```
```yaml
max_isolation_days: 30
```

<br />

### ***ignore_hidden_files***
Determines whether hidden files and folders are ignored during a backup run.  

Default Value: *true*

*Example config entries:*
```yaml
ignore_hidden_files: true
```
```yaml
ignore_hidden_files: false
```

<br />

### ***excluded_dirs***
Determines the list of directories (or sub-directories) that will be ***excluded*** from the backup.  

These directories will not be copied or synced. This can be useful when saving on target storage space.  

Default Value: *None*

*Example config entries:*
```yaml
excluded_dirs:
 - obj
 - bin
 - _sgbak
 - .vs
```
```yaml
excluded_dirs: []
```

<br />

### ***excluded_types***
Determines the list of file types/extensions that will be ***excluded*** from the backup.  

Files with these extensions will not be copied or synced. This can be useful when saving on target storage space.  

Default Value: *None*

*Example config entries:*
```yaml
excluded_types:
 - dll
 - pdb
 - zip
```
```yaml
excluded_types: []
```
