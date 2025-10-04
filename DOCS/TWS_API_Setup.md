# TWS API Setup for AutoRevOption

The IBKR C# API (CSharpAPI.dll) is not included with IB Gateway. You need to download it separately.

## Download TWS API

1. **Go to:** https://www.interactivebrokers.com/en/trading/tws-api.php
2. **Click:** "Download TWS API"
3. **Select:** "TWS API for Windows" (or your OS)
4. **Install** the TWS API package

## Installation Paths

After installing TWS API, the DLLs will be located at:

**Windows:**
- `C:\TWS API\source\CSharpClient\bin\CSharpAPI.dll`
- `C:\TWS API\source\CSharpClient\bin\TWSLib.dll`

**Or:**
- `C:\Program Files\TWS API\source\CSharpClient\bin\`

## Update Project Reference

Once installed, update the path in:
- `AutoRevOption.Monitor\AutoRevOption.Monitor.csproj`

Change the HintPath to match your installation:
```xml
<Reference Include="CSharpAPI">
  <HintPath>C:\TWS API\source\CSharpClient\bin\CSharpAPI.dll</HintPath>
</Reference>
```

## Alternative: Copy DLL to Project

You can also copy the DLL to the project:

1. Copy `CSharpAPI.dll` to `AutoRevOption.Monitor\lib\`
2. Update project reference:
```xml
<Reference Include="CSharpAPI">
  <HintPath>lib\CSharpAPI.dll</HintPath>
</Reference>
```

## Verify Installation

After setup, rebuild:
```bash
dotnet build AutoRevOption.sln
```

You should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
