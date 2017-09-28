# nbox

NBox is the utility which can help to simplify deployment of your Application, minimize load time when it starts at
executing, minimize the cumulative size of Application and hide your Application code from quick decompilation
by tools like Reflector.

You can merge several dependency assemblies into a one executable file. Result assembly will be 100% managed.
Also you can embed any file into result assembly. It can be a native module (dll), or configuration files
(config, xml), or any file that you need. This content of result assembly will be extracted on loading time into
directory that you can specify.

You can select the kind of register your dependency. There are three ways to do it :
* File (assembly will attempt to load dependency from separate packed file, usually placed near than result assembly)
* Resource (assembly will be loaded from resources)
* Overlay (in build time the packed dependency assembly will be appended to the end of result assembly file)
* App.config file. If you have a configuration file, you can specify it explicitly. And NBox will unpack and configure your application during starting
  even if executable file will be renamed.

You can straightforward automate the build process because there are no command line arguments set,
there are only NBox.exe and your configuration file.

NBox is compatible with many obfuscation tools like {smartassembly}. If you use tools like that, you can make the 2-level obfuscation : before NBoxing and
after them. Result assembly will be 100% managed, compressed and obfuscated.

NBox uses LZMA algorithm applied in popular archive manager 7-zip (http://7-zip.org) so it produces the
minimal size of output assemblies and provides high decompression speed at run time.
