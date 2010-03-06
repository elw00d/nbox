using System.Collections.Generic;
using System.Threading;
using NBox.Utils;

namespace NBox.Config
{
    public enum OutputAppType
    {
        Console = 1,
        WinExe = 2
    }

    public enum OutputMachine
    {
        Any = 1,
        x86 = 2,
        x64 = 3,
        Itanium = 4
    }

    public enum CompilerVersionRequired
    {
        v2_0,
        v3_0,
        v3_5,
        v4_0
    }

    public sealed class OutputConfig
    {
        private readonly OutputAppType appType;

        public OutputAppType AppType {
            get {
                return (this.appType);
            }
        }

        private readonly OutputMachine machine;

        public OutputMachine Machine {
            get {
                return (this.machine);
            }
        }

        private readonly string path;

        public string Path {
            get {
                return (this.path);
            }
        }

        private readonly string assemblyName;

        [CanBeNull]
        public string AssemblyName {
            get {
                return (assemblyName);
            }
        }

        private readonly string win32IconPath;

        public string Win32IconPath {
            get {
                return (this.win32IconPath);
            }
        }

        private readonly IncludedAssemblyConfig mainAssembly;

        public IncludedAssemblyConfig MainAssembly {
            get {
                return (mainAssembly);
            }
        }

        private readonly ApartmentState apartmentState;

        public ApartmentState ApartmentState {
            get {
                return (this.apartmentState);
            }
        }

        private readonly bool grabResources;

        public bool GrabResources {
            get {
                return (grabResources);
            }
        }

        private readonly string compilerOptions;

        [CanBeNull]
        public string CompilerOptions {
            get {
                return (compilerOptions);
            }
        }

        private readonly CompilerVersionRequired compilerVersionRequired;

        /// <summary>
        /// By default set to v2_0.
        /// </summary>
        public CompilerVersionRequired CompilerVersionRequired
        {
            get
            {
                return compilerVersionRequired;
            }
        }

        private readonly List<IncludedObjectConfigBase> includedObjects = new List<IncludedObjectConfigBase>();

        /// <summary>
        /// Objects declared to embed into output assembly
        /// excluding the main assembly config.
        /// </summary>
        public IList<IncludedObjectConfigBase> IncludedObjects {
            get {
                return (includedObjects);
            }
        }

        private readonly string appConfigFileId;

        [CanBeNull]
        public string AppConfigFileID
        {
            get
            {
                return appConfigFileId;
            }
        }

        private readonly bool useShadowCopying;

        public bool UseShadowCopying
        {
            get
            {
                return useShadowCopying;
            }
        }

        /// <summary>
        /// Retrieves all included objects declared in output assembly configuration
        /// including the main assembly configuration.
        /// </summary>
        public IList<IncludedObjectConfigBase> GetAllIncludedObjects() {
            IList<IncludedObjectConfigBase> res = new List<IncludedObjectConfigBase>(this.includedObjects);
            res.Add(this.mainAssembly);
            return (res);
        }

        public OutputConfig(OutputAppType outputAppType, OutputMachine outputMachine, string outputPath,
            string assemblyName, string outputWin32IconPath, IncludedAssemblyConfig mainAssembly,
            ApartmentState outputApartmentState, bool grabResources, string compilerOptions,
            CompilerVersionRequired compilerVersionRequired,
            string appConfigFileId, bool useShadowCopying)
        {
            //
            this.appType = outputAppType;
            this.machine = outputMachine;
            this.path = outputPath;
            this.assemblyName = assemblyName;
            this.win32IconPath = outputWin32IconPath;
            this.mainAssembly = mainAssembly;
            this.apartmentState = outputApartmentState;
            this.grabResources = grabResources;
            this.compilerOptions = compilerOptions;
            this.compilerVersionRequired = compilerVersionRequired;
            this.appConfigFileId = appConfigFileId;
            this.useShadowCopying = useShadowCopying;
        }
    }
}
