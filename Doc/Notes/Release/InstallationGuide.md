# Weevil: Installation Guide

## Requirements

- 8GB of RAM
  - 16GB of RAM is strongly recommended.
- [.NET 10 Desktop Runtime, x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
	- This dependency must be installed before starting *Weevil*.

## Installation

### Microsoft Installer

1. Start the Weevil setup program.
   - Because the executable is not digitally signed, you may see the following dialog: "Windows protected your PC"
   - Simply click: More Info => Run Anyway
2. Continue through the installation prompts using the default values.

### Compressed Binary Files

- Copy the `*.zip` file to the target computer, and decompress the files.
- The Weevil application assumes the following directory structure at run-time:

```
C:\Program Files\BlueDotBrigade\Weevil
   \Bin\WeevilGui.exe
   \Doc
   \Licenses
```