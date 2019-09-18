# OMOD Extractor

This program was made for [Wabbajack](https://github.com/halgari/wabbajack) but can also be used standalone.
The name is self-explanatory: you can extract `.omod` files that are used by the [Oblivion Mod Manager](https://www.nexusmods.com/oblivion/mods/2097).

## Usage

You have three download options:

1) Standalone command line program with 7z
2) Standalone command line program without 7z
3) DLL Library

### Command line program

Download the latest release and choose the `OMODExtractor-with-7z` version as you need the `7z.exe` and `7z.dll` in the same directory.

You can view all available commands using `OMODExtractor.exe --help`.

The required arguments are `-i` for the input file and `-o` for the output folder. The output folder will be created on start and deleted on start if it already exists. The input file can be both an `.omod` file or an archive (`.zip`,`.7z`). If the input file is an archive that contains the omod file than you should set `-z`/`--sevenzip` to `true` so the program can extract the archive. If `-z`/`--sevenzip` is set to false (default) and the input is an archive than the program expects the extracted omod file in the output directory.

Example command:

`OMODExtractor.exe -i DarNified UI 1.3.2.omod -o output -r true -s true`

This will extract all files from the `.omod` file.

| Arguments | Required | Default | Info |
|-----------|----------|---------|------|
| -i, --input | true |  | The `.omod` file, can also be an archive containing the omod file but 7z has to be enabled for the extraction. |
| -o, --output | true |  | The output folder. |
| -z --sevenzip | false | false | Sets the usage of 7zip, requires the `7z.exe` to be in the same directory |
| -c --config | false | true | Extracts the config to `config.txt` |
| -d --data | false | true | Extracts `data.crc` to `data/` |
| -p --plugins | false | true | Extracts `plugins.crc` to `plugins/`, skips this step if `plugins.crc` is not present |
| -s --script | false | false | Extracts the script to `script.txt` |
| -r --readme | false | false | Extracts the readme to `readme.txt` |
| -q --quiet | false | false | Sets the program to be quiet (no console output) |
| -t --temp | false | temp | The temp folder. This will be inside the output folder and delete after successful execution |

#### Exitcodes

If you decide to use this program inside your application than checking for the exitcodes during execution should be important for you.

| Code        | Comment           |
| ----------- |-----------------|
| 0 | Everything went successful |
| 1 | When `-z` is set to `true` and no `7z.exe` was found |
| 2 | When the output directory doesn't exist after extraction (you will get this when the directory is deleted during execution) |
| 3 | When no `.omod` file was found in the output directory (probably because the archive didnt contain an omod file) |
| 4 | When multiple `.omod` files are in the output directory (possible that the archive contains multiple omod files) |

### DLL

You can add the dll as reference to your project. Do note that the DLL is in written in `C#`.

After adding the dll to your project you will have to use the namespace `OMODExtractorDLL` to access the functions.

```C#
using OMODExtractorDLL;

// path to the omod file (don't use absolute paths)
string source = "youromod.omod";
// output folder
string dest = "output";
// temp folder (should be inside output folder), use Path.Combine for correct paths
string temp = "temp";
// delete the folder if it already exists before creating a new one
Directory.CreateDirectory(dest);
// create a new OMOD
OMOD omod = new OMOD(source,dest,temp);
// now do stuff
omod.SaveConfig();
omod.SaveFile("readme");
omod.SaveFile("script");
omod.ExtractData();
omod.ExtractPlugins();
// be sure to delete the temp folder after execution or on exit
```

Check the [OMODExtract.cs](https://github.com/erri120/OMOD-Extractor/blob/master/OMODExtractor/OMODExtract.cs) for a complete program that makes use of the dll. You can also just check the code for yourself [here](https://github.com/erri120/OMOD-Extractor/tree/master/OMODExtractorDLL). Most of the functions are commented and have documentation available when hovering over the function name in Visual Studio.

## Licence and 3rd Parties

The Licence for this project can be found [here](https://github.com/erri120/OMOD-Extractor/blob/master/LICENSE). This project uses code from the OMM, made by Timeslip under the same licence. The licence for 7zip, made by Igor Pavlov, can be found [here](https://www.7-zip.org/license.txt).
