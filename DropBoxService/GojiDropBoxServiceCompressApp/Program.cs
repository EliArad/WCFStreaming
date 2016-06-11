using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompresFiles;
using System.IO;
using Ionic.Zip;

namespace FebrisCompressApp
{
    class Program
    {

        public class Listener
        {
            int m_fileCount = 0;
            bool m_firstTime = true;
            int m_compressCounter = 0;
            public void Subscribe()
            {
                FebrisInstaller.Tick += new FebrisInstaller.ZipProgress(HeardIt);
            }

            private void HeardIt(int count, string fileName)
            {

                if (m_firstTime == true)
                {
                    Console.WriteLine("Total files to compress: " + count);
                    m_fileCount = count;
                    // progressbar.maximum = count;
                    // progressbar.value = 0;
                    m_firstTime = false;
                }
                else
                {
                    m_compressCounter += count;
                    Console.WriteLine("Adding files {0}, {1}", m_compressCounter, fileName);

                    // progressbar.value++;
                }
            }
        }
        static void Release(string version, bool overwrite, string comVer)
        {
            try
            {
                string[] releaseDirectories = {
               @"C:\GojiLTDTrunk\WCF Streaming\GojiDropBoxService\GojiDropBoxServiceInstaller\bin\Release\",                           
            };

                string[] installerName = { "GojiDropBoxServiceInstaller",                                     
                                     };
                for (int i = 0; i < releaseDirectories.Length; i++)
                {
                    using (ZipFile zip = new ZipFile())
                    {
                        foreach (var file in Directory.GetFiles(releaseDirectories[i]))
                        {
                            if (Path.GetExtension(file).ToLower() == ".tmp")
                            {
                                File.Delete(file);
                                continue;
                            }
                            if (Path.GetExtension(file).ToLower() == ".pdb")
                            {
                                File.Delete(file);
                            }
                        }
                        zip.AddDirectory(releaseDirectories[i], "");
                        zip.Comment = "Goji DropBox Service Version:" + version;
                        zip.Save(installerName[i] + "_" + comVer + "_" + version + ".zip");
                    }
                }


                Console.WriteLine(@"Copy the release to Directory Z:\Rnd\ads\Software\Eli\Febris\Version1.1\Installer\GojiDropBoxService\" + version);
                string finalReleaseDirectory = @"Z:\Rnd\ads\Software\Eli\Febris\Version1.1\Installer\GojiDropBoxService\\" + "Release_" + version;
                Directory.CreateDirectory(finalReleaseDirectory);

                for (int i = 0; i < installerName.Length; i++)
                {
                    Console.Write("Copying " + installerName[i] + "..");
                    File.Copy(installerName[i] + "_" + comVer + "_" + version + ".zip", finalReleaseDirectory + "\\" + installerName[i] + "_" + comVer + "_" + version + ".zip", overwrite);
                    Console.WriteLine("Done!");
                }

            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }

        }
        static void Main(string[] args)
        {
            string comver = "x64";
            try
            {

                Listener listen = new Listener();
                listen.Subscribe();
                FebrisInstaller.Zip();
                Console.WriteLine("compress done!");

                Console.WriteLine("Compressing the installers");
                Console.WriteLine("Please specify the version:");
                string ver = Console.ReadLine();
                while (ver == "")
                {
                    ver = Console.ReadLine();
                }
                if (ver != "exit")
                {
                    string finalReleaseDirectory = @"Z:\Rnd\ads\Software\Eli\Febris\Version1.1\Installer\GojiDropBoxService\" + "Release_" + ver;
                    bool overwrite = false;
                    while (Directory.Exists(finalReleaseDirectory) == true)
                    {
                        Console.WriteLine("Release " + ver + " Already exist!\nSelect different release");
                        string lver = ver;
                        ver = Console.ReadLine();
                        while (ver == "")
                        {
                            ver = Console.ReadLine();
                        }
                        if (ver == "sheep111")
                        {
                            overwrite = true;
                            ver = lver;
                            break;
                        }
                        finalReleaseDirectory = @"Z:\Rnd\ads\Software\Eli\Febris\Version1.1\Installer\GojiDropBoxService\\" + "Release_" + ver;
                    }
                    Release(ver, overwrite, comver);
                    Console.WriteLine("Done");
                }


                //FebrisInstaller.Unzip();
                //FebrisInstaller.CreateShortcuts(true);
            }
            catch (Exception err)
            {
                Console.WriteLine("Compress failed:\n" + err.Message);
                Console.ReadLine();
            }
        }
    }
}
