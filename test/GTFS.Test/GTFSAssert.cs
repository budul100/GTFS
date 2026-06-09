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

using GTFS.Entities;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GTFS.Test
{
    /// <summary>
    /// Contains test extensions/helper methods.
    /// </summary>
    public static class GTFSAssert
    {
        /// <summary>
        /// Builds the source from embedded streams.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IGTFSSourceFile> BuildSource()
        {
            var source = new List<IGTFSSourceFile>();
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.agency.txt"), "agency"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.calendar.txt"), "calendar"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.calendar_dates.txt"), "calendar_dates"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.fare_attributes.txt"), "fare_attributes"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.fare_rules.txt"), "fare_rules"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.frequencies.txt"), "frequencies"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.routes.txt"), "routes"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.shapes.txt"), "shapes"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.stop_times.txt"), "stop_times"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.stops.txt"), "stops"));
            source.Add(new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.trips.txt"), "trips"));
            return source;
        }

        /// <summary>
        /// Compares two feeds.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(IGTFSFeed actual, IGTFSFeed expected)
        {
            // first compare feed info.
            GTFSAssert.AreEqual(actual.GetFeedInfo(), expected.GetFeedInfo());

            // compare agencies.
            GTFSAssert.AreEqual<Agency>(actual.Agencies, expected.Agencies,
                (x, y) => x.Id == y.Id, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<CalendarDate>(actual.CalendarDates, expected.CalendarDates,
                (x, y) => x.ServiceId == y.ServiceId && x.Date == y.Date && x.ExceptionType == y.ExceptionType, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Calendar>(actual.Calendars, expected.Calendars,
                (x, y) => x.ToString() == y.ToString(), (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<FareAttribute>(actual.FareAttributes, expected.FareAttributes,
                (x, y) => x.FareId == y.FareId, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<FareRule>(actual.FareRules, expected.FareRules,
                (x, y) => x.ContainsId == y.ContainsId && x.DestinationId == y.DestinationId &&
                    x.FareId == y.FareId && x.OriginId == y.OriginId && x.RouteId == y.RouteId, 
                    (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Frequency>(actual.Frequencies, expected.Frequencies,
                (x, y) => x.TripId == y.TripId && x.StartTime == y.StartTime, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Route>(actual.Routes, expected.Routes,
                (x, y) => x.Id == y.Id, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Shape>(actual.Shapes, expected.Shapes,
                (x, y) => x.Id == y.Id && x.Sequence == y.Sequence, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Stop>(actual.Stops, expected.Stops,
                (x, y) => x.Id == y.Id, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<StopTime>(actual.StopTimes, expected.StopTimes,
                (x, y) => x.TripId == y.TripId && x.StopId == y.StopId && x.StopSequence == y.StopSequence, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Transfer>(actual.Transfers, expected.Transfers,
                (x, y) => x.FromStopId == y.FromStopId && x.ToStopId == y.ToStopId && x.TransferType == y.TransferType, (x, y) => GTFSAssert.AreEqual(x, y));
            GTFSAssert.AreEqual<Trip>(actual.Trips, expected.Trips,
                (x, y) => x.Id == y.Id, (x, y) => GTFSAssert.AreEqual(x, y));
        }

        /// <summary>
        /// Compares the two enumerables.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actuals"></param>
        /// <param name="expectees"></param>
        /// <param name="idEqual"></param>
        /// <param name="areEqualAction"></param>
        public static void AreEqual<T>(IEnumerable<T> actuals, IEnumerable<T> expectees, 
            Func<T, T, bool> idEqual, Action<T, T> areEqualAction)
        {
            Assert.That(actuals.Count(), Is.EqualTo(expectees.Count()));
            foreach(var actual in actuals)
            {
                var expected = expectees.First(x => idEqual(x, actual));
                Assert.That(expected, Is.Not.Null);
                areEqualAction(actual, expected);
            }
        }

        /// <summary>
        /// Compares two feed-infos.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(FeedInfo actual, FeedInfo expected)
        {
            if(actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.EndDate, Is.EqualTo(expected.EndDate));
            Assert.That(actual.Lang, Is.EqualTo(expected.Lang));
            Assert.That(actual.PublisherName, Is.EqualTo(expected.PublisherName));
            Assert.That(actual.PublisherUrl, Is.EqualTo(expected.PublisherUrl));
            Assert.That(actual.StartDate, Is.EqualTo(expected.StartDate));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
        }

        /// <summary>
        /// Compares two agencies.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Agency actual, Agency expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.Email, Is.EqualTo(expected.Email));
            Assert.That(actual.FareURL, Is.EqualTo(expected.FareURL));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.LanguageCode, Is.EqualTo(expected.LanguageCode));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Phone, Is.EqualTo(expected.Phone));
            Assert.That(actual.Timezone, Is.EqualTo(expected.Timezone));
            Assert.That(actual.URL, Is.EqualTo(expected.URL));
        }

        /// <summary>
        /// Compares two calendars.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Calendar actual, Calendar expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.EndDate, Is.EqualTo(expected.EndDate));
            Assert.That(actual.Friday, Is.EqualTo(expected.Friday));
            Assert.That(actual.Monday, Is.EqualTo(expected.Monday));
            Assert.That(actual.Saturday, Is.EqualTo(expected.Saturday));
            Assert.That(actual.ServiceId, Is.EqualTo(expected.ServiceId));
            Assert.That(actual.StartDate, Is.EqualTo(expected.StartDate));
            Assert.That(actual.Sunday, Is.EqualTo(expected.Sunday));
            Assert.That(actual.Thursday, Is.EqualTo(expected.Thursday));
            Assert.That(actual.Tuesday, Is.EqualTo(expected.Tuesday));
            Assert.That(actual.Wednesday, Is.EqualTo(expected.Wednesday));
        }

        /// <summary>
        /// Compares two calendar dates.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(CalendarDate actual, CalendarDate expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.Date, Is.EqualTo(expected.Date));
            Assert.That(actual.ExceptionType, Is.EqualTo(expected.ExceptionType));
            Assert.That(actual.ServiceId, Is.EqualTo(expected.ServiceId));
        }

        /// <summary>
        /// Compares two fare attributes.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>

        public static void AreEqual(FareAttribute actual, FareAttribute expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.CurrencyType, Is.EqualTo(expected.CurrencyType));
            Assert.That(actual.FareId, Is.EqualTo(expected.FareId));
            Assert.That(actual.PaymentMethod, Is.EqualTo(expected.PaymentMethod));
            Assert.That(actual.Price, Is.EqualTo(expected.Price));
            Assert.That(actual.TransferDuration, Is.EqualTo(expected.TransferDuration));
            Assert.That(actual.Transfers, Is.EqualTo(expected.Transfers));
        }

        /// <summary>
        /// Compares two fare rules.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(FareRule actual, FareRule expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.ContainsId, Is.EqualTo(expected.ContainsId));
            Assert.That(actual.DestinationId, Is.EqualTo(expected.DestinationId));
            Assert.That(actual.FareId, Is.EqualTo(expected.FareId));
            Assert.That(actual.OriginId, Is.EqualTo(expected.OriginId));
            Assert.That(actual.RouteId, Is.EqualTo(expected.RouteId));
        }

        /// <summary>
        /// Compares two frequencies
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Frequency actual, Frequency expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.EndTime, Is.EqualTo(expected.EndTime));
            Assert.That(actual.ExactTimes, Is.EqualTo(expected.ExactTimes));
            Assert.That(actual.HeadwaySecs, Is.EqualTo(expected.HeadwaySecs));
            Assert.That(actual.StartTime, Is.EqualTo(expected.StartTime));
            Assert.That(actual.TripId, Is.EqualTo(expected.TripId));
        }

        /// <summary>
        /// Compares two routes.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Route actual, Route expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.AgencyId, Is.EqualTo(expected.AgencyId));
            Assert.That(actual.Color, Is.EqualTo(expected.Color));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.LongName, Is.EqualTo(expected.LongName));
            Assert.That(actual.ShortName, Is.EqualTo(expected.ShortName));
            Assert.That(actual.TextColor, Is.EqualTo(expected.TextColor));
            Assert.That(actual.Type, Is.EqualTo(expected.Type));
            Assert.That(actual.Url, Is.EqualTo(expected.Url));
        }

        /// <summary>
        /// Compares two shapes.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Shape actual, Shape expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.DistanceTravelled, Is.EqualTo(expected.DistanceTravelled));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Latitude, Is.EqualTo(expected.Latitude));
            Assert.That(actual.Longitude, Is.EqualTo(expected.Longitude));
            Assert.That(actual.Sequence, Is.EqualTo(expected.Sequence));
        }

        /// <summary>
        /// Compares two stops.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Stop actual, Stop expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.Code, Is.EqualTo(expected.Code));
            Assert.That(actual.Description, Is.EqualTo(expected.Description));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Latitude, Is.EqualTo(expected.Latitude));
            Assert.That(actual.LocationType, Is.EqualTo(expected.LocationType));
            Assert.That(actual.Longitude, Is.EqualTo(expected.Longitude));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.ParentStation, Is.EqualTo(expected.ParentStation));
            Assert.That(actual.Timezone, Is.EqualTo(expected.Timezone));
            Assert.That(actual.Url, Is.EqualTo(expected.Url));
            Assert.That(actual.WheelchairBoarding, Is.EqualTo(expected.WheelchairBoarding));
            Assert.That(actual.Zone, Is.EqualTo(expected.Zone));
        }

        /// <summary>
        /// Compares two stoptimes.
        /// </summary>
        public static void AreEqual(StopTime actual, StopTime expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.ArrivalTime, Is.EqualTo(expected.ArrivalTime));
            Assert.That(actual.DepartureTime, Is.EqualTo(expected.DepartureTime));
            Assert.That(actual.DropOffType, Is.EqualTo(expected.DropOffType));
            Assert.That(actual.PickupType, Is.EqualTo(expected.PickupType));
            Assert.That(actual.ShapeDistTravelled, Is.EqualTo(expected.ShapeDistTravelled));
            Assert.That(actual.StopHeadsign, Is.EqualTo(expected.StopHeadsign));
            Assert.That(actual.StopId, Is.EqualTo(expected.StopId));
            Assert.That(actual.StopSequence, Is.EqualTo(expected.StopSequence));
            Assert.That(actual.TripId, Is.EqualTo(expected.TripId));
            Assert.That(actual.TimepointType, Is.EqualTo(expected.TimepointType));
        }

        /// <summary>
        /// Compares two transfers.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Transfer actual, Transfer expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.FromStopId, Is.EqualTo(expected.FromStopId));
            Assert.That(actual.MinimumTransferTime, Is.EqualTo(expected.MinimumTransferTime));
            Assert.That(actual.ToStopId, Is.EqualTo(expected.ToStopId));
            Assert.That(actual.TransferType, Is.EqualTo(expected.TransferType));
        }

        /// <summary>
        /// Compares two trips.
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        public static void AreEqual(Trip actual, Trip expected)
        {
            if (actual == null)
            {
                Assert.That(expected, Is.Null);
                return;
            }
            Assert.That(actual.AccessibilityType, Is.EqualTo(expected.AccessibilityType));
            Assert.That(actual.BlockId, Is.EqualTo(expected.BlockId));
            Assert.That(actual.Direction, Is.EqualTo(expected.Direction));
            Assert.That(actual.Headsign, Is.EqualTo(expected.Headsign));
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.RouteId, Is.EqualTo(expected.RouteId));
            Assert.That(actual.ServiceId, Is.EqualTo(expected.ServiceId));
            Assert.That(actual.ShapeId, Is.EqualTo(expected.ShapeId));
            Assert.That(actual.ShortName, Is.EqualTo(expected.ShortName));
        }
    }
}
