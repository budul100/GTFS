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
using System.Linq;
using System.Reflection;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using GTFS.IO;
using GTFS.IO.CSV;
using NUnit.Framework;

namespace GTFS.Test
{
    /// <summary>
    /// Contains tests for the directory source.
    /// </summary>
    [TestFixture]
    public class DirectorySourceTest
    {
        #region Public Methods

        /// <summary>
        /// Tests parsing agencies.
        /// </summary>
        [Test]
        public void ParseAgencies()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("agency")));

            // test result.
            Assert.That(feed.Agencies, Is.Not.Null);
            var agencies = new List<Agency>(feed.Agencies);
            Assert.That(agencies.Count, Is.EqualTo(1));
            Assert.That(agencies[0].FareURL, Is.Null);
            Assert.That(agencies[0].Id, Is.EqualTo("DTA"));
            Assert.That(agencies[0].LanguageCode, Is.Null);
            Assert.That(agencies[0].Name, Is.EqualTo("Demo Transit Authority"));
            Assert.That(agencies[0].Phone, Is.Null);
            Assert.That(agencies[0].Timezone, Is.EqualTo("America/Los_Angeles"));
            Assert.That(agencies[0].URL, Is.EqualTo("http://google.com"));
        }

        /// <summary>
        /// Tests parsing calendar dates.
        /// </summary>
        [Test]
        public void ParseCalendarDates()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("calendar_dates")));

            // test result.
            Assert.That(feed.CalendarDates, Is.Not.Null);
            var calendarDates = new List<CalendarDate>(feed.CalendarDates);
            Assert.That(calendarDates.Count, Is.EqualTo(1));

            // @ 1: service_id,date,exception_type
            // @ 2: FULLW,20070604,2
            int idx = 0;
            Assert.That(calendarDates[idx].ServiceId, Is.EqualTo("FULLW"));
            Assert.That(calendarDates[idx].Date, Is.EqualTo(new System.DateTime(2007, 06, 04)));
            Assert.That(calendarDates[idx].ExceptionType, Is.EqualTo(ExceptionType.Removed));
        }

        /// <summary>
        /// Tests parsing calendars.
        /// </summary>
        [Test]
        public void ParseCalendars()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("calendar")));

            // test result.
            Assert.That(feed.Calendars, Is.Not.Null);
            var calendars = feed.Calendars.ToList();
            Assert.That(calendars.Count, Is.EqualTo(2));

            // @ 1: service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date
            // @ 2: FULLW,1,1,1,1,1,1,1,20070101,20101231
            int idx = 0;
            Assert.That(calendars[idx].ServiceId, Is.EqualTo("FULLW"));
            Assert.That(calendars[idx].Monday, Is.True);
            Assert.That(calendars[idx].Tuesday, Is.True);
            Assert.That(calendars[idx].Wednesday, Is.True);
            Assert.That(calendars[idx].Thursday, Is.True);
            Assert.That(calendars[idx].Friday, Is.True);
            Assert.That(calendars[idx].Saturday, Is.True);
            Assert.That(calendars[idx].Sunday, Is.True);
            Assert.That(calendars[idx].StartDate, Is.EqualTo(new DateTime(2007, 01, 01)));
            Assert.That(calendars[idx].EndDate, Is.EqualTo(new DateTime(2010, 12, 31)));

            // @3: WE,0,0,0,0,0,1,1,20070101,20101231
            idx = 1;
            Assert.That(calendars[idx].ServiceId, Is.EqualTo("WE"));
            Assert.That(calendars[idx].Monday, Is.False);
            Assert.That(calendars[idx].Tuesday, Is.False);
            Assert.That(calendars[idx].Wednesday, Is.False);
            Assert.That(calendars[idx].Thursday, Is.False);
            Assert.That(calendars[idx].Friday, Is.False);
            Assert.That(calendars[idx].Saturday, Is.True);
            Assert.That(calendars[idx].Sunday, Is.True);
            Assert.That(calendars[idx].StartDate, Is.EqualTo(new DateTime(2007, 01, 01)));
            Assert.That(calendars[idx].EndDate, Is.EqualTo(new DateTime(2010, 12, 31)));
        }

        /// <summary>
        /// Tests parsing routes.
        /// </summary>
        [Test]
        public void ParseFareAttributes()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("fare_attributes")));

            // test result.
            Assert.That(feed.FareAttributes, Is.Not.Null);

            var fareAttributes = feed.FareAttributes.ToList();
            Assert.That(fareAttributes.Count, Is.EqualTo(2));

            //fare_id,price,currency_type,payment_method,transfers,transfer_duration

            //p,1.25,USD,0,0,
            int idx = 0;
            Assert.That(fareAttributes[idx].FareId, Is.EqualTo("p"));
            Assert.That(fareAttributes[idx].Price, Is.EqualTo("1.25"));
            Assert.That(fareAttributes[idx].CurrencyType, Is.EqualTo("USD"));
            Assert.That(fareAttributes[idx].PaymentMethod, Is.EqualTo(PaymentMethodType.OnBoard));
            Assert.That(fareAttributes[idx].Transfers, Is.EqualTo(0));
            Assert.That(fareAttributes[idx].TransferDuration, Is.EqualTo(string.Empty));

            //a,5.25,USD,0,0,
            idx = 1;
            Assert.That(fareAttributes[idx].FareId, Is.EqualTo("a"));
            Assert.That(fareAttributes[idx].Price, Is.EqualTo("5.25"));
            Assert.That(fareAttributes[idx].CurrencyType, Is.EqualTo("USD"));
            Assert.That(fareAttributes[idx].PaymentMethod, Is.EqualTo(PaymentMethodType.OnBoard));
            Assert.That(fareAttributes[idx].Transfers, Is.EqualTo(0));
            Assert.That(fareAttributes[idx].TransferDuration, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Tests parsing routes.
        /// </summary>
        [Test]
        public void ParseFareRules()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("fare_rules")));

            // test result.
            Assert.That(feed.FareRules, Is.Not.Null);

            var fareRules = feed.FareRules.ToList();
            Assert.That(fareRules.Count, Is.EqualTo(4));

            // fare_id,route_id,origin_id,destination_id,contains_id

            //p,AB,,,
            int idx = 0;
            Assert.That(fareRules[idx].RouteId, Is.EqualTo("AB"));
            Assert.That(fareRules[idx].FareId, Is.EqualTo("p"));
            Assert.That(fareRules[idx].OriginId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].DestinationId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].ContainsId, Is.EqualTo(string.Empty));

            //p,STBA,,,
            idx = 1;
            Assert.That(fareRules[idx].RouteId, Is.EqualTo("STBA"));
            Assert.That(fareRules[idx].FareId, Is.EqualTo("p"));
            Assert.That(fareRules[idx].OriginId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].DestinationId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].ContainsId, Is.EqualTo(string.Empty));

            //p,BFC,,,
            idx = 2;
            Assert.That(fareRules[idx].RouteId, Is.EqualTo("BFC"));
            Assert.That(fareRules[idx].FareId, Is.EqualTo("p"));
            Assert.That(fareRules[idx].OriginId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].DestinationId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].ContainsId, Is.EqualTo(string.Empty));

            //a,AAMV,,,
            idx = 3;
            Assert.That(fareRules[idx].RouteId, Is.EqualTo("AAMV"));
            Assert.That(fareRules[idx].FareId, Is.EqualTo("a"));
            Assert.That(fareRules[idx].OriginId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].DestinationId, Is.EqualTo(string.Empty));
            Assert.That(fareRules[idx].ContainsId, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Tests parsing frequencies.
        /// </summary>
        [Test]
        public void ParseFrequencies()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>(false);

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("frequencies")));

            // test result.
            Assert.That(feed.Frequencies, Is.Not.Null);
            var frequencies = feed.Frequencies.ToList();
            Assert.That(frequencies.Count, Is.EqualTo(11));

            // @ 1: trip_id,start_time,end_time,headway_secs
            // @ 2: STBA,6:00:00,22:00:00,1800

            // @ 1: route_id,service_id,trip_id,trip_headsign,direction_id,block_id,shape_id
            // @ 2: AB,FULLW,AB1,to Bullfrog,0,1,shape_1
            int idx = 0;
            Assert.That(frequencies[idx].TripId, Is.EqualTo("STBA"));
            Assert.That(frequencies[idx].StartTime, Is.EqualTo("6:00:00"));
            Assert.That(frequencies[idx].EndTime, Is.EqualTo("22:00:00"));
            Assert.That(frequencies[idx].HeadwaySecs, Is.EqualTo("1800"));
            Assert.That(frequencies[idx].ExactTimes, Is.Null);

            // @ 10: CITY2,16:00:00,18:59:59,600
            idx = 8;
            Assert.That(frequencies[idx].TripId, Is.EqualTo("CITY2"));
            Assert.That(frequencies[idx].StartTime, Is.EqualTo("16:00:00"));
            Assert.That(frequencies[idx].EndTime, Is.EqualTo("18:59:59"));
            Assert.That(frequencies[idx].HeadwaySecs, Is.EqualTo("600"));
            Assert.That(frequencies[idx].ExactTimes, Is.Null);
        }

        /// <summary>
        /// Tests parsing routes.
        /// </summary>
        [Test]
        public void ParseRoutes()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("routes")));

            // test result.
            Assert.That(feed.Routes, Is.Not.Null);
            var routes = feed.Routes.ToList();
            Assert.That(routes.Count, Is.EqualTo(5));

            //route_id,agency_id,route_short_name,route_long_name,route_desc,route_type,route_url,route_color,route_text_color

            //AB,DTA,10,Airport - Bullfrog,,3,,,
            int idx = 0;
            Assert.That(routes[idx].Id, Is.EqualTo("AB"));
            Assert.That(routes[idx].AgencyId, Is.EqualTo("DTA"));
            Assert.That(routes[idx].ShortName, Is.EqualTo("10"));
            Assert.That(routes[idx].LongName, Is.EqualTo("Airport - Bullfrog"));
            Assert.That(routes[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(routes[idx].Type, Is.EqualTo(RouteTypeExtended.BusService));
            Assert.That(routes[idx].Color, Is.EqualTo(-3932017));
            Assert.That(routes[idx].TextColor, Is.Null);

            //BFC,DTA,20,Bullfrog - Furnace Creek Resort,,3,,,
            idx = 1;
            Assert.That(routes[idx].Id, Is.EqualTo("BFC"));
            Assert.That(routes[idx].AgencyId, Is.EqualTo("DTA"));
            Assert.That(routes[idx].ShortName, Is.EqualTo("20"));
            Assert.That(routes[idx].LongName, Is.EqualTo("Bullfrog - Furnace Creek Resort"));
            Assert.That(routes[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(routes[idx].Type, Is.EqualTo(RouteTypeExtended.BusService));
            Assert.That(routes[idx].Color, Is.EqualTo(-1));
            Assert.That(routes[idx].TextColor, Is.Null);

            //STBA,DTA,30,Stagecoach - Airport Shuttle,,3,,,
            idx = 2;
            Assert.That(routes[idx].Id, Is.EqualTo("STBA"));
            Assert.That(routes[idx].AgencyId, Is.EqualTo("DTA"));
            Assert.That(routes[idx].ShortName, Is.EqualTo("30"));
            Assert.That(routes[idx].LongName, Is.EqualTo("Stagecoach - Airport Shuttle"));
            Assert.That(routes[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(routes[idx].Type, Is.EqualTo(RouteTypeExtended.BusService));
            Assert.That(routes[idx].Color, Is.EqualTo(null));
            Assert.That(routes[idx].TextColor, Is.Null);

            //CITY,DTA,40,City,,3,,,
            idx = 3;
            Assert.That(routes[idx].Id, Is.EqualTo("CITY"));
            Assert.That(routes[idx].AgencyId, Is.EqualTo("DTA"));
            Assert.That(routes[idx].ShortName, Is.EqualTo("40"));
            Assert.That(routes[idx].LongName, Is.EqualTo("City"));
            Assert.That(routes[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(routes[idx].Type, Is.EqualTo(RouteTypeExtended.BusService));
            Assert.That(routes[idx].Color, Is.EqualTo(null));
            Assert.That(routes[idx].TextColor, Is.Null);

            //AAMV,DTA,50,Airport - Amargosa Valley,,3,,,
            idx = 4;
            Assert.That(routes[idx].Id, Is.EqualTo("AAMV"));
            Assert.That(routes[idx].AgencyId, Is.EqualTo("DTA"));
            Assert.That(routes[idx].ShortName, Is.EqualTo("50"));
            Assert.That(routes[idx].LongName, Is.EqualTo("Airport - Amargosa Valley"));
            Assert.That(routes[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(routes[idx].Type, Is.EqualTo(RouteTypeExtended.BusService));
            Assert.That(routes[idx].Color, Is.EqualTo(null));
            Assert.That(routes[idx].TextColor, Is.Null);
        }

        /// <summary>
        /// Tests parsing shapes.
        /// </summary>
        [Test]
        public void ParseShapes()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("shapes")));

            // test result.
            Assert.That(feed.Shapes, Is.Not.Null);
            var shapes = feed.Shapes.ToList();
            Assert.That(shapes.Count, Is.EqualTo(44));

            // @ 1: shape_id,shape_pt_lat,shape_pt_lon,shape_pt_sequence,shape_dist_traveled
            // @ 2: shape_1,37.754211,-122.197868,1,
            int idx = 0;
            Assert.That(shapes[idx].Id, Is.EqualTo("shape_1"));
            Assert.That(shapes[idx].Latitude, Is.EqualTo(37.754211));
            Assert.That(shapes[idx].Longitude, Is.EqualTo(-122.197868));
            Assert.That(shapes[idx].Sequence, Is.EqualTo(1));
            Assert.That(shapes[idx].DistanceTravelled, Is.Null);

            // @ 10: shape_3,37.73645,-122.19706,1,
            idx = 8;
            Assert.That(shapes[idx].Id, Is.EqualTo("shape_3"));
            Assert.That(shapes[idx].Latitude, Is.EqualTo(37.73645));
            Assert.That(shapes[idx].Longitude, Is.EqualTo(-122.19706));
            Assert.That(shapes[idx].Sequence, Is.EqualTo(1));
            Assert.That(shapes[idx].DistanceTravelled, Is.Null);
        }

        /// <summary>
        /// Tests parsing stops.
        /// </summary>
        [Test]
        public void ParseStops()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>();

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("stops")));

            // test result.
            Assert.That(feed.Stops, Is.Not.Null);
            var stops = feed.Stops.ToList();
            Assert.That(stops.Count, Is.EqualTo(9));

            // @ 1: stop_id,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url
            // @ 2: FUR_CREEK_RES,Furnace Creek Resort (Demo),,36.425288,-117.133162,,
            int idx = 0;
            Assert.That(stops[idx].Id, Is.EqualTo("FUR_CREEK_RES"));
            Assert.That(stops[idx].Name, Is.EqualTo("Furnace Creek Resort (Demo)"));
            Assert.That(stops[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(stops[idx].Latitude, Is.EqualTo(36.425288));
            Assert.That(stops[idx].Longitude, Is.EqualTo(-117.133162));
            Assert.That(stops[idx].Url, Is.EqualTo(string.Empty));

            // @ 10: AMV,Amargosa Valley (Demo),,36.641496,-116.40094,,
            idx = 8;
            Assert.That(stops[idx].Id, Is.EqualTo("AMV"));
            Assert.That(stops[idx].Name, Is.EqualTo("Amargosa Valley (Demo)"));
            Assert.That(stops[idx].Description, Is.EqualTo(string.Empty));
            Assert.That(stops[idx].Latitude, Is.EqualTo(36.641496));
            Assert.That(stops[idx].Longitude, Is.EqualTo(-116.40094));
            Assert.That(stops[idx].Url, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Tests parsing stops.
        /// </summary>
        [Test]
        public void ParseStopTimes()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>(false);

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("stop_times")));

            // test result.
            Assert.That(feed.StopTimes, Is.Not.Null);
            var stopTimes = feed.StopTimes.ToList();
            Assert.That(stopTimes.Count, Is.EqualTo(28));

            // @ 1: trip_id,arrival_time,departure_time,stop_id,stop_sequence,stop_headsign,pickup_type,drop_off_time,shape_dist_traveled
            // @ SORTED: AAMV1,8:00:00,8:00:00,BEATTY_AIRPORT,1
            int idx = 0;
            Assert.That(stopTimes[idx].TripId, Is.EqualTo("AAMV1"));
            Assert.That(stopTimes[idx].ArrivalTime, Is.EqualTo(new TimeOfDay() { Hours = 8 }));
            Assert.That(stopTimes[idx].DepartureTime, Is.EqualTo(new TimeOfDay() { Hours = 8 }));
            Assert.That(stopTimes[idx].StopId, Is.EqualTo("BEATTY_AIRPORT"));
            Assert.That(stopTimes[idx].StopSequence, Is.EqualTo(1));
            Assert.That(stopTimes[idx].StopHeadsign, Is.Empty);
            Assert.That(stopTimes[idx].PickupType, Is.Null);
            Assert.That(stopTimes[idx].DropOffType, Is.Null);
            Assert.That(stopTimes[idx].ShapeDistTravelled, Is.Null);
            Assert.That(stopTimes[idx].TimepointType, Is.EqualTo(TimePointType.None));

            // @ SORTED: CITY1,6:00:00,6:00:00,STAGECOACH,1,,,,,1
            idx = 16;
            Assert.That(stopTimes[idx].TripId, Is.EqualTo("CITY1"));
            Assert.That(stopTimes[idx].ArrivalTime, Is.EqualTo(new TimeOfDay() { Hours = 6, Minutes = 00 }));
            Assert.That(stopTimes[idx].DepartureTime, Is.EqualTo(new TimeOfDay() { Hours = 6, Minutes = 00 }));
            Assert.That(stopTimes[idx].StopId, Is.EqualTo("STAGECOACH"));
            Assert.That(stopTimes[idx].StopSequence, Is.EqualTo(1));
            Assert.That(stopTimes[idx].StopHeadsign, Is.EqualTo(string.Empty));
            Assert.That(stopTimes[idx].PickupType, Is.Null);
            Assert.That(stopTimes[idx].DropOffType, Is.Null);
            Assert.That(stopTimes[idx].ShapeDistTravelled, Is.Null);
            Assert.That(stopTimes[idx].TimepointType, Is.EqualTo(TimePointType.Exact));

            // @ SORTED: CITY1,,,NANAA,2,,,,,0
            idx = 17;
            Assert.That(stopTimes[idx].TripId, Is.EqualTo("CITY1"));
            Assert.That(stopTimes[idx].ArrivalTime, Is.EqualTo(new TimeOfDay() { Hours = 0, Minutes = 0, Seconds = 0 }));
            Assert.That(stopTimes[idx].DepartureTime, Is.EqualTo(new TimeOfDay() { Hours = 0, Minutes = 0, Seconds = 0 }));
            Assert.That(stopTimes[idx].StopId, Is.EqualTo("NANAA"));
            Assert.That(stopTimes[idx].StopSequence, Is.EqualTo(2));
            Assert.That(stopTimes[idx].StopHeadsign, Is.EqualTo(string.Empty));
            Assert.That(stopTimes[idx].PickupType, Is.Null);
            Assert.That(stopTimes[idx].DropOffType, Is.Null);
            Assert.That(stopTimes[idx].ShapeDistTravelled, Is.Null);
            Assert.That(stopTimes[idx].TimepointType, Is.EqualTo(TimePointType.Approximate));

            // @ SORTED: STBA,6:20:00,6:20:00,BEATTY_AIRPORT,2,,,,
            idx = 27;
            Assert.That(stopTimes[idx].TripId, Is.EqualTo("STBA"));
            Assert.That(stopTimes[idx].ArrivalTime, Is.EqualTo(new TimeOfDay() { Hours = 6, Minutes = 20 }));
            Assert.That(stopTimes[idx].DepartureTime, Is.EqualTo(new TimeOfDay() { Hours = 6, Minutes = 20 }));
            Assert.That(stopTimes[idx].StopId, Is.EqualTo("BEATTY_AIRPORT"));
            Assert.That(stopTimes[idx].StopSequence, Is.EqualTo(2));
            Assert.That(stopTimes[idx].StopHeadsign, Is.EqualTo(string.Empty));
            Assert.That(stopTimes[idx].PickupType, Is.Null);
            Assert.That(stopTimes[idx].DropOffType, Is.Null);
            Assert.That(stopTimes[idx].ShapeDistTravelled, Is.Null);
            Assert.That(stopTimes[idx].TimepointType, Is.EqualTo(TimePointType.None));
        }

        /// <summary>
        /// Tests parsing trips.
        /// </summary>
        [Test]
        public void ParseTrips()
        {
            // create the reader.
            GTFSReader<GTFSFeed> reader = new GTFSReader<GTFSFeed>(false);

            // build the source
            var source = this.BuildSource();

            // execute the reader.
            var feed = reader.Read(source, source.First(x => x.Name.Equals("trips")));

            // test result.
            Assert.That(feed.Trips, Is.Not.Null);
            var trips = feed.Trips.ToList();
            Assert.That(trips.Count, Is.EqualTo(11));

            // @ 1: route_id,service_id,trip_id,trip_headsign,direction_id,block_id,shape_id
            // @ 2: AB,FULLW,AB1,to Bullfrog,0,1,shape_1
            int idx = 0;
            Assert.That(trips[idx].RouteId, Is.EqualTo("AB"));
            Assert.That(trips[idx].ServiceId, Is.EqualTo("FULLW"));
            Assert.That(trips[idx].Id, Is.EqualTo("AB1"));
            Assert.That(trips[idx].Headsign, Is.EqualTo("to Bullfrog"));
            Assert.That(trips[idx].Direction, Is.EqualTo(DirectionType.OneDirection));
            Assert.That(trips[idx].BlockId, Is.EqualTo("1"));
            Assert.That(trips[idx].ShapeId, Is.EqualTo("shape_1"));

            // @ 10: BFC,FULLW,BFC1,to Furnace Creek Resort,0,1,shape_6
            idx = 5;
            Assert.That(trips[idx].RouteId, Is.EqualTo("BFC"));
            Assert.That(trips[idx].ServiceId, Is.EqualTo("FULLW"));
            Assert.That(trips[idx].Id, Is.EqualTo("BFC1"));
            Assert.That(trips[idx].Headsign, Is.EqualTo("to Furnace Creek Resort"));
            Assert.That(trips[idx].Direction, Is.EqualTo(DirectionType.OneDirection));
            Assert.That(trips[idx].BlockId, Is.EqualTo("1"));
            Assert.That(trips[idx].ShapeId, Is.EqualTo("shape_6"));

            // AAMV,WE,AAMV4,"""to Airport""",1,,shape_11
            idx = 10;
            Assert.That(trips[idx].RouteId, Is.EqualTo("AAMV"));
            Assert.That(trips[idx].ServiceId, Is.EqualTo("WE"));
            Assert.That(trips[idx].Id, Is.EqualTo("AAMV4"));
            Assert.That(trips[idx].Headsign, Is.EqualTo("\"to Airport\""));
            Assert.That(trips[idx].Direction, Is.EqualTo(DirectionType.OppositeDirection));
            Assert.That(trips[idx].BlockId, Is.EqualTo(""));
            Assert.That(trips[idx].ShapeId, Is.EqualTo("shape_11"));
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Builds the source from embedded streams.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IGTFSSourceFile> BuildSource()
        {
            var source = new List<IGTFSSourceFile>
            {
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.agency.txt"), "agency"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.calendar.txt"), "calendar"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.calendar_dates.txt"), "calendar_dates"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.fare_attributes.txt"), "fare_attributes"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.fare_rules.txt"), "fare_rules"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.frequencies.txt"), "frequencies"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.routes.txt"), "routes"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.shapes.txt"), "shapes"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.stop_times.txt"), "stop_times"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.stops.txt"), "stops"),
                new GTFSSourceFileStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GTFS.Test.sample_feed.trips.txt"), "trips")
            };
            return source;
        }

        #endregion Private Methods
    }
}