# TailNET
TailNET is a small and simple library which will monitor a file and output appended data like -f (follow) function in tail.m

## Target .NET Framework
[.NET Core 3.1](https://dotnet.microsoft.com/download)

## Simple Usage
Add a reference to your project.
```
Right-click on the project node and select Add > Reference.
```
Create a new TailNET object with the path as an argument.
```
TailNET tailNET = new TailNET(filePath);
```
Subscribe to the LineAdded event. Event data is provided as string.
```
tailNET.LineAdded += TailNET_LineAdded;
```
Start the monitoring
```
tailNET.Start();
```
See the example application for a better understanding.

## Worth mentioning
- If the file is deleted, the monitoring will stop. It can be restarted, if the file exists again or is created.
- If the file becomes smaller, it will be recognized and the monitoring will be reset to the new file size.

## Download
Download here: https://github.com/hexadezi/TailNET/releases

## License
This project is licensed under the MIT License - see the LICENSE file for details
