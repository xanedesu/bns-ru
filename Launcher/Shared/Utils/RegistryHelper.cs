using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Unlakki.Bns.Launcher.Shared.Exceptions;
using Unlakki.Bns.Launcher.Shared.Extensions;
using Unlakki.Bns.Launcher.Shared.Services.Interfaces;

namespace Unlakki.Bns.Launcher.Shared.Utils
{
    [Export(typeof(ILauncherInSystemRegistrator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public static class RegistryHelper
    {
        private static readonly RegistryKey _defaultRootKey = Registry.LocalMachine;

        private static readonly char[] _quotes = new char[2] { '\'', '"' };

        private static readonly string _uninstallBaseFolder = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public static bool TryRegisterUninstallInfo(string appKey, RegistryUninstallInfo info)
        {
            try
            {
                string uninstallRegistryFolder = GetUninstallRegistryFolder(appKey);

                using (RegistryKey registryKey1 = _defaultRootKey.OpenSubKey(
                    uninstallRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(uninstallRegistryFolder))
                {
                    if (registryKey1 == null)
                    {
                        return false;
                    }

                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.DisplayName, info.Name ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.ApplicationVersion, info.Version ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.Publisher, info.Publisher);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.ShortcutName, info.Name ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.InstallLocation,
                      info.InstallationPath ?? string.Empty);
                    registryKey1.SetValue(RegistryConstants.Uninstall.DisplayIcon,
                        info.IconPath ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.UninstallString,
                        info.UninstallCommand ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.DisplayVersion, info.Version ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.InstallationDate,
                        info.InstallationDate.ToString(RegistryConstants.Uninstall.RegistryDateFormat));

                    long? sizeBytes = info.SizeBytes;

                    if (sizeBytes.HasValue)
                    {
                        RegistryKey registryKey2 = registryKey1;
                        string estimatedSize = RegistryConstants.Uninstall.EstimatedSize;
                        sizeBytes = info.SizeBytes;
                        long num = 1024;
                        ValueType local = (int)(sizeBytes.HasValue
                            ? new long?(sizeBytes.GetValueOrDefault() / num)
                            : new long?()).Value;
                        registryKey2.SetValue(estimatedSize, local, RegistryValueKind.DWord);
                    }

                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.NoModify, 1, RegistryValueKind.DWord);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.NoRepair, 1, RegistryValueKind.DWord);
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static bool TryUpdateUninstallInfo(string appKey, RegistryUninstallInfo info)
        {
            try
            {
                string uninstallRegistryFolder = GetUninstallRegistryFolder(appKey);

                using (RegistryKey registryKey1 = _defaultRootKey.OpenSubKey(
                    uninstallRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(uninstallRegistryFolder))
                {
                    if (registryKey1 == null)
                    {
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(info.Name))
                    {
                        registryKey1.SetValue(
                          RegistryConstants.Uninstall.DisplayName, info.Name ?? string.Empty);
                        registryKey1.SetValue(
                          RegistryConstants.Uninstall.ShortcutName, info.Name ?? string.Empty);
                    }

                    if (!string.IsNullOrWhiteSpace(info.Version))
                    {
                        registryKey1.SetValue(
                            RegistryConstants.Uninstall.ApplicationVersion,
                            info.Version ?? string.Empty);
                        registryKey1.SetValue(
                            RegistryConstants.Uninstall.DisplayVersion, info.Version ?? string.Empty);
                    }

                    if (!string.IsNullOrWhiteSpace(info.Publisher))
                    {
                        registryKey1.SetValue(RegistryConstants.Uninstall.Publisher, info.Publisher);
                    }

                    if (!string.IsNullOrWhiteSpace(info.IconPath))
                    {
                        registryKey1.SetValue(RegistryConstants.Uninstall.DisplayIcon, info.IconPath);
                    }

                    if (!string.IsNullOrWhiteSpace(info.UninstallCommand))
                    {
                        registryKey1.SetValue(
                          RegistryConstants.Uninstall.UninstallString, info.UninstallCommand);
                    }

                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.LastUpdateDate,
                        info.InstallationDate.ToString(RegistryConstants.Uninstall.RegistryDateFormat));

                    if (info.SizeBytes.HasValue)
                    {
                        RegistryKey registryKey2 = registryKey1;
                        string estimatedSize = RegistryConstants.Uninstall.EstimatedSize;
                        long? sizeBytes = info.SizeBytes;
                        long num = 1024;
                        ValueType local = (int)(sizeBytes.HasValue
                            ? new long?(sizeBytes.GetValueOrDefault() / num)
                            : new long?()).Value;
                        registryKey2.SetValue(estimatedSize, local, RegistryValueKind.DWord);
                    }

                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.NoModify, 1, RegistryValueKind.DWord);
                    registryKey1.SetValue(
                        RegistryConstants.Uninstall.NoRepair, 1, RegistryValueKind.DWord);
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static RegistryUninstallInfo GetUninstallInfo(string key)
        {
            string uninstallRegistryFolder = GetUninstallRegistryFolder(key);

            using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(
                uninstallRegistryFolder, true)
                ?? _defaultRootKey.CreateSubKey(uninstallRegistryFolder))
            {
                if (registryKey == null)
                {
                    throw new InvalidOperationException(
                        $"Can't get uninstall info node {uninstallRegistryFolder}");
                }

                DateTime result1;
                DateTime.TryParseExact(
                    registryKey.GetValue(RegistryConstants.Uninstall.InstallationDate)?.ToString(),
                    RegistryConstants.Uninstall.RegistryDateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result1);

                int result2;
                int.TryParse(
                    registryKey.GetValue(RegistryConstants.Uninstall.EstimatedSize)?.ToString() ?? "0",
                    out result2);

                return new RegistryUninstallInfo {
                    Version = registryKey.GetValue(
                        RegistryConstants.Uninstall.ApplicationVersion)?.ToString(),
                    Name = registryKey.GetValue(RegistryConstants.Uninstall.DisplayName)?.ToString(),
                    IconPath = registryKey.GetValue(
                        RegistryConstants.Uninstall.DisplayIcon)?.ToString(),
                    InstallationPath = registryKey.GetValue(
                        RegistryConstants.Uninstall.InstallLocation)?.ToString(),
                    InstallationDate = result1,
                    UninstallCommand = registryKey.GetValue(
                        RegistryConstants.Uninstall.UninstallString)?.ToString(),
                    Publisher = registryKey.GetValue(
                        RegistryConstants.Uninstall.Publisher)?.ToString(),
                    SizeBytes = new long?(result2 * 1024)
                };
            }
        }

        public static void DeleteUninstallInfo(string appKey)
        {
            try
            {
                string uninstallRegistryFolder = GetUninstallRegistryFolder(appKey);
                _defaultRootKey.DeleteSubKeyTree(uninstallRegistryFolder, false);
            }
            catch (BadRegistryPathPart)
            {
            }
        }

        public static bool UninstallInfoExists(string appKey)
        {
            try
            {
                string uninstallRegistryFolder = GetUninstallRegistryFolder(appKey);

                using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(uninstallRegistryFolder))
                {
                    return registryKey != null;
                }
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static List<string> GetUninstallInfoKeys(string prefix)
        {
            using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(_uninstallBaseFolder))
            {
                return registryKey == null
                    ? new List<string>()
                    : ((IEnumerable<string>)registryKey.GetSubKeyNames())
                    .Where((e => e.StartsWith(prefix))).ToList();
            }
        }

        public static bool TryRegisterUrlScheme(string schemeName, string schemeExePath)
        {
            try
            {
                string schemeRegistryFolder = GetUrlSchemeRegistryFolder(schemeName);

                using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(schemeRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(schemeRegistryFolder))
                {
                    if (registryKey == null)
                    {
                        return false;
                    }

                    registryKey.SetValue(string.Empty, $"URL:{schemeName}");
                    registryKey.SetValue(RegistryConstants.UrlScheme.UrlProtocolKey, string.Empty);

                    using (RegistryKey subKey = registryKey.CreateSubKey(
                        RegistryConstants.UrlScheme.DefaultIconKey))
                    {
                        if (subKey == null)
                        {
                            return false;
                        }

                        subKey.SetValue(string.Empty, ($"{Path.GetFileName(schemeExePath)},1"));
                    }

                    using (RegistryKey subKey = registryKey.CreateSubKey(@"shell\open\command"))
                    {
                        if (subKey == null)
                        {
                            return false;
                        }

                        subKey.SetValue(string.Empty, ($"\"{schemeExePath}\" %1"));
                    }
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static void DeleteUrlScheme(string schemeName)
        {
            try
            {
                string schemeRegistryFolder = GetUrlSchemeRegistryFolder(schemeName);
                _defaultRootKey.DeleteSubKeyTree(schemeRegistryFolder, false);
            }
            catch (BadRegistryPathPart)
            {
            }
        }

        public static UrlSchemeInfo GetUrlSchemeInfo(string schemeName)
        {
            UrlSchemeInfo urlSchemeInfo = GetUrlSchemeInfo(Registry.LocalMachine, schemeName);
            return GetUrlSchemeInfo(Registry.CurrentUser, schemeName) ?? urlSchemeInfo;
        }

        public static RegisterLauncherSoftwareInfo GetRegisterLauncherSoftwareData(
            string publisher,
            string launcherKey)
        {
            string launcherRegistryFolder = GetSoftwareLauncherRegistryFolder(publisher, launcherKey);

            using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(launcherRegistryFolder, true)
                ?? _defaultRootKey.CreateSubKey(launcherRegistryFolder))
            {
                if (registryKey == null)
                {
                    throw new InvalidOperationException(
                        $"Can't get software info node {launcherRegistryFolder}");
                }

                string installationDate = registryKey.GetValue(
                    RegistryConstants.Software.InstallationDate)?.ToString();
                object obj = registryKey.GetValue(RegistryConstants.Software.OldGamesTaken);
                string lastGamesInstallDirectory = registryKey.GetValue(
                    RegistryConstants.Software.LastGamesInstallDirectory)?.ToString();
                string launcherRegion = registryKey.GetValue(
                    RegistryConstants.Software.LauncherRegion)?.ToString();

                return new RegisterLauncherSoftwareInfo {
                    Version = registryKey.GetValue(RegistryConstants.Software.Version)?.ToString(),
                    LauncherKey = launcherKey,
                    InstallationPath = registryKey.GetValue(
                        RegistryConstants.Software.Path)?.ToString(),
                    Publisher = publisher,
                    LauncherId = registryKey.GetValue(
                        RegistryConstants.Software.LauncherId)?.ToString(),
                    OldGamesTaken = new bool?(obj != null && (int)obj == 1),
                    LastGamesInstallDirectory = lastGamesInstallDirectory,
                    LauncherRegion = launcherRegion,
                    InstallationDate = ParseRegistryDate(
                        installationDate, RegistryConstants.Format.DateFormat)
                };
            }
        }

        public static bool TryRegisterLauncherSoftwareData(RegisterLauncherSoftwareInfo softwareInfo)
        {
            try
            {
                string launcherRegistryFolder = GetSoftwareLauncherRegistryFolder(
                    softwareInfo.Publisher, softwareInfo.LauncherKey);

                using (RegistryKey registryKey1 = _defaultRootKey.OpenSubKey(
                    launcherRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(launcherRegistryFolder))
                {
                    if (registryKey1 == null)
                    {
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(softwareInfo.Version))
                    {
                        registryKey1.SetValue(
                            RegistryConstants.Software.Version, softwareInfo.Version ?? string.Empty);
                    }

                    DateTime? nullable;
                    if (softwareInfo.InstallationDate.HasValue)
                    {
                        RegistryKey registryKey2 = registryKey1;
                        string installationDate = RegistryConstants.Software.InstallationDate;
                        nullable = softwareInfo.InstallationDate;
                        ref DateTime? local = ref nullable;
                        string str = (local.HasValue
                            ? local.GetValueOrDefault().ToString(RegistryConstants.Format.DateFormat)
                            : null) ?? string.Empty;
                        registryKey2.SetValue(installationDate, str);
                    }

                    if (softwareInfo.OldGamesTaken.HasValue)
                    {
                        RegistryKey registryKey2 = registryKey1;
                        string oldGamesTaken1 = RegistryConstants.Software.OldGamesTaken;
                        bool? oldGamesTaken2 = softwareInfo.OldGamesTaken;
                        bool flag = true;
                        ValueType local = oldGamesTaken2
                            .GetValueOrDefault() == flag & oldGamesTaken2.HasValue ? 1 : 0;
                        registryKey2.SetValue(oldGamesTaken1, local);
                    }

                    if (!string.IsNullOrWhiteSpace(softwareInfo.InstallationPath))
                    {
                        registryKey1.SetValue(
                            RegistryConstants.Software.Path,
                            softwareInfo.InstallationPath ?? string.Empty);
                    }

                    nullable = softwareInfo.LastUpdateDate;

                    if (nullable.HasValue)
                    {
                        RegistryKey registryKey2 = registryKey1;
                        string lastUpdateDate = RegistryConstants.Software.LastUpdateDate;
                        nullable = softwareInfo.LastUpdateDate;
                        ref DateTime? local = ref nullable;
                        string str = (local.HasValue
                            ? local.GetValueOrDefault().ToString(RegistryConstants.Format.DateFormat)
                            : null) ?? string.Empty;
                        registryKey2.SetValue(lastUpdateDate, str);
                    }

                    if (registryKey1.GetValue(RegistryConstants.Software.LauncherId) == null
                        && !string.IsNullOrWhiteSpace(softwareInfo.LauncherId))
                    {
                        string launcherId = softwareInfo.LauncherId;
                        registryKey1.SetValue(RegistryConstants.Software.LauncherId, launcherId);
                    }

                    if (!string.IsNullOrWhiteSpace(softwareInfo.LastGamesInstallDirectory))
                    {
                        registryKey1.SetValue(
                            RegistryConstants.Software.LastGamesInstallDirectory,
                            softwareInfo.LastGamesInstallDirectory ?? string.Empty);
                    }

                    if (!string.IsNullOrWhiteSpace(softwareInfo.LauncherRegion))
                    {
                        registryKey1.SetValue(
                            RegistryConstants.Software.LauncherRegion,
                            softwareInfo.LauncherRegion ?? string.Empty);
                    }
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static bool TrySetLauncherIdIfNotExist(
            string publisher,
            string launcherKey,
            string launcherId)
        {
            try
            {
                string launcherRegistryFolder = GetSoftwareLauncherRegistryFolder(
                    publisher, launcherKey);

                using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(
                    launcherRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(launcherRegistryFolder))
                {
                    if (registryKey == null)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(
                        registryKey.GetValue(RegistryConstants.Software.LauncherId)?.ToString()))
                    {
                        registryKey.SetValue(RegistryConstants.Software.LauncherId, launcherId);
                    }
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static bool TryRegisterGameSoftwareData(RegisterGameSoftwareInfo softwareInfo)
        {
            try
            {
                string gameRegistryFolder = GetSoftwareGameRegistryFolder(
                    softwareInfo.Publisher, softwareInfo.LauncherKey, softwareInfo.GameName);

                using (RegistryKey registryKey1 = _defaultRootKey.OpenSubKey(gameRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(gameRegistryFolder))
                {
                    if (registryKey1 == null)
                    {
                        return false;
                    }

                    registryKey1.SetValue(
                        RegistryConstants.Software.Path,
                        softwareInfo.InstallationPath ?? string.Empty);
                    registryKey1.SetValue(
                        RegistryConstants.Software.Version, (softwareInfo.Version ?? string.Empty));
                    RegistryKey registryKey2 = registryKey1;
                    string installationDate1 = RegistryConstants.Software.InstallationDate;
                    DateTime? installationDate2 = softwareInfo.InstallationDate;
                    ref DateTime? local = ref installationDate2;
                    string str = (local.HasValue
                        ? local.GetValueOrDefault().ToString(RegistryConstants.Format.DateFormat)
                        : null) ?? string.Empty;
                    registryKey2.SetValue(installationDate1, str);
                }

                return true;
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        public static RegisterGameSoftwareInfo GetGameSoftwareData(
            string publisher,
            string launcherKey,
            string gameName)
        {
            try
            {
                string gameRegistryFolder = GetSoftwareGameRegistryFolder(
                    publisher, launcherKey, gameName);

                using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(gameRegistryFolder, true))
                {
                    if (registryKey == null)
                    {
                        return null;
                    }

                    string str = registryKey.GetValue(
                        RegistryConstants.Software.InstallationDate)?.ToString();

                    return new RegisterGameSoftwareInfo {
                        Publisher = publisher,
                        LauncherKey = launcherKey,
                        InstallationPath = registryKey.GetValue(
                            RegistryConstants.Software.Path)?.ToString(),
                        Version = registryKey.GetValue(
                            RegistryConstants.Software.Version)?.ToString(),
                        InstallationDate = new DateTime?(
                            ParseRegistryDate(str, RegistryConstants.Format.DateFormat)
                            ?? DateTime.Now)
                    };
                }
            }
            catch (BadRegistryPathPart)
            {
                return null;
            }
        }

        public static void DeleteGameSoftwareData(DeleteGameSoftwareInfo softwareInfo)
        {
            try
            {
                string gameRegistryFolder = GetSoftwareGameRegistryFolder(
                    softwareInfo.Publisher, softwareInfo.LauncherKey, softwareInfo.GameName);
                _defaultRootKey.DeleteSubKeyTree(gameRegistryFolder, false);
            }
            catch (BadRegistryPathPart)
            {
            }
        }

        public static DateTime? GetLastErrorCheckDate(string publisher, string launcherKey)
        {
            string launcherRegistryFolder = GetSoftwareLauncherRegistryFolder(publisher, launcherKey);

            using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(launcherRegistryFolder, true)
                ?? _defaultRootKey.CreateSubKey(launcherRegistryFolder))
            {
                if (registryKey == null)
                {
                    throw new InvalidOperationException(
                        $"Can't get software info node {launcherRegistryFolder}");
                }

                return ParseRegistryDate(
                    registryKey.GetValue(RegistryConstants.Software.LastErrorCheckDate)?.ToString(),
                    RegistryConstants.Format.DateTimeFormat);
            }
        }

        public static bool TryUpdateLastErrorCheckDate(
            string publisher,
            string launcherKey,
            DateTime dateTime)
        {
            try
            {
                string launcherRegistryFolder = GetSoftwareLauncherRegistryFolder(
                    publisher, launcherKey);

                using (RegistryKey registryKey = _defaultRootKey.OpenSubKey(
                    launcherRegistryFolder, true)
                    ?? _defaultRootKey.CreateSubKey(launcherRegistryFolder))
                {
                    if (registryKey == null)
                    {
                        return false;
                    }

                    registryKey.SetValue(
                        RegistryConstants.Software.LastErrorCheckDate,
                        dateTime.ToString(RegistryConstants.Format.DateTimeFormat));

                    return true;
                }
            }
            catch (BadRegistryPathPart)
            {
                return false;
            }
        }

        private static UrlSchemeInfo GetUrlSchemeInfo(
            RegistryKey rootKey,
            string schemeName)
        {
            string schemeRegistryFolder = GetUrlSchemeRegistryFolder(schemeName);

            using (RegistryKey registryKey = rootKey.OpenSubKey(schemeRegistryFolder, true))
            {
                if (registryKey == null)
                {
                    return null;
                }

                UrlSchemeInfo urlSchemeInfo = new UrlSchemeInfo {
                    SchemeName = schemeName,
                    ApplicationName = schemeName
                };

                using (RegistryKey subKey = registryKey.CreateSubKey(
                  RegistryConstants.UrlScheme.DefaultIconKey))
                {
                    if (subKey == null)
                    {
                        return null;
                    }

                    string str = subKey
                        .GetValue(string.Empty)
                        .ToString()
                        .Split(',')[0]
                        .RemoveLeadingSymbols(_quotes);

                    urlSchemeInfo.ExePath = str;

                    if (File.Exists(str))
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(str);
                        urlSchemeInfo.ApplicationName = versionInfo.FileDescription;
                    }
                }

                return urlSchemeInfo;
            }
        }

        private static string GetValidPathPart(string partToClear)
        {
            if (partToClear == null)
            {
                throw new BadRegistryPathPart(partToClear);
            }

            string part = partToClear;

            foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                partToClear = partToClear.Replace(invalidFileNameChar.ToString(), string.Empty);
            }

            if (string.IsNullOrWhiteSpace(partToClear))
            {
                throw new BadRegistryPathPart(part);
            }

            return partToClear;
        }

        private static string GetUninstallRegistryFolder(string key)
        {
            string validPathPart = GetValidPathPart(key);
            return $@"{_uninstallBaseFolder}\{validPathPart}";
        }

        private static string GetUrlSchemeRegistryFolder(string schemeName)
        {
            return $@"Software\Classes\{GetValidPathPart(schemeName)}";
        }

        private static string GetSoftwareLauncherRegistryFolder(string publisher, string launcherKey)
        {
            string validPathPart = GetValidPathPart(launcherKey);
            return $@"SOFTWARE\{publisher}\{validPathPart}";
        }

        private static string GetSoftwareGameRegistryFolder(
            string publisher,
            string launcherKey,
            string gameName)
        {
            return Path.Combine(
                GetSoftwareGamesRegistryFolder(publisher, launcherKey),
                GetValidPathPart(gameName) ?? "");
        }

        private static string GetSoftwareGamesRegistryFolder(string publisher, string launcherKey)
        {
            return Path.Combine(GetSoftwareLauncherRegistryFolder(publisher, launcherKey), "Games");
        }

        private static DateTime? ParseRegistryDate(string value, string format)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new DateTime?();
            }

            DateTime result;
            return !DateTime.TryParseExact(
                value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                ? new DateTime?()
                : new DateTime?(result);
        }
    }
}
