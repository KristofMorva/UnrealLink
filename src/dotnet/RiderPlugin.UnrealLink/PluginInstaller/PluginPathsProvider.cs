﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Components;
using JetBrains.Extension;
using JetBrains.Util;
using Newtonsoft.Json.Linq;

namespace RiderPlugin.UnrealLink.PluginInstaller
{
    [ShellComponent]
    public class PluginPathsProvider
    {
        private readonly ApplicationPackages myApplicationPackages;
        private readonly IDeployedPackagesExpandLocationResolver myResolver;
        private readonly ILogger myLogger;

        private static readonly string EditorPluginFile = "RiderLink.zip";
        public readonly FileSystemPath PathToPackedPlugin;
        public readonly byte[] CurrentPluginChecksum;
        public static readonly byte[] NullChecksum = { 0 };

        public PluginPathsProvider(ApplicationPackages applicationPackages,
            IDeployedPackagesExpandLocationResolver resolver, ProductSettingsLocation productSettingsLocation,
            ILogger logger)
        {
            myApplicationPackages = applicationPackages;
            myResolver = resolver;
            myLogger = logger;
            PathToPackedPlugin = GetEditorPluginPathFile(productSettingsLocation);
            CurrentPluginChecksum = GetCurrentPluginChecksum();
        }

        private FileSystemPath GetEditorPluginPathFile(ProductSettingsLocation productSettingsLocation)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var package = myApplicationPackages.FindPackageWithAssembly(assembly, OnError.LogException);
            var installDirectory = myResolver.GetDeployedPackageDirectory(package);
            var editorPluginPathDirectory = installDirectory != productSettingsLocation.InstallDir
              ? installDirectory.Parent.Combine("EditorPlugin")
              : installDirectory;
            return editorPluginPathDirectory.Combine(EditorPluginFile);
        }

        private byte[] GetCurrentPluginChecksum()
        {
            var editorPluginPathFile = PathToPackedPlugin;
            using var zipArchive = ZipFile.OpenRead(editorPluginPathFile.FullPath);
            var zipArchiveEntry = zipArchive.GetEntry(UnrealPluginDetector.CHEKCSUM_ENTRY_PATH);
            var stream = zipArchiveEntry?.Open();
            return stream?.ReadAllBytes();
        }

        public byte[] GetPluginChecksum(VirtualFileSystemPath checksumPath)
        {
            if (!checksumPath.ExistsFile)
            {
                return NullChecksum;
            }
            try
            {
                return File.ReadAllBytes(checksumPath.FullPath);
            }
            catch (Exception exception)
            {
                myLogger.Error(exception, "[UnrealLink]: Couldn't read RiderLink plugin version");
                return null;
            }
        }
    }
}