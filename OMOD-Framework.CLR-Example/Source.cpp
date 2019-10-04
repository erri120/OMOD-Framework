#include <iostream>

using namespace OMODFramework;
using namespace OMODFramework::Scripting;
using namespace cli;

// .NET string
typedef System::String^ String;
// .NET string[]
typedef array<String>^ sArray;
// .NET int[]
typedef array<int>^ iArray;
// .NET List<string>
typedef System::Collections::Generic::List<String>^ sList;
// .NET File
typedef System::IO::File File;
// .NET Directory
typedef System::IO::Directory Directory;
// .NET Path
typedef System::IO::Path Path;

ref class ScriptFunctions : IScriptRunnerFunctions
{
public:
	// warns the user
	virtual void Warn(String msg) {}
	// creates a messagebox
	virtual void Message(String text, String title) {}
	// creates a yes-no dialog, return 0=no, 1=yes
	virtual int DialogYesNo(String text, String title) { return 1; }
	// checks if a file relative to the oblivion game folder exists
	virtual bool ExistsFile(String filePath) { return false; }
	// gets the version of a file relative to the oblivion game folder
	virtual System::Version^ GetFileVersion(String filePath) { return gcnew System::Version(1, 0, 0); }
	/*
	Creates a select dialog. The user can select either a single item (multiSelect=false) or multiple items (multiSelect=true)
	Descriptions and preview images are index dependent of the current clicked item index:
	If you select the item with index 2 than the description is in the descriptions array at index 2, same with previewImagePaths
	previewImagePaths can be empty, in this case you only have items with descriptions
	You return an int[] array that contains the indexes of the selected items, if multiSelect=false than the array will
		only contain one item
	*/
	virtual iArray DialogSelect(sArray items, String title, bool multiSelect, sArray previewImagePaths, sArray descriptions)
	{
		return gcnew array<int>(1);
	}
	// displays an image, path to image is absolute and should be located inside the extracted dataFiles folder which is in the temp folder
	virtual void DisplayImage(String imageFilePath) {}
	// displays text, similar to message idk why you have two methods for that
	virtual void DisplayText(String text, String title) {}
	// creates an input dialog, initialContent can be empty
	virtual String InputString(String title, String initialContent) { return ""; }
	// returns a String[] array with the NAMES of all esps, dont care if active or not just return all esp NAMES WITHOUT EXTENSIONS (no .esp or .esm)
	virtual sArray GetActiveESPNames() { return gcnew array<String>(1); }
	/*
	returns the absolute path of a file, the path input argument is relative to the oblivion game folder
	aka: if the scripts wants to get a file that SHOULD be in the oblivion game folder but it's not there
	because MO2 installs those mods somewhere else, you return the path to the mod installation folder, EXAMPLE:
	path = "data/meshes/AnotherMod/FileFromAnotherMod.nif"
	than return something like "C:/Modding/MO2/mods/AnotherMod/meshes/AnotherMod/FileFromAnotherMod.nif"
	NOTE:
	this function is only called inside ReadExistingDataFile, a function callable by an installation script
		ReadExistingDataFile original function was to read a data file which resides inside the oblivion data folder
		and return it's content as a byte[] array. I've yet to see a script which uses this function
		ReadExistingDataFile is also an C# only function, so obmmScripts do not call this function
	*/
	virtual String GetFileFromPath(String path) { return ""; }
};

int main() {
	Framework^ f = gcnew Framework();
	f->SetDLLPath(""); // important when the dll is not in the current dir
	f->SetTempDirectory(""); // temp directory will contain extracted data

	String omodPath = ""; // path to the omod file (should be absolute)

	OMOD^ omod = gcnew OMOD(omodPath, f);

	String dataPath = omod->ExtractDataFiles(); // will be inside the temp folder
	String pluginsPath = omod->ExtractPlugins(); // can be null if there are no plugins

	// you can get all values from the loaded config:
	String modName = omod->GetModName();
	String author = omod->GetAuthor();
	String description = omod->GetDescription();
	String email = omod->GetEmail();
	String website = omod->GetWebsite();
	String version = omod->GetVersion(); // version of the mod
	// version of the omod, the file itself not the mod:
	unsigned char fileVersion = omod->GetFileVersion(); // in C# this returns a byte that can either be 1,2,3,4


	ScriptFunctions^ sf = gcnew ScriptFunctions();

	// new ScriptRunner
	ScriptRunner^ sr = gcnew ScriptRunner(omod, sf);
	String script = sr->GetScript(); // get the entire script without the first byte
	ScriptType^ scriptType = sr->GetScriptType(); // get the script type (obmmScript, VB, C# or python)

	ScriptReturnData^ srd = sr->ExecuteScript(); // execute the script

	if (srd->CancelInstall) exit(0); // check if the installation is cancled for some reason

	/*
					What to do with the ScriptReturnData(srd)?
	========================================================================================================
	There is one fact you always have to keep in mind:
		plugins (esp, esm files) and data files (meshes, textures, sounds,...) are seperated:
		an .omod file is an archive. In this archive you will find these files:
			script
			readme		(optional)
			config
			data.crc
			plugins.crc	(optional)
		The plugins are in the plugins.crc file and the data files are in the data.crc file.
		This means that during extraction to the temp folder you will get two folders, one for data.crc and
		one for plugins.crc
		In OBMM this seperation carries over to the functions:
		------------------------------------------------------
			InstallAllPlugins	-	InstallAllData
			InstallPlugins		-	InstallData
			IgnorePlugins		-	IgnoreData
			CopyPlugins			-	CopyDataFiles


	Now that you know those things are seperated and have their own functions, we can get to work:
		The basic task list is:
			- check if you can install everything (InstallAll is a boolean)
			- check the Install list
			- ignore items in the Ignore list
			- copy the items from the Copy list
	*/

	//--------------------------------------------------------------------------
	//============= the following example is the C# example in C++ =============
	//--------------------------------------------------------------------------

	/*
	Two .NET List<string> which will be populated with all plugins/datafiles to be installed
	*/
	sList InstallPlugins = gcnew System::Collections::Generic::List<String>();
	sList InstallDataFiles = gcnew System::Collections::Generic::List<String>();

	// if we can install all plugins, add all plugins from the GetPluginList to the InstallPlugins list
	if (srd->InstallAllPlugins) {
		for each (String s in omod->GetPluginList())
		{
			// safety check from C#
			if (!s->Contains("\\")) InstallPlugins->Add(s);
		}
	}

	/*
	if you can't install everything go and check the list called InstallPlugins
	this list gets populated when InstallAllPlugins is false
	the Framework comes with two utility functions that helps in creating the temp list:
	strArrayContains and strArrayRemove
	*/
	for each (String s in srd->InstallPlugins)
	{
		if (!Framework::strArrayContains(InstallPlugins, s)) InstallPlugins->Add(s);
	}

	// next up is removing all plugins that are set to be ignored:
	for each (String s in srd->IgnorePlugins)
	{
		Framework::strArrayRemove(InstallPlugins, s);
	}

	/*
	last is going through the CopyPlugins list
	in case you ask why there is a CopyPlugins list and what is does:
	(it makes more sense with data files but whatever)
	if the omod has eg this folder structure:

	installfiles/
				Option1/
						Meshes/
						Textures/
				Option2/
						Meshes/
						Textures/
	this is nice for writing the installation script as you kan keep track of what option
	has what files
	Authors than call CopyPlugins/Data and move the files from the options folder to
	the root folder:

	meshes/
	textures/
	installfiles/
				Option1/
						Meshes/
						Textures/
				Option2/
						Meshes/
						Textures/
	*/
	for each (ScriptCopyDataFile scd in srd->CopyPlugins)
	{
		// check if the file you want to copy actually exists
		if (!File::Exists(Path::Combine(pluginsPath, scd.CopyFrom)));
		else {
			if (scd.CopyFrom != scd.CopyTo)
			{
				// unlikely but you never know
				if (File::Exists(Path::Combine(pluginsPath, scd.CopyTo))) File::Delete(Path::Combine(pluginsPath, scd.CopyTo));
				File::Copy(Path::Combine(pluginsPath, scd.CopyFrom), Path::Combine(pluginsPath, scd.CopyTo));
			}
			// important to add the file to the temp list or else it will not be installed
			if (!Framework::strArrayContains(InstallPlugins, scd.CopyTo)) InstallPlugins->Add(scd.CopyTo);
		}
	}

	// now do the same for the data files :)
	if (srd->InstallAllData)
	{
		for each (String s in omod->GetDataFileList()) { InstallDataFiles->Add(s); }
	}
	for each (String s in srd->InstallData) { if (!Framework::strArrayContains(InstallDataFiles, s)) InstallDataFiles->Add(s); }
	for each (String s in srd->IgnoreData) { Framework::strArrayRemove(InstallDataFiles, s); }
	for each (ScriptCopyDataFile scd in srd->CopyDataFiles)
	{
		if (!File::Exists(Path::Combine(dataPath, scd.CopyFrom)));
		else
		{
			if (scd.CopyFrom != scd.CopyTo)
			{
				// because data files can be in subdirectories we have to check if the folder actually exists
				String dirName = Path::GetDirectoryName(Path::Combine(dataPath, scd.CopyTo));
				if (!Directory::Exists(dirName)) Directory::CreateDirectory(dirName);
				if (File::Exists(Path::Combine(dataPath, scd.CopyTo))) File::Delete(Path::Combine(dataPath, scd.CopyTo));
				File::Copy(Path::Combine(dataPath, scd.CopyFrom), Path::Combine(dataPath, scd.CopyTo));
			}
			if (!Framework::strArrayContains(InstallDataFiles, scd.CopyTo)) InstallDataFiles->Add(scd.CopyTo);
		}
	}

	// after everything is done some final checks
	for (int i = 0; i < InstallDataFiles->Count; i++)
	{
		// if the files have \\ at the start than Path.Combine wont work :(
		if (InstallDataFiles[i]->StartsWith("\\")) InstallDataFiles[i] = InstallDataFiles[i]->Substring(1);
		String currentFile = Path::Combine(dataPath, InstallDataFiles[i]);
		// also check if the file we want to install exists and is not in the 5th dimension eating lunch
		if (!File::Exists(currentFile)) InstallDataFiles->RemoveAt(i--);
	}

	for (int i = 0; i < InstallPlugins->Count; i++)
	{
		if (InstallPlugins[i]->StartsWith("\\")) InstallPlugins[i] = InstallPlugins[i]->Substring(1);
		String currentFile = Path::Combine(pluginsPath, InstallPlugins[i]);
		if (!File::Exists(currentFile)) InstallPlugins->RemoveAt(i--);
	}

	String OutputDir = ""; // final destination

	// now install
	for (int i = 0; i < InstallDataFiles->Count; i++)
	{
		// check if the folder exists before copying
		String s = Path::GetDirectoryName(InstallDataFiles[i]);
		if (!Directory::Exists(Path::Combine(OutputDir, s))) Directory::CreateDirectory(Path::Combine(OutputDir, s));
		File::Move(Path::Combine(dataPath, InstallDataFiles[i]), Path::Combine(OutputDir, InstallDataFiles[i]));
	}
	for (int i = 0; i < InstallPlugins->Count; i++)
	{
		File::Move(Path::Combine(pluginsPath, InstallPlugins[i]), Path::Combine(OutputDir, InstallPlugins[i]));
	}

	return 0;
}