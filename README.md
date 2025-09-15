# ApplyGitPatch

A comprehensive .NET 8 tool for applying Git patch files (unified diff format) to target directories. This application provides both a user-friendly WPF graphical interface and a command-line interface for automated workflows and CI/CD pipelines.

## Overview

ApplyGitPatch is designed to handle Git patch files with high fidelity, supporting all standard patch operations including file modifications, additions, deletions, and renames. The tool maintains strict context matching to ensure patches are applied correctly and provides detailed logging for troubleshooting.

## Features

### Core Functionality
- **Multi-file patch support**: Apply patches across multiple files in a single operation
- **Complete patch operations**: Support for modify, add, delete, and rename operations
- **Strict context matching**: Ensures patches are applied exactly as intended with proper validation
- **Automatic directory creation**: Creates missing directories as needed during patch application
- **Cross-platform compatibility**: Built on .NET 8 for Windows, Linux, and macOS support

### User Interfaces
- **WPF Graphical Interface**: Intuitive desktop application with real-time logging
- **Command-line Interface**: Headless operation for automation and scripting
- **Real-time feedback**: Detailed logging shows exactly what changes are being applied

### Advanced Features
- **Newline preservation**: Maintains original file line ending styles (CRLF/LF)
- **Error handling**: Comprehensive error reporting with specific line number information
- **Batch processing**: Handle multiple files in a single patch operation
- **Validation**: Pre-application validation to catch potential issues early

## System Requirements

- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 18.04+)
- **Runtime**: .NET 8.0 Runtime or SDK
- **Memory**: Minimum 512MB RAM (recommended 1GB+)
- **Disk Space**: 50MB for application and dependencies

## Installation

### Option 1: Build from Source
```bash
git clone <repository-url>
cd applygitpatch
dotnet build ApplyGitPatch.sln --configuration Release
```

### Option 2: Download Pre-built Binaries
Download the latest release from the releases page and extract to your desired location.

## Project Structure

The solution contains two main projects:

- **`ApplyGitPatch`** (WPF Application): Main desktop application with graphical user interface
- **`ApplyGitPatch.Cli`** (Console Application): Command-line interface for automation

Both projects share the same core patch parsing and application logic, ensuring consistent behavior across interfaces.

## Usage

### WPF Application

Launch the graphical interface:
```bash
dotnet run --project .\ApplyGitPatch\ApplyGitPatch.csproj
```

#### Interface Overview
1. **Select Target Folder**: Click "Select Folder..." to choose the destination directory
2. **Paste Patch Content**: Copy and paste your Git patch content into the text area
3. **Apply Patch**: Click "Apply Patch" to execute the patch application
4. **Review Results**: Monitor the log output for detailed operation results
5. **Clear**: Use "Clear" to reset the interface for a new patch

#### Best Practices
- Use patches generated with full context (`git diff` with adequate context lines)
- Ensure target files match the expected state before applying patches
- Review the log output carefully for any warnings or errors

### Command-Line Interface

#### Basic Syntax
```bash
ApplyGitPatch.Cli --apply <patchfile> --to <targetfolder>
```

#### Examples

**Apply a patch file:**
```bash
dotnet run --project .\ApplyGitPatch.Cli\ApplyGitPatch.Cli.csproj --configuration Release -- --apply sample.patch --to ./TestTarget
```

**Using pre-built executable:**
```bash
./ApplyGitPatch.Cli --apply changes.patch --to /path/to/project
```

#### Exit Codes
- `0`: Success - patch applied without errors
- `1`: Completed with errors - some operations failed
- `2`: Invalid arguments or missing parameters
- `3`: File not found or access denied

## Testing and Validation

### Sample Patch Testing

The repository includes a `sample.patch` file for testing purposes. To test the application:

1. **Prepare test environment:**
   ```bash
   mkdir TestTarget
   echo 'console.WriteLine("Hello, world!");' > TestTarget/Program.cs
   echo 'This file will be removed by the patch.' > TestTarget/OldFile.txt
   ```

2. **Apply the sample patch:**
   ```bash
   dotnet run --project .\ApplyGitPatch.Cli\ApplyGitPatch.Cli.csproj --configuration Release -- --apply sample.patch --to TestTarget
   ```

3. **Expected results:**
   - `Program.cs` modified with updated greeting and additional test line
   - `README.md` created with sample content
   - `OldFile.txt` deleted

### Validation Checklist
- [ ] Patch parses without errors
- [ ] All file operations complete successfully
- [ ] Context matching validates correctly
- [ ] Newline styles preserved
- [ ] Directory structure created as needed

## Patch Format Support

### Supported Formats
- **Standard unified diff**: Full support for `git diff` and `git format-patch` output
- **File operations**: Add, modify, delete, and rename operations
- **Hunk headers**: Proper parsing of `@@` line number specifications
- **Context lines**: Support for context (` `), additions (`+`), and deletions (`-`)
- **File mode changes**: Recognition of `new file mode` and `deleted file mode`

### Limitations
- **Binary patches**: Not supported (text files only)
- **Permission changes**: File permissions beyond new/delete flags not handled
- **Submodules**: Git submodule changes not supported
- **Fuzzy matching**: No partial or fuzzy patch application
- **Large files**: Performance may degrade with very large files (>100MB)

### Error Handling
When context mismatches occur, the application will:
- Log the specific error with line number information
- Continue processing other files in the patch
- Return appropriate exit codes for automation
- Provide detailed error messages for troubleshooting

## Troubleshooting

### Common Issues

**"Context mismatch" errors:**
- Ensure target files match the expected state
- Verify patch was generated with sufficient context
- Check for line ending differences (CRLF vs LF)

**"File not found" errors:**
- Verify target directory exists and is accessible
- Check file paths in the patch are correct
- Ensure proper permissions for file operations

**Performance issues:**
- Large patches may take time to process
- Consider breaking very large patches into smaller chunks
- Monitor system resources during processing

### Debug Mode
For detailed debugging information, run with verbose logging:
```bash
dotnet run --project .\ApplyGitPatch.Cli\ApplyGitPatch.Cli.csproj --configuration Debug -- --apply patch.patch --to target
```

## Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8 SDK
3. Open solution in Visual Studio or VS Code
4. Build and test using the sample patch

### Code Structure
- **PatchParser**: Handles parsing of Git patch files
- **PatchApplier**: Manages application of parsed patches
- **MainWindow**: WPF UI implementation
- **Program**: CLI entry point and argument parsing

## License

This project is provided as internal tooling sample code. Add appropriate licensing information if distributing publicly.

## Changelog

### Version 1.0.0
- Initial release with WPF and CLI interfaces
- Full support for standard Git patch operations
- Comprehensive error handling and logging
- Cross-platform .NET 8 compatibility

---

For additional support or feature requests, please refer to the project documentation or contact the development team.