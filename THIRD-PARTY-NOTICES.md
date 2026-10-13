# Third-party notices

Auto GC Supply ships no third-party data and no code ported from other plugins. The one bundled third-party asset is the font below; its license travels with it.

The release zip also carries the compiled libraries the plugin builds against: the ECommons submodule and the NuGet packages pinned in `AutoGcSupply/packages.lock.json` (the clib automation library and what it depends on). They ship as built and unmodified, each under its own license, and none of their source is copied into this repository. Besides those, the zip holds the plugin itself, its manifest, its icon, the nine language catalogs and the font below.

Auto Shopper and vnavmesh are separate plugins you install yourself; none of their code ships with Auto GC Supply, which talks to them only through Dalamud's IPC. Market prices are read at run time by Auto Shopper from the public Universalis API (universalis.app); no Universalis data ships with the plugin. Game data such as item, zone and class names, the Personnel Officer's menu text and the HQ trade prompt is read from your game client at run time.

## Noto Sans

- Source: Noto Sans Medium by the Noto Project Authors, https://github.com/notofonts/latin-greek-cyrillic, shipped as `AutoGcSupply/Fonts/NotoSans-Medium-Latin.ttf`.
- Used: the primary font for Latin letters, digits and punctuation in the plugin's windows.
- License: SIL Open Font License 1.1, shipped next to the font in full as `AutoGcSupply/Fonts/OFL-NotoSans.txt`.
