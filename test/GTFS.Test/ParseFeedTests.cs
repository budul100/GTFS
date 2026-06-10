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
using System.Reflection;
using GTFS.Exceptions;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;

namespace GTFS.Test
{
    /// <summary>
    /// Contains tests for ensuring required and optional files.
    /// </summary>
    [TestFixture]
    public class ParseFeedTests
    {
        #region Public Methods

        /// <summary>
        /// Tests parsing feed with all required files.
        /// </summary>
        [Test]
        public void ParseFeedWithAllRequiredFiles()
        {
            var reader = new GTFSReader<GTFSFeed>();
            var source = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.agency.txt"),"agency"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stops.txt"),"stops"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.routes.txt"),"routes"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.trips.txt"),"trips"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stop_times.txt"),"stop_times"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.calendar.txt"),"calendar"),
            };

            var feed = reader.Read(source);
            Assert.That(feed, Is.Not.Null);
        }

        /// <summary>
        /// Tests parsing feed with both calendar and calendar_dates file missing.
        /// </summary>
        [Test]
        public void ParseFeedWithBothCalendarFileMissing()
        {
            var reader = new GTFSReader<GTFSFeed>(true);
            var source = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.agency.txt"),"agency"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stops.txt"),"stops"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.routes.txt"),"routes"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.trips.txt"),"trips"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stop_times.txt"),"stop_times"),
            };

            void readAction() => reader.Read(source);
            string expectedMessage = string.Format(GTFSRequiredFileSetMissingException.MessageFormat, string.Join(",", "calendar", "calendar_dates"));

            Assert.That(
                (Action)readAction,
                Throws.TypeOf<GTFSRequiredFileSetMissingException>().With.Message.EqualTo(expectedMessage));
        }

        /// <summary>
        /// Tests parsing feed with all required files.
        /// </summary>
        [Test]
        public void ParseFeedWithCalendarReplacedByCalendarDatesFile()
        {
            var reader = new GTFSReader<GTFSFeed>();
            var source = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.agency.txt"),"agency"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stops.txt"),"stops"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.routes.txt"),"routes"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.trips.txt"),"trips"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stop_times.txt"),"stop_times"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.calendar_dates.txt"),"calendar_dates")
            };

            var feed = reader.Read(source);
            Assert.That(feed, Is.Not.Null);
        }

        /// <summary>
        /// Tests parsing feed with stop-times file missing.
        /// </summary>
        [Test]
        public void ParseFeedWithStopTimesFileMissing()
        {
            var reader = new GTFSReader<GTFSFeed>(true);
            var source = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.agency.txt"),"agency"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.stops.txt"),"stops"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.routes.txt"),"routes"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.trips.txt"),"trips"),
                new GTFSSourceFileStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.Samples.sample_feed.calendar.txt"),"calendar")
            };

            void readAction() => reader.Read(source);
            string expectedMessage = string.Format(GTFSRequiredFileMissingException.MessageFormat, "stop_times");

            Assert.That(
                (Action)readAction,
                Throws.TypeOf<GTFSRequiredFileMissingException>().With.Message.EqualTo(expectedMessage));
        }

        #endregion Public Methods
    }
}