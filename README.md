isolated-task
=============

Why
---

I ran into [this annoyance](http://stackoverflow.com/q/3371545/239394) when
I would much have preferred to be getting actual work done.  I had the
constraint of needing to stay in Visual Studio 2008, and I needed custom
build tasks.  Trying to develop and debug these tasks when the build
engine kept locking the DLLs was inefficient, as it required repeatedly
closing and reloading Visual Studio.

The suggestion to derive custom tasks from [`AppDomainIsolatedTask`](http://msdn.microsoft.com/en-us/library/microsoft.build.utilities.appdomainisolatedtask(v=vs.90).aspx) did not completely solve the problem, but it pointed me in the
right direction.  Another suggestion to use
`<GenerateResourceNeverLockTypeAssemblies>` similarly was not
always workable.  What I found was that MSBuild seemed to be parsing
`<UsingTask>` directives by loading the DLL into the main application
domain, even though it actually executes the task in a separate domain,
provided that the task class derives from `MarshalByRefObject` (which
`AppDomainIsolatedTask` does).  It occurred to me, then, that a wrapper
for custom tasks should be possible so that the actual custom task DLLs
do not get locked, but a "sacrificial" wrapper DLL would get locked
instead.  This project is that wrapper.

Requirements
------------

1. Visual Studio 2008, for now (other versions may be forthcoming)
2. To build the solution, [WiX Toolset 3.8](http://wixtoolset.org/) is required
3. To sign the IsolatedTask DLL, you will [need your own signing key](http://msdn.microsoft.com/en-us/library/ms247123(v=vs.90).aspx)
4. Optional: To sign the MSI, you will need an installation of the
   Windows SDK in order to use Signtool.exe as well as your
   own Authenticode key

Use
---

### 1. Install the MSI

The installer for v1.0.0.0 is [here](https://github.com/abrobston/isolated-task/releases/download/v1.0.0.0/IsolatedTask.msi).

### 2. Reference IsolatedTask.dll in your MSBuild file

For the current version (1.0.0.0) and the pre-built MSI, add:

```xml
<UsingTask AssemblyName="IsolatedTask, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2b71fcced119aa9c, processorArchitecture=MSIL"
           TaskName="IsolatedTask" />
```

If you compile your own, you will of course need your own primary key token,
version number, etc.

### 3. Replace your other `<UsingTask>` elements

Instead of `<UsingTask>` for your custom tasks, you will just
reference your custom task assemblies in the `<IsolatedTask>`
element within a target.  See below.

### 4. Replace your custom task elements with `<IsolatedTask>` elements

You refer to the assembly with either the `AssemblyName` or `AssemblyFile`
attribute, and refer to the task name with the `TaskNameWithNamespace`
attribute.  For example:

```xml
<Target Name="MyTarget">
  <IsolatedTask AssemblyFile="$(MyAssemblyPath)"
                TaskNameWithNamespace="MyNamespace.MyCustomTask"
                ParameterNames="OneParameter;AnotherParameter"
                ParameterValues="$(SomeProperty);false"
                TaskItems="" />
</Target>
```

XML Reference
-------------

| Attribute | Type | Description | Required |
| --------- | ---- | ----------- | -------- |
| `AssemblyFile` | `string` | The full path to the custom task DLL | Either `AssemblyName` or `AssemblyFile` must be specified |
| `AssemblyName` | `string` | The full name of a strong-named assembly | Either `AssemblyName` or `AssemblyFile` must be specified |
| `InnerTaskOutput` | `string` | The value of the output parameter in the custom task whose name was specified with `OutputParameterName` | Optional output parameter |
| `OutputParameterName` | `string` | The name of the output parameter from the custom task to copy to the `InnerTaskOutput` parameter | Optional |
| `ParameterNames` | `string[]` | The names of the custom task input parameters &mdash; use the same name multiple times to specify multiple values for an array | Optional |
| `ParameterValues` | `string[]` | The values of the custom task input parameters in the same order as specified in `ParameterNames` | Optional |
| `TaskItems` | `ITaskItem[]` | An item array that will be passed to any input parameter in the custom task of type `ITaskItem[]` | Optional |
| `TaskNameWithNamespace` | `string` | The full name, including the namespace, of the custom task class implementing `ITask` | Required |

The number of items in `ParameterNames` and `ParameterValues` must match.
To pass a literal array within XML, separate the items by semicolons.

Limits
------

1. For now, only one `ITaskItem` array is supported, and it will be passed
   to every input parameter of type `ITaskItem[]` in the custom task.
2. Only one output parameter (the one specified with `OutputParameterName`)
   will be available.
3. Visual Studio 2008 only, for now.  The task probably works in
   MSBuild 3.5 (from the command line or a continuous-integration server)
   as well, though I have not yet tested this assertion.  You will, of
   course, need to install the DLL on your build server.

Building IsolatedTask yourself
------------------------------

Currently, IsolatedTask.Setup.wixproj attempts to sign the MSI.  I have
a key that I use in the official MSI build.  Authenticode keys are
available from a number of vendors.  However, if you want to turn off
signing, set `SignOutput` to `false` in the IsolatedTask.Setup.wixproj
MSBuild file.

Contributions
-------------
Pull requests are welcome.  I do not anticipate having much time to
maintain this project.  Contributions must be dual-licenced under
MIT and GPLv2, just as the current code is licensed.
