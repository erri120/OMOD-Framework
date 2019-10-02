Getting started
==========================================

Huge thank you for checking out my project :)

Installation
------------------------------------------

This Framework is a C# class library (.dll) that you can
include in your project. How you want to include this is up
to you. You can create a `git submodule` and add the project to 
your solution or just get the dll and add that as a reference to
your project.

Setup
-----------------------------------------

.. code-block:: csharp

    using OMODFramework;

.. note:: Do note that you will find another namespace from OBMM
          called *OblivionModManager* that is needed for compiling
          the installation script in *.omod* files.

.. code-block:: csharp

    Framework f = new Framework();

This is the basic setup. You can change some internal variables 
like the temp path used during extraction and script execution with 

.. warning:: When working with paths, always use absolute paths Path.Combine
             when combining paths in this Framework

.. code-block:: csharp

    f.SetTempPath("absolute-path-please");

or change the emulated OBMM version

.. code-block:: csharp

    f.SetOBMMVersion(1, 1, 12);

.. note:: 1.1.12 is the latest official OBMM version

or if you want to use the execute script functions than you might 
want to set the dll path so that you don't get a mission file 
exception.

.. code-block:: csharp

    f.SetDLLPath("erri120.OMODFramework.dll");

What to do next depends on your use case.
