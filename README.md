# DecryptTextAsset

A tool to decrypt Silksong text assets obtained from `resources.assets`.

This tool does not extract `resources.assets`. You would use tools like [AssetRipper](https://assetripper.github.io/AssetRipper/) or [AssetStudio](https://github.com/Perfare/AssetStudio/) to extract those assets, and then decrypt them with this tool.

# Usage

Run the program once.

Put the text asset files into `text-asset` folder and run the program again.

# Build

.NET 10 is required.

```sh
dotnet publish -c Release
```