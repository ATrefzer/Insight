# Insight
Analyzes the commit history of your version control.

This utility implements some of the ideas presented in Adam Tornhill's great book "Your Code as a Crime Scene".

Currently, it only supports Subversion and C# projects.

Work is in progress ...


## Screenshots

### Show hotspots
Hotspots can be visualized via tree map or circle packaging.

A hotspot is a large file (lines of code) that changes frequently.
This is quite common for configuration files, string resources, designer generated files, etc. 
But the change frequency combined with the large size may also be a hint that the file contains too many responsibilities. Such a hotspot may be a candidate to refactor.

There is a principle in software development called the single responsibility principle (SRP). It says that a class (file) should have only one reason to change. If the file changes often combined with the large size, this may be a hint that the file contains too many responsibilities.

This is a disadvantage for two reasons. First, you need more time to understand this file. Since a software developer usually spends more time reading and understanding code than writing it, this is a waste of time.

Second, if you have to make modifications in a file with interwoven code serving different tasks there is a higher risk that you break an unrelated feature Y when working on feature X.

The utility helps you to find these hotspots.


![Tree Map](https://github.com/ATrefzer/Insight/blob/master/Screenshots/TreeMap.PNG)

![Circle Packaging](https://github.com/ATrefzer/Insight/blob/master/Screenshots/CirclePackaging.PNG)

### Show file trends

![Trend](https://github.com/ATrefzer/Insight/blob/master/Screenshots/LOC.PNG)

### Show change couplings

Files that change together frequently can be visualized in a chord diagram.

There are many cases when modifying a file leads to modification in another one.  Examples are classes and their unit tests, user interfaces and their view models, etc.

However, making this commit pattern visible can give interesting insights in the code.

When a file often changes together with another one, it can also mean that abstraction is missing - causing duplicated work. Or maybe it is just because of copied and pasted code that needs to be maintained twice now. This is dangerous because it is easy to introduce bugs by missing code to update.
 
This kind of analysis can also make dependencies visible that no static code checker can find. Think of an encryption/decryption pair of functions. If you change one, you need to update the other one, too.

Change coupling analysis can raise interesting questions because it takes a different look at the source code.


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
