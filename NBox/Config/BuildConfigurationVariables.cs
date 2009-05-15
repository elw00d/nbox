using NBox.Utils;

namespace NBox.Config
{
    public struct BuildConfigurationVariables
    {
        private readonly string configFileDirectory;
        
        public string ConfigFileDirectory {
            get {
                return (configFileDirectory);
            }
        }

        private readonly string rootDirectory;

        public string RootDirectory {
            get {
                return (rootDirectory);
            }
        }

        public string GetVariableByAlias(string alias) {
            ArgumentChecker.NotNullOrEmpty(alias, "alias");
            //
            if (alias == "%configdir%") {
                return (configFileDirectory);
            }
            if (alias == "%rootdir%") {
                return (rootDirectory);
            }
            //
            return (null);
        }

        public string this[string index] {
            get {
                return (GetVariableByAlias(index));
            }
        }

        public BuildConfigurationVariables(string configFileDirectory, string rootDirectory) {
#if !LOADER
            ArgumentChecker.NotNullOrEmptyExistsDirectoryPath(configFileDirectory, "configFileDirectory");
            ArgumentChecker.NotNullOrEmptyExistsDirectoryPath(rootDirectory, "rootDirectory");
#endif
            //
            this.configFileDirectory = configFileDirectory;
            this.rootDirectory = rootDirectory;
        }
    }
}