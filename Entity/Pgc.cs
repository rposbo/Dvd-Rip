using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace Entity
{
    [XmlRootAttribute("Pgc")]
    public class Pgc : IComparable
    {
        public static Regex PgcAndLengthMatch = new Regex("PGC ([0-9]*). Length: ([0-9]:[0-9][0-9]:[0-9][0-9]):([0-9][0-9])");
        public static Regex AudioMatch = new Regex(@"Audio ([0-9]*). ([a-zA-Z]*) \([a-zA-Z0-9 ]*, 0xBD (0x[0-9]*)\)");
        public static Regex SubtitleMatch = new Regex(@"Subtitle ([0-9]*). ([a-zA-Z]*) [0-9] \(0xBD (0x[0-9]*)\)");
        public static Regex VtsMatch = new Regex("VTS_([0-9]*)");

        public List<AudioStream> Audio { get; set; }
        public List<SubtitleStream> Subtitle { get; set; }

        private TimeSpan _length;

        [XmlIgnore]
        public TimeSpan Length { 
            get {return _length;}
            set {_length = value;}
        }

        [XmlElement("Length", DataType = "duration")]
        public string XmlLength
        {
            get { return XmlConvert.ToString(_length); }
            set { _length = XmlConvert.ToTimeSpan(value); }
        }

        public int? Id { get; set; }
        public int Vts { get; set; }

        public Pgc()
        {
            Audio = new List<AudioStream>();
            Subtitle = new List<SubtitleStream>();
        }

        public string StreamDemuxArgs
        {
            get
            {
                var videoArgs = "0xE0";
                var audioArgs = string.Empty;
                var subtitleArgs = string.Empty;
                var outputArgs = string.Empty;

                var englishAudioStreams = from a in Audio
                                          where a.Name.Equals("english", StringComparison.InvariantCultureIgnoreCase)
                                          orderby a.Id descending
                                          select a;

                var foreignAudioStreams = from a in Audio
                                          where !a.Name.Equals("english", StringComparison.InvariantCultureIgnoreCase)
                                          orderby a.Id descending
                                          select a;

                var englishSubtitles = from s in Subtitle
                                       where s.Name.Equals("english", StringComparison.InvariantCultureIgnoreCase)
                                       orderby s.Id descending
                                       select s;

                var potentialAlternateAudios = from f in foreignAudioStreams
                                               where !f.Name.Equals("english", StringComparison.InvariantCultureIgnoreCase)
                                               select f;

                if (potentialAlternateAudios.Count() > 0)
                {
                    var alternateAudio = from p in potentialAlternateAudios
                                         where p.Code.Equals("0x80", StringComparison.InvariantCultureIgnoreCase)
                                                ||
                                                p.Name.Equals("japanese", StringComparison.InvariantCultureIgnoreCase)
                                         select p;

                    audioArgs = string.Format("{0} {1}", alternateAudio.First().Code, englishAudioStreams.First().Code);
                    subtitleArgs = englishSubtitles.First().Code;
                }

                if (audioArgs != string.Empty)
                {
                    outputArgs = string.Format(" /DEMUX {0} {1}", videoArgs, audioArgs);
                }

                return outputArgs;
            }
        }

        public int CompareTo(object obj)
        {
            var otherPgc = obj as Pgc;
            return (otherPgc.Length.Duration().CompareTo(this.Length.Duration()));
        }

        public override string ToString()
        {
            var audioString = string.Empty;
            foreach (var audio in Audio)
            {
                audioString += string.Format("\n\rAudio {0}-{1}-{2}-{3}"
                    ,audio.Id.ToString(), audio.Name, audio.Code, audio.Number);
            }
            
            var subtitleString = string.Empty;
            foreach (var subtitle in Subtitle)
            {
                subtitleString += string.Format("\n\rSubtitle {0}-{1}-{2}-{3}"
                    , subtitle.Id.ToString(), subtitle.Name, subtitle.Code, subtitle.Number);
            }

            var pgcString = string.Format("--PGC--\n\rVTS:{0}\n\rID:{1}\n\rLength:{2}{3}{4}\n\r", Vts, Id, Length, audioString, subtitleString);

            return pgcString;
        }
    }

    public class AudioStream
    {
        public int Id;
        public string Name;
        public string Code;
        public string Number;

    }

    public class SubtitleStream
    {
        public int Id;
        public string Name;
        public string Code;
        public string Number;
    }
}
