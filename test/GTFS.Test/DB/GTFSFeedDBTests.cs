// The MIT License (MIT)

// Copyright (c) 2014 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Reflection;
using GTFS.DB;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;

namespace GTFS.Test.DB
{
    /// <summary>
    /// Contains test methods that can be applied to any GTFS feed db.
    /// </summary>
    [TestFixture]
    public abstract class GTFSFeedDBTests
    {
        #region Public Methods

        /// <summary>
        /// Tests adding a feed.
        /// </summary>
        [Test]
        public void TestAddFeed()
        {
            // get test db.
            var db = this.CreateDB();

            // build test feed.
            var feed = this.BuildTestFeed();

            // add/get to/from db and compare all.
            var feedId = db.AddFeed(feed);
            GTFSAssert.AreEqual(feed, db.GetFeed(feedId));
        }

        /// <summary>
        /// Test get feeds.
        /// </summary>
        [Test]
        public void TestGetFeeds()
        {
            // get test db.
            var db = this.CreateDB();

            // build test feed.
            var feed = this.BuildTestFeed();

            // add feed.
            var feedId = db.AddFeed(feed);

            db.RemoveFeed(feedId);

            // get feed.
            feed = db.GetFeed(feedId);
            Assert.That(feed, Is.Null);
        }

        /// <summary>
        /// Test removing a feed.
        /// </summary>
        [Test]
        public void TestRemoveFeed()
        {
            // get test db.
            var db = this.CreateDB();

            // build test feed.
            var feed = this.BuildTestFeed();

            // add feed.
            var feedId = db.AddFeed(feed);

            db.RemoveFeed(feedId);

            // get feed.
            feed = db.GetFeed(feedId);
            Assert.That(feed, Is.Null);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Builds a test feed.
        /// </summary>
        /// <returns></returns>
        protected virtual IGTFSFeed BuildTestFeed()
        {
            // create the reader.
            var reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            return reader.Read(source);
        }

        /// <summary>
        /// Creates a new test db.
        /// </summary>
        /// <returns></returns>
        protected abstract IGTFSFeedDB CreateDB();

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Builds the source from embedded streams.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IGTFSSourceFile> BuildSource()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var result = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.agency.txt"), "agency"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.calendar.txt"), "calendar"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.calendar_dates.txt"), "calendar_dates"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.fare_attributes.txt"), "fare_attributes"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.fare_rules.txt"), "fare_rules"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.frequencies.txt"), "frequencies"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.routes.txt"), "routes"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.shapes.txt"), "shapes"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stop_times.txt"), "stop_times"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stops.txt"), "stops"),
                new GTFSSourceFileStream(assembly.GetManifestResourceStream("GTFS.Test.Samples.sample_feed.trips.txt"), "trips")
            };
            return result;
        }

        #endregion Private Methods
    }
}