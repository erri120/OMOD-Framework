# OMOD Extractor

This program was made for [Wabbajack](https://github.com/halgari/wabbajack) but can also be used standalone.
The name is self-explanatory: you can extract `.omod` files that are used by the [Oblivion Mod Manager](https://www.nexusmods.com/oblivion/mods/2097).

## Usage

Download the latest release and choose the `OMODExtractor-with-7z` version as you need the `7z.exe` and `7z.dll` in the same directory.

You can view all available commands using `OMODExtractor.exe --help`.

The required arguments are `-i` for the input file and `-o` for the output folder. The output folder will be created on start and deleted on start if it already exists. The input file can be both an `.omod` file or an archive (`.zip`,`.7z`). If the input file is an archive that contains the omod file than you should set `-z`/`--sevenzip` to `true` so the program can extract the archive. If `-z`/`--sevenzip` is set to false (default) and the input is an archive than the program expects the extracted omod file in the output directory.

Some arguments such as `-c`/`--config`, `-d`/`--data` and `-p`/`--plugins` are set to true as default as every `.omod` file contains a `config` and a `data.crc` file. If no `plugins.crc` file is found than the program will skip it.

Example command:

`OMODExtractor.exe -i DarNified UI 1.3.2.omod -o output -r true -s true`

This will extract all files from the `.omod` file.
