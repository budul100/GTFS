using System;
using System.Collections.Generic;
using System.IO;
using GTFS.DB;
using GTFS.IO;
using GTFS.IO.Compression;

namespace GTFS
{
    /// <summary>
    /// Contains extension methods for the GTFS reader.
    /// </summary>
    public static class GTFSReaderExtensions
    {
        #region Public Methods

        /// <summary>
        /// Reads a GTFS feed from the given source.
        /// </summary>
        /// <typeparam name="T">The type of the GTFS feed to create and populate.</typeparam>
        /// <param name="reader">The <see cref="GTFSReader{T}"/> instance used to read the feed.</param>
        /// <param name="source">The collection of GTFS source files to read from.</param>
        /// <param name="progress">An optional progress reporter receiving values between 0.0 and 1.0.</param>
        /// <returns>A new instance of <typeparamref name="T"/> populated with the data from <paramref name="source"/>.</returns>
        public static T Read<T>(this GTFSReader<T> reader, IEnumerable<IGTFSSourceFile> source,
            IProgress<double> progress = null)
            where T : IGTFSFeed, new()
        {
            return reader.Read(new T(), source, progress);
        }

        /// <summary>
        /// Reads a GTFS feed from the given source, restricted to a single source file.
        /// </summary>
        /// <typeparam name="T">The type of the GTFS feed to create and populate.</typeparam>
        /// <param name="reader">The <see cref="GTFSReader{T}"/> instance used to read the feed.</param>
        /// <param name="source">The collection of GTFS source files available to the reader.</param>
        /// <param name="file">The specific GTFS source file to read.</param>
        /// <returns>A new instance of <typeparamref name="T"/> populated with the data from <paramref name="file"/>.</returns>
        public static T Read<T>(this GTFSReader<T> reader, IEnumerable<IGTFSSourceFile> source, IGTFSSourceFile file)
            where T : IGTFSFeed, new()
        {
            return reader.Read(new T(), source, file);
        }

        /// <summary>
        /// Reads a GTFS feed directly into a GTFS feed db.
        /// </summary>
        /// <param name="reader">The <see cref="GTFSReader{IGTFSFeed}"/> instance used to read the feed.</param>
        /// <param name="db">The <see cref="IGTFSFeedDB"/> database into which the feed is added.</param>
        /// <param name="source">The collection of GTFS source files to read from.</param>
        /// <param name="progress">An optional progress reporter receiving values between 0.0 and 1.0.</param>
        /// <returns>The identifier of the newly added feed within <paramref name="db"/>.</returns>
        public static int Read(this GTFSReader<IGTFSFeed> reader, IGTFSFeedDB db, IEnumerable<IGTFSSourceFile> source,
            IProgress<double> progress = null)
        {
            var newFeed = db.AddFeed();
            var feed = db.GetFeed(newFeed);

            reader.Read(feed, source, progress);

            return newFeed;
        }

        /// <summary>
        /// Reads a GTFS feed.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="path">The path, this has to be either a folder or a zip archive.</param>
        /// <param name="separator">A custom separator.</param>
        /// <param name="progress">An optional progress reporter receiving values between 0.0 and 1.0.</param>
        /// <typeparam name="T">The GTFS feed type.</typeparam>
        /// <returns>The GTFS feed.</returns>
        public static T Read<T>(this GTFSReader<T> reader, string path, char? separator = null, IProgress<double> progress = null)
            where T : IGTFSFeed, new()
        {
            ArgumentNullException.ThrowIfNull(path);

            if (Directory.Exists(path))
            {
                using var source = new GTFSDirectorySource(path, separator);

                return reader.Read(source, progress);
            }
            else if (File.Exists(path) && path.ToLower().EndsWith(".zip"))
            {
                using var source = new GTFSArchiveSource(File.OpenRead(path), separator);

                return reader.Read(source, progress);
            }

            throw new ArgumentException("Could not open GTFS feed, directory or archive not found.", nameof(path));
        }

        #endregion Public Methods
    }
}