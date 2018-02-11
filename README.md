# Insight
Analyzes the commit history of your version control.

This utility implements some of the ideas presented in Adam Tornhill's book "Your Code as a Crime Scene".

Currently, it only supports Subversion and C# projects.

Work is in progress ...


## Screenshots

### Show hotspots
Hotspots can be visualized via tree map or circle packaging.
A hotspot is a large file that changes frequently.

![Tree Map](https://github.com/ATrefzer/Insight/blob/master/Screenshots/TreeMap.PNG)

![Circle Packaging](https://github.com/ATrefzer/Insight/blob/master/Screenshots/CirclePackaging.PNG)

### Show file trends

![Trend](https://github.com/ATrefzer/Insight/blob/master/Screenshots/LOC.PNG)

### Show change couplings

Files that change together frequently can be visualized in a chord diagram.

![Change Coupling](https://github.com/ATrefzer/Insight/blob/master/Screenshots/Chord.PNG)

# How to build

* To count lines of code an external tool is used. Download cloc-1.76.exe from https://github.com/AlDanial/cloc/releases/tag/v1.76 and copy it to the directory Binaries\ExternalTools.
* Build Insight.sln (I use Visual Studio 2017). All output is generated in the Binaries directory.

# How to use

Note: If you use SVN, it has to be found in the search path. If you use TortoiseSVN take care that the "Command line client tools" are also installed.

* Start Binaries\Insight.exe
* Click Setup and select the root folder of the project you want to analyze. Also, specify a cache directory. Create an empty directory for each project you want to analyze. The application uses this directory to cache files downloaded from the version control and uses it as output directory for CSV exports.
* Click Sync to update the working copy of your project and calculate some metrics of the files in the project directory.
* Now the functions in the Analysis group are available.
