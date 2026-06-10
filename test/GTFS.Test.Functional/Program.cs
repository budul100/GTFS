using System.IO;
using Microsoft.Extensions.Logging;

namespace GTFS.Test.Functional
{
    public class Program
    {
        #region Public Methods

        public static void Main(string[] args)
        {
            // enable logging.
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            GTFS.Logging.Logger.UseLoggerFactory(loggerFactory);

            // read from archive.
            var reader = new GTFSReader<GTFSFeed>();
            var feed = reader.Read("gtfs1.zip");

            // write to folder.
            var path = "output";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var writer = new GTFSWriter<GTFSFeed>();
            writer.Write(feed, path);

            // read from folder.
            feed = reader.Read(path);
        }

        #endregion Public Methods
    }
}