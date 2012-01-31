using System;
using System.Linq;
using System.Collections.Generic;

namespace Entity
{
    public class Dvd
    {
        public string Title { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string TagString
        {
            get
            {
                return string.Join(",", Tags.ToArray<string>());
            }
        }
        public int Id { get; set; }
        public string Year { get; set; }
        public DvdType Type { get; set; }

        public override string ToString()
        {
            return string.Format("--DVD--\n\rTitle:{0}\n\rId:{1}\n\rType:{2}\n\rTags:{3}\n\r",
                Title, Id, Type.ToString(), TagString);
        }
    }

    public enum DvdType
    {
        Movie,
        Tv
    }
}
