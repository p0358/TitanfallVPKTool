﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValvePak;
using ValveResourceFormat;

namespace GUI.Utils
{
    internal static class FileExtensions
    {
        private static readonly Dictionary<string, Package> CachedPackages = new Dictionary<string, Package>();
        private static readonly Dictionary<string, Resource> CachedResources = new Dictionary<string, Resource>();

        // http://stackoverflow.com/a/4975942/272647
        public static string ToFileSizeString(this ulong byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            string result;

            if (byteCount == 0)
            {
                result = string.Format("{0} {1}", byteCount, suf[0]);
            }
            else
            {
                var absoluteByteCount = Math.Abs((long)byteCount);
                var place = Convert.ToInt32(Math.Floor(Math.Log(absoluteByteCount, 1024)));
                var num = Math.Round(absoluteByteCount / Math.Pow(1024, place), 1);
                result = string.Format("{0} {1}", Math.Sign((long)byteCount) * num, suf[place]);
                // System.Windows.Forms.MessageBox.Show("Test: " + byteCount + ", " + Convert.ToInt64(byteCount) + ", " + ((long)byteCount)); -- all give the same result, so we're good
            }

            return result;
        }

        public static string ToFileSizeString(this uint byteCount)
        {
            return ToFileSizeString((ulong)byteCount);
        }

        public static void ClearCache()
        {
            foreach (var res in CachedResources)
            {
                res.Value.Dispose();
            }

            CachedResources.Clear();
        }

        public static Resource LoadFileByAnyMeansNecessary(string file, string currentFullPath, Package currentPackage)
        {
            // TODO: Might conflict where same file name is available in different paths
            if (CachedResources.TryGetValue(file, out var resource) && resource.Reader != null)
            {
                return resource;
            }

            resource = new Resource();

            var entry = currentPackage?.FindEntry(file);

            if (entry != null)
            {
                currentPackage.ReadEntry(entry, out var output);
                resource.Read(new MemoryStream(output));
                CachedResources[file] = resource;

                return resource;
            }

            var paths = Settings.Config.GameSearchPaths.ToList();
            var packages = new List<Package>();

            foreach (var searchPath in paths.Where(searchPath => searchPath.EndsWith(".vpk")).ToList())
            {
                paths.Remove(searchPath);

                if (!CachedPackages.TryGetValue(searchPath, out var package))
                {
                    Console.WriteLine("Preloading vpk {0}", searchPath);

                    package = new Package();
                    package.Read(searchPath);
                    CachedPackages[searchPath] = package;
                }

                packages.Add(package);
            }

            foreach (var package in packages)
            {
                entry = package?.FindEntry(file);

                if (entry != null)
                {
                    package.ReadEntry(entry, out var output);
                    resource.Read(new MemoryStream(output));
                    CachedResources[file] = resource;

                    return resource;
                }
            }

            var path = FindResourcePath(paths, file, currentFullPath);

            if (path == null)
            {
                return null;
            }

            resource.Read(path);
            CachedResources[file] = resource;

            return resource;
        }

        private static string FindResourcePath(IList<string> paths, string file, string currentFullPath = null)
        {
            if (currentFullPath != null)
            {
                paths = paths.OrderByDescending(x => currentFullPath.StartsWith(x, StringComparison.Ordinal)).ToList();
            }

            foreach (var searchPath in paths)
            {
                var path = Path.Combine(searchPath, file);
                path = Path.GetFullPath(path);

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
