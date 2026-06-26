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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;

namespace GTFS.Test
{
    /// <summary>
    /// Tests for the IProgress&lt;double&gt; support in GTFSReader.
    /// </summary>
    [TestFixture]
    public class ProgressTests
    {
        #region Public Methods

        /// <summary>
        /// Verifies that reads with and without progress produce identical feeds.
        /// </summary>
        [Test]
        public void Read_WithAndWithoutProgress_ProduceIdenticalFeeds()
        {
            var feedWithout = new GTFSReader<GTFSFeed>(strict: false)
                .Read(new GTFSFeed(), BuildMinimalSource(), progress: null);

            var feedWith = new GTFSReader<GTFSFeed>(strict: false)
                .Read(new GTFSFeed(), BuildMinimalSource(), new Progress<double>(_ => { }));

            Assert.That(feedWith.Agencies.Count, Is.EqualTo(feedWithout.Agencies.Count));
            Assert.That(feedWith.Stops.Count, Is.EqualTo(feedWithout.Stops.Count));
            Assert.That(feedWith.Routes.Count, Is.EqualTo(feedWithout.Routes.Count));
            Assert.That(feedWith.Trips.Count, Is.EqualTo(feedWithout.Trips.Count));
            Assert.That(feedWith.StopTimes.Count(), Is.EqualTo(feedWithout.StopTimes.Count()));
        }

        /// <summary>
        /// Verifies that no progress callbacks are fired when progress is null.
        /// </summary>
        [Test]
        public void Read_WithoutProgress_DoesNotThrow()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();

            reader.Read(new GTFSFeed(), source, progress: null);
        }

        /// <summary>
        /// Verifies that all reported values are within [0.0, 1.0].
        /// </summary>
        [Test]
        public void Read_WithProgress_AllValuesInRange()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();
            var reported = new List<double>();

            reader.Read(new GTFSFeed(), source, new SyncProgress(reported));

            Assert.That(reported, Is.All.InRange(0.0, 1.0));
        }

        /// <summary>
        /// Verifies that the feed is correctly populated even when progress is provided.
        /// Progress must not interfere with parse results.
        /// </summary>
        [Test]
        public void Read_WithProgress_FeedIsCorrectlyPopulated()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();

            var feed = reader.Read(new GTFSFeed(), source, new Progress<double>(_ => { }));

            Assert.That(feed.Agencies.Count, Is.EqualTo(1));
            Assert.That(feed.Stops.Count, Is.EqualTo(2));
            Assert.That(feed.Routes.Count, Is.EqualTo(1));
            Assert.That(feed.Trips.Count, Is.EqualTo(1));
            Assert.That(feed.StopTimes.Count(), Is.EqualTo(2));
        }

        /// <summary>
        /// Verifies that the final reported value is exactly 1.0.
        /// </summary>
        [Test]
        public void Read_WithProgress_LastValueIsOne()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();
            var reported = new List<double>();

            reader.Read(new GTFSFeed(), source, new SyncProgress(reported));

            Assert.That(reported.Last(), Is.EqualTo(1.0));
        }

        /// <summary>
        /// Verifies that at least one progress callback is fired during a read.
        /// </summary>
        [Test]
        public void Read_WithProgress_ReportsAtLeastOnce()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();
            var reported = new List<double>();

            reader.Read(new GTFSFeed(), source, new SyncProgress(reported));

            Assert.That(reported, Is.Not.Empty);
        }

        /// <summary>
        /// Verifies that reported values are non-decreasing.
        /// Progress must not go backwards.
        /// </summary>
        [Test]
        public void Read_WithProgress_ValuesAreNonDecreasing()
        {
            var reader = new GTFSReader<GTFSFeed>(strict: false);
            var source = BuildMinimalSource();
            var reported = new List<double>();

            reader.Read(new GTFSFeed(), source, new SyncProgress(reported));

            for (int i = 1; i < reported.Count; i++)
            {
                Assert.That(reported[i], Is.GreaterThanOrEqualTo(reported[i - 1]),
                    $"Progress went backwards at index {i}: {reported[i - 1]} -> {reported[i]}");
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Builds a minimal valid GTFS source from inline CSV strings.
        /// Covers all required files so strict mode does not throw.
        /// </summary>
        private static List<IGTFSSourceFile> BuildMinimalSource()
        {
            return
            [
                CsvFile("agency",
                    "agency_id,agency_name,agency_url,agency_timezone",
                    "DTA,Demo Transit Authority,http://example.com,America/Los_Angeles"),

                CsvFile("stops",
                    "stop_id,stop_name,stop_lat,stop_lon",
                    "S1,Stop One,47.0,8.0",
                    "S2,Stop Two,47.1,8.1"),

                CsvFile("routes",
                    "route_id,agency_id,route_short_name,route_long_name,route_type",
                    "R1,DTA,1,Route One,3"),

                CsvFile("calendar",
                    "service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date",
                    "WD,1,1,1,1,1,0,0,20240101,20241231"),

                CsvFile("trips",
                    "route_id,service_id,trip_id",
                    "R1,WD,T1"),

                CsvFile("stop_times",
                    "trip_id,arrival_time,departure_time,stop_id,stop_sequence",
                    "T1,08:00:00,08:00:00,S1,1",
                    "T1,08:10:00,08:10:00,S2,2"),
            ];
        }

        /// <summary>
        /// Creates an IGTFSSourceFile from a file name and CSV lines.
        /// </summary>
        private static IGTFSSourceFile CsvFile(string name, params string[] lines)
        {
            var csv = string.Join("\n", lines) + "\n";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            return new GTFSSourceFileStream(stream, name);
        }

        #endregion Private Methods

        #region Private Classes

        private sealed class SyncProgress(List<double> target) : IProgress<double>
        {
            #region Public Methods

            public void Report(double value) => target.Add(value);

            #endregion Public Methods
        }

        #endregion Private Classes
    }
}