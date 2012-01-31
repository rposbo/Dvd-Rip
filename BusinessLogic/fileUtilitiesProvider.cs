using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Entity;

namespace BusinessLogic
{
    public delegate void PgcLoadedHandler (object sender, EventArgs e);

    public class fileUtilitiesProvider
    {
        private string _workingDirectory { get; set; }
        private DriveInfo _dvdDrive { get; set; }
        private string _dvdDecrypterPath { get; set; }
        private string _mencoderPath { get; set; }
        private string _vstripPath { get; set; }

        public fileUtilitiesProvider(string workingRootDirectory, DriveInfo dvdDrive, string dvdDecrypterPath, string mencoderPath, string vstripPath)
        {
            _workingDirectory = workingRootDirectory;
            _dvdDrive = dvdDrive;
            _dvdDecrypterPath = dvdDecrypterPath;
            _mencoderPath = mencoderPath;
            _vstripPath = vstripPath;
        }

        public void scanIfo()
        {
            foreach (var ifo in new DirectoryInfo(_dvdDrive.RootDirectory.FullName).GetFiles("*.ifo", SearchOption.AllDirectories))
            {
                try
                {
                    if (!File.Exists(_workingDirectory + @"\" + ifo.Name + ".scan"))
                    {
                        var process = new Process()
                        {
                            StartInfo = new ProcessStartInfo {
                                FileName = _vstripPath,
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                Arguments = string.Format("\"{0}\" \"{1}\"", ifo.FullName, _workingDirectory + @"\" + ifo.Name + ".scan")
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    }
                }
                catch (Exception e)
                {
                    File.WriteAllText(string.Format(@"{0}\{1}", _workingDirectory, ifo.Name + ".fail"), e.Message);
                }
            }
        }

        public List<Pgc> processIfo()
        {
            var pgcs = new List<Pgc>();
            var pgc = new Pgc();

            try
            {
                foreach (var scan in new DirectoryInfo(_workingDirectory).GetFiles("*.scan"))
                {   
                    var vts = Convert.ToInt32(Pgc.VtsMatch.Match(scan.Name).Groups[1].Value);
                    
                    foreach (var line in File.ReadAllLines(scan.FullName))
                    {
                        if (Pgc.PgcAndLengthMatch.Match(line).Success)
                        {
                            if (pgc.Id != null) pgcs.Add(pgc);

                            pgc = new Pgc()
                            {
                                Vts = vts,
                                Id = Convert.ToInt32(Pgc.PgcAndLengthMatch.Match(line).Groups[1].Value),
                                Length = TimeSpan.Parse(string.Format("{0}.{1}", Pgc.PgcAndLengthMatch.Match(line).Groups[2].Value, Pgc.PgcAndLengthMatch.Match(line).Groups[3].Value))
                            };
                        }

                        if (Pgc.AudioMatch.Match(line).Success)
                        {
                            var audioLine = Pgc.AudioMatch.Match(line);
                            pgc.Audio.Add(new AudioStream()
                            {
                                Id = Convert.ToInt32(audioLine.Groups[1].Value),
                                Name = audioLine.Groups[2].Value,
                                Code = audioLine.Groups[3].Value
                            });
                        }

                        if (Pgc.SubtitleMatch.Match(line).Success)
                        {
                            var subtitleLine = Pgc.SubtitleMatch.Match(line);
                            pgc.Subtitle.Add(new SubtitleStream()
                            {
                                Id = Convert.ToInt32(subtitleLine.Groups[1].Value),
                                Name = subtitleLine.Groups[2].Value,
                                Code = subtitleLine.Groups[3].Value
                            });
                        }
                    }
                }

                if (pgc.Id != null) pgcs.Add(pgc);
            }
            catch (Exception e)
            {
                File.WriteAllText(string.Format(@"{0}\{1}", _workingDirectory, "processIfo.fail"), e.Message);
            }

            return pgcs;
        }

        public void SavePgcs(List<Pgc> pgcs)
        {
            var xml = new System.Xml.Serialization.XmlSerializer(pgcs.GetType());
            TextWriter writer = new StreamWriter(string.Format(@"{0}\{1}", _workingDirectory, "Pgcs.xml"));
            xml.Serialize(writer, pgcs);
            writer.Dispose();
        }

        public List<Pgc> LoadPgcs()
        {
            var pgcXml = string.Format(@"{0}\{1}", _workingDirectory, "Pgcs.xml");

            if (File.Exists(pgcXml))
            {
                var xml = new System.Xml.Serialization.XmlSerializer(typeof(List<Pgc>));
                TextReader reader = new StreamReader(pgcXml);
                var pgcList = (List<Pgc>)xml.Deserialize(reader);
                reader.Dispose();
                return pgcList;
            }

            return null;
        }

        public void ripPgc(Pgc pgc)
        {
            try
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = _dvdDecrypterPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Arguments = string.Format(
                            "/MODE IFO /SRC {0} /DEST {1} /VTS {2} /PGC {3} /SPLIT NONE /CHAPTERS ALL{4} /START /CLOSE",
                            _dvdDrive.Name,
                            _workingDirectory,
                            pgc.Vts,
                            pgc.Id,
                            pgc.StreamDemuxArgs
                            )
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                File.WriteAllText(string.Format(@"{0}\{1}_{2}_ripPgc_{3}", _workingDirectory, pgc.Vts, pgc.Id, ".fail"), e.Message);
            }
        }

        public void convertPgc()
        {
            try
            {
                var outputFilename = "movie.avi";
                var encodingArgs = @"""{0}"" -ovc lavc -lavcopts vcodec=mpeg4:vbitrate=2000:v4mv:mbd=2:trell:cmp=3:subcmp=3:autoaspect=1:last_pred=2:vb_strategy=1:vpass={4}:turbo -oac mp3lame {1} -af volnorm -o ""{2}\{3}""";

                var vobs = Directory.GetFiles(_workingDirectory, "*.VOB", SearchOption.TopDirectoryOnly);
                var videoStreams = Directory.GetFiles(_workingDirectory, "*.M2V", SearchOption.TopDirectoryOnly);
                var audioStreams = Directory.GetFiles(_workingDirectory, "*.AC3", SearchOption.TopDirectoryOnly);

                var videoStreamPath = videoStreams.Count() > 0 ? videoStreams.First() : vobs.First();
                var audioStreamPath = audioStreams.Count() > 0 ? string.Format(@"-audiofile ""{0}""", audioStreams.First()) : string.Empty;

                for (var passNum = 1; passNum <= 2; passNum++)
                {
                    var processArguements = string.Format(encodingArgs, videoStreamPath, audioStreamPath, _workingDirectory, outputFilename, passNum);
                    
                    using (var encodeProcess = new Process())
                    {
                        encodeProcess.StartInfo = new ProcessStartInfo()
                        {
                            FileName = _mencoderPath,
                            UseShellExecute = false,
                            Arguments = processArguements
                        };
                        encodeProcess.Start();
                        encodeProcess.WaitForExit();
                    }
                }
            }
            catch (Exception e)
            {
                File.WriteAllText(string.Format(@"{0}\convertPgc.fail", _workingDirectory), e.Message);
            }
        }

        public void RemoveTemporaryFiles()
        {
            foreach (var file in Directory.GetFiles(_workingDirectory, "*.*").Where(f
                    => f.EndsWith(".VOB", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".M2V", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".AC3", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".XML", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".SCAN", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".IFO", StringComparison.OrdinalIgnoreCase)))
                File.Delete(file);
        }
    }
}
