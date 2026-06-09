// The MIT License (MIT)

// Copyright (c) 2015 Ben Abelshausen

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
using GTFS.Entities.Collections;

namespace GTFS
{
    /// <summary>
    /// Represents an entire GTFS feed as it exists on disk.
    /// </summary>
    public class GTFSFeed : IGTFSFeed
    {
        #region Protected Fields

        /// <summary>
        /// Holds the feedinfo.
        /// </summary>
        protected FeedInfo _feedInfo;

        #endregion Protected Fields

        #region Public Constructors

        /// <summary>
        /// Creates a new feed.
        /// </summary>
        public GTFSFeed()
        {
            _feedInfo = new FeedInfo();
            this.Agencies = new UniqueEntityListCollection<Agency>([],
                (e, id) => e.Id == id);
            this.CalendarDates = new EntityListCollection<CalendarDate>([],
                (e, id) => e.ServiceId == id);
            this.Calendars = new EntityListCollection<Calendar>([],
                (e, id) => e.ServiceId == id);
            this.FareAttributes = new EntityListCollection<FareAttribute>([],
                (e, id) => e.FareId == id);
            this.FareRules = new UniqueEntityListCollection<FareRule>([],
                (e, id) => e.FareId == id);
            this.Frequencies = new EntityListCollection<Frequency>([],
                (e, id) => e.TripId == id);
            this.Routes = new UniqueEntityListCollection<Route>([],
                (e, id) => e.Id == id);
            this.Shapes = new EntityListCollection<Shape>([],
                (e, id) => e.Id == id);
            this.Stops = new UniqueEntityListCollection<Stop>([],
                (e, id) => e.Id == id);
            this.StopTimes = new StopTimeListCollection([]);
            this.Transfers = new TransferListCollection([]);
            this.Trips = new UniqueEntityListCollection<Trip>([],
                (e, id) => e.Id == id);
            this.Levels = new UniqueEntityListCollection<Level>([],
                (e, id) => e.Id == id);
            this.Pathways = new UniqueEntityListCollection<Pathway>([],
                (e, id) => e.Id == id);
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets the collection of .
        /// </summary>
        public IUniqueEntityCollection<Agency> Agencies
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of calendar dates.
        /// </summary>
        public IEntityCollection<CalendarDate> CalendarDates
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of calendars.
        /// </summary>
        public IEntityCollection<Calendar> Calendars
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of fare attributes.
        /// </summary>
        public IEntityCollection<FareAttribute> FareAttributes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of fare rules.
        /// </summary>
        public IUniqueEntityCollection<FareRule> FareRules
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of frequencies.
        /// </summary>
        public IEntityCollection<Frequency> Frequencies
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of levels.
        /// </summary>
        public IUniqueEntityCollection<Level> Levels
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of pathways.
        /// </summary>
        public IUniqueEntityCollection<Pathway> Pathways
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of routes.
        /// </summary>
        public IUniqueEntityCollection<Route> Routes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of shapes.
        /// </summary>
        public IEntityCollection<Shape> Shapes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of stops.
        /// </summary>
        public IUniqueEntityCollection<Stop> Stops
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of stop times.
        /// </summary>
        public IStopTimeCollection StopTimes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of transfers.
        /// </summary>
        public ITransferCollection Transfers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of trips.
        /// </summary>
        public IUniqueEntityCollection<Trip> Trips
        {
            get;
            private set;
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Gets the feed info.
        /// </summary>
        /// <returns></returns>
        public FeedInfo GetFeedInfo()
        {
            return _feedInfo;
        }

        /// <summary>
        /// Sets the feed info.
        /// </summary>
        /// <param name="feedInfo"></param>
        public void SetFeedInfo(FeedInfo feedInfo)
        {
            if (feedInfo != null)
            {
                _feedInfo.EndDate = feedInfo.EndDate;
                _feedInfo.Lang = feedInfo.Lang;
                _feedInfo.PublisherName = feedInfo.PublisherName;
                _feedInfo.PublisherUrl = feedInfo.PublisherUrl;
                _feedInfo.StartDate = feedInfo.StartDate;
                _feedInfo.Version = feedInfo.Version;
                _feedInfo.Tag = feedInfo.Tag;
            }
        }

        #endregion Public Methods
    }
}