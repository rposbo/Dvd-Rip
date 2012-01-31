using System.Linq;
using System.IO;
using BusinessLogic;
using System.Collections.Generic;
using System.Management;
using System.Timers;
using Entity;
using System;

namespace DvdRip
{
    class Program
    {
        private static string ripRootPath = @"C:\Videos\_ripRoot\";
        private static string dvdDecrypterPath = @"C:\Program Files\DVD Decrypter\dvddecrypter.exe";
        private static string mencoderPath = @"C:\Program Files\mplayer\mencoder.exe";
        private static string vstripPath = @"C:\Program Files\AutoGK\tools\vstrip_ifo.exe";

        private static fileUtilitiesProvider fileUtilities;
        private static List<Pgc> pgcs;
        private static DriveInfo dvdDrive;

        static void Main()
        {
            try
            {                
                Console.WriteLine("** Started **");
                
                GetDvdDrive();
                CreateStartFolder();
                //ProcessIfo();
                //RipBiggestPgc();
                //ConvertPgc();
                RemoveTemporaryFiles();

                Console.ReadKey();

            }
            catch(Exception e)
            {
                Console.WriteLine(string.Format("Error: {0}", e.Message));
                Console.ReadKey();
            }
        }

        private static void CreateStartFolder()
        {
            var newFolder = string.Format(@"{0}\{1}", ripRootPath, dvdDrive.VolumeLabel);
            fileUtilities = new fileUtilitiesProvider(newFolder, dvdDrive, dvdDecrypterPath, mencoderPath, vstripPath);

            if (!Directory.Exists(newFolder))
                Directory.CreateDirectory(newFolder);

            Console.WriteLine(" - Start directory created");
        }

        private static void ProcessIfo()
        {
            fileUtilities.scanIfo();
            pgcs = fileUtilities.processIfo();
            fileUtilities.SavePgcs(pgcs);
            Console.WriteLine(" - Ifo Processed");
        }

        private static void RipBiggestPgc()
        {
            pgcs.Sort();
            fileUtilities.ripPgc(pgcs[0]);
            Console.WriteLine(" - Dvd ripped - safe to eject the disc");
        }

        private static void ConvertPgc()
        {
            fileUtilities.convertPgc();
            Console.WriteLine(" - Conversion complete");
        }

        private static void RemoveTemporaryFiles()
        {
            fileUtilities.RemoveTemporaryFiles();
            Console.WriteLine(" - Directory Cleaned");
        }

        private static void GetDvdDrive()
        {
            dvdDrive = (from drive in DriveInfo.GetDrives()
                        where drive.DriveType == DriveType.CDRom && drive.IsReady
                        select drive).FirstOrDefault();

            Console.WriteLine(string.Format(" - Found drive: {0}", dvdDrive.VolumeLabel));
        }
    }
}
