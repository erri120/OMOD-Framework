Executing the installation script
==========================================

This is more complex than just extracting some files. You will 
have to implement all UI elements yourself.

.. code-block:: csharp

    Framework f = new Framework();
    OMOD omod = new OMOD("path.omod", ref f);

    string dataPath = omod.ExtractDataFiles();
    string pluginsPath = omod.ExtractPlugins();

Start by extracting all files from the omod. You will need the 
location later on.

You will have to implement the `IScriptRunnerFunctions` yourself.
You can create a class like

.. code-block:: csharp

    class ScriptFunctions : IScriptRunnerFunctions
    {
        // implement all functions here
    }
    //...

    ScriptFunctions sFunc = new ScriptFunctions();

The `ScriptRunner` class, responsible for executing the script, requires
those functions:

.. code-block:: csharp

    ScriptRunner sr = new ScriptRunner(ref omod, a);
    // to executing the script:
    ScriptReturnData srd = sr.ExecuteScript();
