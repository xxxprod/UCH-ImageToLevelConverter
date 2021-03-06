# UCH-ImageToLevelConverter

This tool should help generate Ultimate Chicken Horse levels out of existing images and has several features to manipulate and optimize them.

It requires [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime) to be installed.

Some of the implemented features are:
- Convert Images to UCH-Levels
- Set the number of Rows & Columns of the Level
- [optional & recommended] Limit the number of colors. (This helps further when you want to optimize the level)
- Pick Colors from the loaded image
- Paint with any color new Blocks
- Fill whole regions with same color
- Erase Blocks one-by-one
- Erase whole regions of same color (Color Reduction helps here)
- Optimize or Break a region (click into any region and all surrounding blocks with same color get optimized)
- Optimize or Break whole Level
- Support for Layers (requires [UCH-BackgroundBlocks](https://github.com/batram/UCH-BackgroundBlocks) mod installed)
- Zoom grid with Ctrl & mouse wheel
- Pan zoomed grid with middle mouse button