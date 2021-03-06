/*
    Copyright (c) 2018 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.IO;
using Klocman.Extensions;
using Klocman.Tools;
using UninstallTools.Factory.InfoAdders;
using UninstallTools.Properties;

namespace UninstallTools.Factory
{
    public class OculusFactory : IIndependantUninstallerFactory
    {
        private static bool? _helperAvailable;
        private static string HelperPath => Path.Combine(UninstallToolsGlobalConfig.AssemblyLocation, @"OculusHelper.exe");

        private static bool HelperAvailable
        {
            get
            {
                if (!_helperAvailable.HasValue)
                    _helperAvailable = WindowsTools.CheckNetFramework4Installed(true) != null && File.Exists(HelperPath);

                return _helperAvailable.Value;
            }
        }

        public IEnumerable<ApplicationUninstallerEntry> GetUninstallerEntries(ListGenerationProgress.ListGenerationCallback progressCallback)
        {
            if (!HelperAvailable) yield break;

            var output = FactoryTools.StartHelperAndReadOutput(HelperPath, "/query");
            if (string.IsNullOrEmpty(output))
                yield break;

            foreach (var data in FactoryTools.ExtractAppDataSetsFromHelperOutput(output))
            {
                if (!data.ContainsKey("CanonicalName")) continue;
                var name = data["CanonicalName"];
                if (string.IsNullOrEmpty(name)) continue;

                var uninstallStr = $"\"{HelperPath}\" /uninstall {name}";

                var entry = new ApplicationUninstallerEntry
                {
                    RatingId = name,
                    //RegistryKeyName = name,
                    UninstallString = uninstallStr,
                    QuietUninstallString = uninstallStr,
                    IsValid = true,
                    UninstallerKind = UninstallerType.Oculus,
                    InstallLocation = data["InstallLocation"],
                    DisplayVersion = data["Version"],
                    IsProtected = "true".Equals(data["IsCore"], StringComparison.OrdinalIgnoreCase),
                };

                var executable = data["LaunchFile"];
                if (File.Exists(executable))
                {
                    ExecutableAttributeExtractor.FillInformationFromFileAttribs(entry, executable, true);
                    entry.DisplayIcon = executable;
                }

                if (Directory.Exists(entry.InstallLocation))
                    entry.InstallDate = Directory.GetCreationTime(entry.InstallLocation);

                if (string.IsNullOrEmpty(entry.RawDisplayName))
                    entry.RawDisplayName = name.Replace('-', ' ').ToTitleCase();

                yield return entry;
            }
        }

        public bool IsEnabled() => UninstallToolsGlobalConfig.ScanOculus;
        public string DisplayName => Localisation.Progress_AppStores_Oculus;
    }
}