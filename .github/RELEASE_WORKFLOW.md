# PowerPulse Release Workflow

This repository is configured with GitHub Actions to automatically build and release PowerPulse executables for multiple architectures.

## How It Works

The workflow builds self-contained Windows executables for:
- **x86-64 (x64)**: Compatible with most Windows PCs
- **ARM64**: Compatible with ARM-based Windows devices (e.g., Surface Pro X, Windows on ARM laptops)

## Triggering a Release

### Option 1: Create a Git Tag (Recommended)
1. Commit your changes to the main branch
2. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. The workflow will automatically trigger and create a release

### Option 2: Manual Trigger
1. Go to the "Actions" tab in your GitHub repository
2. Select "Build and Release" workflow
3. Click "Run workflow" button
4. Choose the branch and click "Run workflow"

## Release Output

Each release will include:
- `PowerPulse-x86-64.zip`: Executable for x86-64 architecture
- `PowerPulse-ARM64.zip`: Executable for ARM64 architecture

Each zip file contains a standalone executable that can run on Windows without requiring .NET to be installed.

## Keeping Source Code Private

To release executables while keeping the source code private:
1. Keep this repository private (Repository Settings → Change visibility → Private)
2. The releases and their executable files will still be publicly accessible
3. Users can download the executables from the Releases page without seeing your source code

**Note**: By default, releases in a private repository are only visible to repository collaborators. To make releases public while keeping the code private, you'll need to manually share the release URLs or use GitHub's release API with a personal access token.

## Publishing Configuration

The workflow publishes executables with these settings:
- **Self-contained**: Includes the .NET runtime (no installation required)
- **Single file**: Everything bundled into one executable
- **Platform-specific**: Optimized for each target architecture

## Version Numbering

Follow semantic versioning for your tags:
- `v1.0.0` - Major release
- `v1.1.0` - Minor release (new features)
- `v1.0.1` - Patch release (bug fixes)
- `v2.0.0-beta.1` - Pre-release versions

## Troubleshooting

If the workflow fails:
1. Check the Actions tab for error logs
2. Ensure your code builds successfully locally with:
   ```bash
   dotnet build PowerPulse.UI/PowerPulse.UI.csproj -c Release
   ```
3. Verify all dependencies are properly referenced in the .csproj files
