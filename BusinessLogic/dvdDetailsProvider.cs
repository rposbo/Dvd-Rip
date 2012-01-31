using System;
using System.Linq;
using TvdbLib;
using TvdbLib.Data;
using Entity;

namespace BusinessLogic
{
    public class dvdDetailsProvider
    {
        private string _movieApiKey { get; set; }
        private string _tvApiKey { get; set; }

        public dvdDetailsProvider(string movieApiKey, string tvApiKey) 
        {
            _movieApiKey = movieApiKey;
            _tvApiKey = tvApiKey;
        }
        
        public Dvd getDvdDetails(string volumeTitle)
        {
            var api = new TheMovieDB.TmdbAPI(_movieApiKey);
            var movies = api.MovieSearch(volumeTitle);
            if (movies.Length > 0)
            {
                var movie = movies[0];
                var info = api.GetMovieInfo(movie.Id);
                var tags = from c in info.Categories
                           where c.Type == "genre"
                           select c.Name;

                var returnDvd = new Dvd { Id = movie.Id, Title = movie.Name, Year = string.Format("{0:yyyy}", info.Released), Tags = tags.ToArray<string>(), Type = DvdType.Movie };
                return returnDvd;
            }
            else
            {
                var tvdb = new TvdbHandler(null, _tvApiKey);
                var tvdbSearchResult = tvdb.SearchSeries(volumeTitle);
                var seriesIds = tvdbSearchResult.ToDictionary(result => result.Id, result => result.SeriesName);

                foreach (var seriesId in seriesIds)
                {
                    var series = tvdb.GetSeries(seriesId.Key, TvdbLanguage.DefaultLanguage, true, false, false);
                    foreach (var episode in series.GetEpisodesAbsoluteOrder())
                    {
                        //Console.WriteLine("Series: {0}\n\rName: {1}\n\rId: {2}\n\rNumber: {3}\n\rGenre: {4}\n\r\n\r",
                        //                  seriesId.Value,
                        //                  episode.EpisodeName,
                        //                  episode.Id,
                        //                  episode.EpisodeNumber,
                        //                  series.GenreString);
                        //Console.WriteLine(episode.ToString());
                    }
                }

                return new Dvd() { Type = DvdType.Tv, Year = tvdbSearchResult[0].FirstAired.ToString() };
            }
        }
    }
}
