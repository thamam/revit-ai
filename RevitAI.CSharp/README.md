# RevitAI - C# SDK Implementation

**Alternative implementation using Revit's official C# .NET API**

This branch provides a C# SDK-based implementation of RevitAI, offering:
- ✅ Official Revit API support
- ✅ Better stability and performance
- ✅ Full .NET ecosystem access
- ✅ Professional Visual Studio development experience
- ✅ Easier debugging and error handling

## Why C# SDK vs pyRevit?

| Feature | pyRevit (Python) | C# SDK |
|---------|------------------|--------|
| **Official Support** | Community | ✅ Official Autodesk |
| **Performance** | Slower (IronPython) | ✅ Fast (native .NET) |
| **Debugging** | Limited | ✅ Full VS debugging |
| **Type Safety** | Dynamic | ✅ Strongly typed |
| **Error Messages** | Cryptic | ✅ Clear stack traces |
| **Async/Await** | Limited | ✅ Full support |
| **NuGet Packages** | Limited | ✅ Full ecosystem |
| **IDE Support** | Basic | ✅ Visual Studio/Rider |

## Project Structure

```
RevitAI.CSharp/
├── Application.cs              # Main add-in entry point
├── RevitAI.addin               # Revit manifest file
├── RevitAI.csproj              # Visual Studio project
├── Commands/
│   ├── CopilotCommand.cs       # Main Copilot command
│   └── SettingsCommand.cs      # Settings command
├── Services/
│   ├── ClaudeService.cs        # Claude API integration
│   └── SafetyValidator.cs      # Operation validation
├── Models/
│   └── RevitAction.cs          # Data models
└── UI/
    ├── CopilotDialog.cs        # Main dialog (WPF)
    └── SettingsDialog.cs       # Settings dialog (WPF)
```

## Building the Project

### Prerequisites

1. **Visual Studio 2022** (Community, Professional, or Enterprise)
2. **Revit 2024** (or adjust target version in .csproj)
3. **.NET Framework 4.8 SDK**
4. **Claude API Key** from https://console.anthropic.com/

### Build Steps

```bash
# Restore NuGet packages
dotnet restore RevitAI.csproj

# Build the project
dotnet build RevitAI.csproj --configuration Debug

# Or build for Release
dotnet build RevitAI.csproj --configuration Release
```

The build process automatically copies the DLL to Revit's Add-ins folder.

## Installation

### Automatic (via Build)

The project is configured to automatically deploy to:
```
%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\
```

### Manual

1. Copy `RevitAI.dll` to: `%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\`
2. Copy `RevitAI.addin` to: `%APPDATA%\Autodesk\Revit\Addins\2024\`
3. Restart Revit

## Configuration

### API Key Setup

Set the environment variable:
```cmd
setx CLAUDE_API_KEY "sk-ant-your-api-key-here"
```

Then restart Revit.

## Development

### Opening in Visual Studio

```bash
# Open project
start RevitAI.csproj

# Or open in VS Code with C# extension
code .
```

### Debugging

1. Set Revit.exe as the startup executable in project properties
2. Set breakpoints in Visual Studio
3. Press F5 to start debugging
4. Revit will launch with the add-in attached

### Hot Reload

Changes require rebuilding and reloading:
1. Close Revit
2. Build project (`Ctrl+Shift+B`)
3. Reopen Revit (or use Revit Add-in Manager for hot reload)

## Architecture Comparison

### Same Features as pyRevit Version:
- ✅ Claude API integration (via Anthropic.SDK NuGet)
- ✅ Safety validation (operation allowlist)
- ✅ Natural language parsing (Hebrew + English)
- ✅ Preview/confirm pattern
- ✅ Secure API key management
- ✅ Comprehensive logging

### Improvements Over pyRevit:
- ✅ **Async/Await**: Proper async Claude API calls
- ✅ **Type Safety**: Compile-time error checking
- ✅ **Better Error Handling**: Clear exception chains
- ✅ **Performance**: Native .NET execution
- ✅ **Debugging**: Full Visual Studio debugging
- ✅ **Stability**: No IronPython quirks

## CLI Automation (Future)

While Revit C# SDK doesn't have built-in CLI, we can add automation via:

### Option 1: Revit Batch Processor
- Runs Revit scripts in batch mode
- Supports scheduling
- Good for overnight processing

### Option 2: Forge Design Automation
- Cloud-based Revit automation
- REST API interface
- Scales to multiple machines

### Option 3: Custom Console Host
- Create wrapper application
- Launches Revit programmatically
- Executes commands via API

**Recommendation**: Start with GUI, add CLI automation in Phase 2.

## Testing

### Unit Tests (Planned)

```bash
# Run tests
dotnet test RevitAI.Tests.csproj
```

### Integration Testing

Use Revit Test Framework:
```bash
# Install Revit Test Framework via NuGet
dotnet add package RevitTestFramework
```

## Next Steps

### Epic 2: Intelligent Dimension Automation

Same features as pyRevit version:
1. Dimension command parser
2. Room boundary detection
3. Dimension chain generation
4. Preview & confirmation

**Advantage**: C# will be faster and more reliable!

### Epic 3+: Advanced Features

Additional capabilities enabled by C# SDK:
- Performance-critical operations
- Complex geometry manipulation
- Multi-threaded processing
- Advanced UI with MVVM

## Troubleshooting

### Add-in Not Loading

1. Check manifest path: `%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI.addin`
2. Check DLL path: `%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\RevitAI.dll`
3. Check Revit version matches (2024)

### API Errors

1. Verify API key is set: `echo %CLAUDE_API_KEY%`
2. Test connection from Settings dialog
3. Check internet connectivity

### Build Errors

1. Restore NuGet packages: `dotnet restore`
2. Check .NET Framework 4.8 is installed
3. Verify Revit API NuGet package version matches installed Revit

## Documentation

- [Revit API Docs](https://www.revitapidocs.com/)
- [Anthropic SDK](https://github.com/anthropics/anthropic-sdk-dotnet)
- [RevitAI Architecture](../docs/architecture.md)
- [RevitAI PRD](../docs/PRD.md)

## Support

- GitHub Issues: https://github.com/thamam/revit-ai/issues
- Compare branches: `epic-1-foundation` (pyRevit) vs `csharp-sdk-implementation` (C# SDK)

## License

See LICENSE file in repository root.
