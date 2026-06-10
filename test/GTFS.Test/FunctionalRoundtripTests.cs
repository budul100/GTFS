using NUnit.Framework;
using System.IO;

namespace GTFS.Test
{
    [TestFixture]
    public class FunctionalRoundtripTests
    {
        #region Public Methods

        [TestCase("gtfs1.zip")]
        [TestCase("gtfs2.zip")]
        public void ReadWriteReadRoundtrip(string file)
        {
            var reader = new GTFSReader<GTFSFeed>();
            var feed = reader.Read(file);

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            try
            {
                var writer = new GTFSWriter<GTFSFeed>();
                writer.Write(feed, path);

                var feedReloaded = reader.Read(path);
                Assert.That(feedReloaded, Is.Not.Null);
                //GTFSAssert.AreEqual(feed, feedReloaded);
            }
            finally
            {
                Directory.Delete(path, recursive: true);
            }
        }

        #endregion Public Methods
    }
}