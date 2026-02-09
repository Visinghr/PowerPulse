# Quick Start: Creating Your First Release

This guide will help you create your first PowerPulse release with executables for x86-64 and ARM64 architectures.

## Prerequisites
- Merge this PR to your main branch
- Ensure your code builds successfully

## Steps to Create a Release

### Method 1: Using Git Tags (Recommended)

1. **Ensure you're on the main branch with latest changes**:
   ```bash
   git checkout main
   git pull origin main
   ```

2. **Create a version tag**:
   ```bash
   git tag v1.0.0
   ```
   
   Use semantic versioning:
   - `v1.0.0` - First major release
   - `v1.1.0` - New features added
   - `v1.0.1` - Bug fixes
   - `v2.0.0-beta.1` - Pre-release versions

3. **Push the tag to GitHub**:
   ```bash
   git push origin v1.0.0
   ```

4. **Monitor the build**:
   - Go to your repository on GitHub
   - Click on the "Actions" tab
   - You should see "Build and Release" workflow running
   - Wait for it to complete (usually 5-10 minutes)

5. **Download your release**:
   - Go to the "Releases" section in your repository
   - You'll see your new release with two ZIP files:
     - `PowerPulse-x86-64.zip` - For standard Windows PCs
     - `PowerPulse-ARM64.zip` - For ARM-based Windows devices

### Method 2: Manual Trigger

1. Go to your repository on GitHub
2. Click on the "Actions" tab
3. Select "Build and Release" from the workflows list
4. Click "Run workflow" button
5. Select the branch (usually `main`)
6. Click the green "Run workflow" button
7. This creates artifacts but NOT an automatic release (you'll need to create release manually)

## What Gets Built

Each build produces a **self-contained executable**, which means:
- ✅ No .NET installation required on the target machine
- ✅ Single executable file (PowerPulse.UI.exe)
- ✅ All dependencies included
- ✅ Works on Windows 10/11 (build 19041+)

## Keeping Source Code Private

Your repository is currently set up to allow releases while keeping code private:

1. **Keep the repository private** (Repository Settings → Change visibility → Make private)
2. **Releases are accessible** to repository collaborators by default
3. **To share publicly**: Share the direct release URL or download link with users

## Troubleshooting

### Build Fails
- Check the Actions tab for detailed error logs
- Verify your code builds locally: `dotnet build PowerPulse.UI/PowerPulse.UI.csproj -c Release`
- Ensure all NuGet packages are properly referenced

### Can't Push Tags
- Make sure you have write access to the repository
- Verify tag doesn't already exist: `git tag -l`

### Release Not Created
- Workflow needs `contents: write` permission (already configured)
- Check if workflow completed successfully in Actions tab
- For manual trigger, releases need to be created manually

## Next Steps

After your first successful release:
1. Test the executables on different machines
2. Create release notes describing features
3. Update version numbers for future releases
4. Consider creating pre-release versions for testing

## Support

If you encounter issues:
1. Check `.github/RELEASE_WORKFLOW.md` for detailed information
2. Review the Actions logs in GitHub
3. Verify local build works first before pushing
