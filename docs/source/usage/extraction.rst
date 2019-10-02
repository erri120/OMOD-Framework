Extracting files from OMODs
==========================================

.. code-block:: csharp

    Framework f = new Framework();
    OMOD omod = new OMOD("path.omod", ref f);
    string dataPath = omod.ExtractDataFiles();
    string pluginsPath = omod.ExtractPlugins();

.. note:: ExtractPlugins returns null if there are no plugins

These two little functions will extract all data files and plugins 
and returns the absolute path to their folder.

You can than copy those folders to somewhere or take a look at them.
