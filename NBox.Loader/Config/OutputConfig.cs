using System.Collections.Generic;
using System.Threading;

namespace NBox.Config
{
    public sealed class OutputConfig
    {
        private readonly OutputAppType outputAppType;

        public OutputAppType OutputAppType {
            get {
                return (outputAppType);
            }
        }

        private readonly OutputMachine outputMachine;

        public OutputMachine OutputMachine {
            get {
                return (outputMachine);
            }
        }

        private readonly string outputPath;

        public string OutputPath {
            get {
                return (outputPath);
            }
        }

        private readonly string outputWin32IconPath;

        public string OutputWin32IconPath {
            get {
                return (outputWin32IconPath);
            }
        }

        private readonly IncludedAssemblyConfig mainAssembly;

        public IncludedAssemblyConfig MainAssembly {
            get {
                return (mainAssembly);
            }
        }

        private readonly ApartmentState outputApartmentState;

        public ApartmentState OutputApartmentState {
            get {
                return (outputApartmentState);
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

        public OutputConfig(OutputAppType outputAppType, OutputMachine outputMachine, string outputPath, string outputWin32IconPath, IncludedAssemblyConfig mainAssembly, ApartmentState outputApartmentState) {
            this.outputAppType = outputAppType;
            this.outputMachine = outputMachine;
            this.outputPath = outputPath;
            this.outputWin32IconPath = outputWin32IconPath;
            this.mainAssembly = mainAssembly;
            this.outputApartmentState = outputApartmentState;
        }
    }
}
