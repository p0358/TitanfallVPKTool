using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ValvePak;

namespace ValvePakTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var package = new Package();
            package.Read(args[0]);

            ulong compressedSum = 0;
            ulong originalSum = 0;
            ulong highestUncompressed = 0;
            ulong highestUncompressedNotSound = 0;
            string highestUncompressedString = "";
            ulong uncompressedSoundSize = 0;

            foreach (var entry in package.Entries)
            {
                //System.Console.Out.WriteLine(entry.Value);
                foreach (var pkgentry in entry.Value)
                {
                    System.Console.Out.WriteLine(pkgentry.ToString());
                    compressedSum += pkgentry.Length;
                    originalSum += pkgentry.OriginalLength;
                    if (pkgentry.Length == pkgentry.OriginalLength && pkgentry.Length > highestUncompressed)
                    {
                        highestUncompressed = pkgentry.Length;
                        //highestUncompressedString = $"{pkgentry.GetFullPath()}";
                    }

                    if (!pkgentry.GetFullPath().StartsWith("sound/") && pkgentry.Length == pkgentry.OriginalLength && pkgentry.Length > highestUncompressedNotSound)
                    {
                        highestUncompressedNotSound = pkgentry.Length;
                        highestUncompressedString = $"{pkgentry.GetFullPath()}";
                    }

                    if (pkgentry.Length == pkgentry.OriginalLength && pkgentry.GetFullPath().StartsWith("sound/"))
                    {
                        uncompressedSoundSize += pkgentry.Length;
                    }
                }
            }

            //float prc = (compressedSum / originalSum) * 100;
            //string prcs = prc.ToString();
            string prcs = (compressedSum / originalSum).ToString("0.00%");
            System.Console.Out.WriteLine($"{compressedSum} / {originalSum} = {prcs}, {highestUncompressedString} - {highestUncompressedNotSound}, uncompressedSoundSize = {uncompressedSoundSize}");

        }
    }
}
