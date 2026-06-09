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

using GTFS.StopsToShape;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFS.Test.StopsToShape
{
    /// <summary>
    /// Holds tests for the stops at shape finder.
    /// </summary>
    [TestFixture]
    public class StopAtShapesFinderTests
    {
        /// <summary>
        /// Tests matching stops to shapes in the sample-feed for trip AB1.
        /// </summary>
        [Test]
        public void TestTripAB1()
        {
            // create the reader.
            var reader = new GTFSReader<GTFSFeed>();
            var source = GTFSAssert.BuildSource();

            // execute the reader.
            var feed = reader.Read(source);

            // find stops.
            var finder = new StopAtShapesFinder();
            var stopAtShapes = finder.Find(feed, "AB1");

            // validate/test.
            Assert.That(stopAtShapes, Is.Not.Null);
            Assert.That(stopAtShapes.Count, Is.EqualTo(2));
            Assert.That(stopAtShapes[0].StopId, Is.EqualTo("BEATTY_AIRPORT"));
            Assert.That(stopAtShapes[0].ShapePointSequence, Is.EqualTo(1));
            Assert.That(stopAtShapes[0].StopOffset, Is.EqualTo(0));
            Assert.That(stopAtShapes[0].TripId, Is.EqualTo("AB1"));
            Assert.That(stopAtShapes[1].StopId, Is.EqualTo("BULLFROG"));
            Assert.That(stopAtShapes[1].ShapePointSequence, Is.EqualTo(4));
            Assert.That(stopAtShapes[1].StopOffset, Is.EqualTo(0));
            Assert.That(stopAtShapes[1].TripId, Is.EqualTo("AB1"));
        }
    }
}