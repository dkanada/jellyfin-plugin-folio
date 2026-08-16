<h1 align="center">Folio</h1>
<h3 align="center">Compatible with <a href="https://jellyfin.org">Jellyfin</a></h3>

## About

This is a plugin for Jellyfin that will automatically generate and manage book series as collections.
The eventual goal is merge this feature into the server but it is unclear whether a new entity type will be created.

Note that Folio currently uses the locally supplied SeriesName property for all operations.
It will not overwrite your existing collections and will only modify the series name and media items.
All other values can be changed in the metadata editor - just make sure to keep backups.

It is recommended to disable TheMovieDb as a provider in your server's XML configuration.
Jellyfin does not expose settings for series metadata so they are treated as movie box sets by default.
Inaccurate data will be fetched about your book series unless these providers are disabled.

## Install

1. Open the dashboard in Jellyfin, then select `Plugins` and open `Repositories` at the top right.

2. Click the `+` button, add the repository URL below, and name it whatever you like.

```
https://raw.githubusercontent.com/dkanada/jellyfin-plugin-folio/master/manifest.json
```

3. Select `Catalog` at the top and click on Folio from the list. Install the most recent compatible version.

4. Restart Jellyfin and go back to the plugin settings. Select the Folio card to configure.

## Build

1. Clone or download this repository.

2. Ensure you have the .NET SDK installed and configured.

3. Build the plugin with following commands.

```sh
dotnet publish --configuration release --output bin
```

4. Place the resulting binary in your `plugins` folder.
