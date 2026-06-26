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
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using GTFS.Exceptions;
using GTFS.Fields;
using GTFS.IO;
using GTFS.Logging;

namespace GTFS
{
    /// <summary>
    /// A GTFS reader that parses GTFS source files and populates a <typeparamref name="T"/> feed object.
    /// </summary>
    public class GTFSReader<T> where T : IGTFSFeed
    {
        #region Private Fields

        private readonly ILogger _logger;

        /// <summary>
        /// Flag making this reader very strict about the GTFS-spec.
        /// </summary>
        private readonly bool _strict = true;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Creates a new GTFS reader using non-strict mode.
        /// </summary>
        public GTFSReader()
            : this(false) { }

        /// <summary>
        /// Creates a new GTFS reader.
        /// </summary>
        /// <param name="strict">If <c>true</c>, the reader enforces strict GTFS specification compliance.</param>
        public GTFSReader(bool strict)
            : this(strict, Logger.CreateLogger(nameof(GTFSReader<T>))) { }

        /// <summary>
        /// Creates a new GTFS reader.
        /// </summary>
        /// <param name="strict">If <c>true</c>, the reader enforces strict GTFS specification compliance.</param>
        /// <param name="logger">The logger to use for diagnostic output.</param>
        public GTFSReader(bool strict, ILogger logger)
        {
            _strict = strict;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.DateTimeReader = (dateString) => DateTime.ParseExact(dateString, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            this.DateTimeWriter = (date) => date.ToString("yyyyMMdd");
            this.TimeOfDayReader = (timeOfDayString) =>
            {
                if (string.IsNullOrWhiteSpace(timeOfDayString))
                {
                    return new TimeOfDay()
                    {
                        Hours = 0,
                        Minutes = 0,
                        Seconds = 0
                    };
                }
                else if (!(timeOfDayString.Length == 8 || timeOfDayString.Length == 7))
                {
                    throw new ArgumentException($"Invalid timeOfDayString: {timeOfDayString}");
                }

                var timeOfDay = new TimeOfDay();
                if (timeOfDayString.Length == 8)
                {
                    timeOfDay.Hours = timeOfDayString.FastParse(0, 2);
                    timeOfDay.Minutes = timeOfDayString.FastParse(3, 2);
                    timeOfDay.Seconds = timeOfDayString.FastParse(6, 2);
                    return timeOfDay;
                }

                timeOfDay.Hours = timeOfDayString.FastParse(0, 1);
                timeOfDay.Minutes = timeOfDayString.FastParse(2, 2);
                timeOfDay.Seconds = timeOfDayString.FastParse(5, 2);
                return timeOfDay;
            };
            this.TimeOfDayWriter = (timeOfDay) => { throw new NotImplementedException(); };

            // initialize maps.
            this.AgencyMap = new FieldMap();
            this.CalendarDateMap = new FieldMap();
            this.CalendarMap = new FieldMap();
            this.FareAttributeMap = new FieldMap();
            this.FareRuleMap = new FieldMap();
            this.FeedInfoMap = new FieldMap();
            this.FrequencyMap = new FieldMap();
            this.RouteMap = new FieldMap();
            this.ShapeMap = new FieldMap();
            this.StopMap = new FieldMap();
            this.StopTimeMap = new FieldMap();
            this.TransferMap = new FieldMap();
            this.TripMap = new FieldMap();
            this.LevelMap = new FieldMap();
            this.PathwayMap = new FieldMap();
        }

        #endregion Public Constructors

        #region Protected Delegates

        /// <summary>
        /// A delegate to add an entity to a feed.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to add.</typeparam>
        /// <param name="entity">The entity to add.</param>
        protected delegate void EntityAddDelegate<TEntity>(TEntity entity);

        /// <summary>
        /// A delegate for parsing a single data row into a GTFS entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to parse.</typeparam>
        protected delegate TEntity EntityParseDelegate<TEntity>(T feed, GTFSSourceFileHeader header, string[] data)
            where TEntity : GTFSEntity;

        #endregion Protected Delegates

        #region Public Properties

        /// <summary>
        /// Gets the agency fieldmap.
        /// </summary>
        public FieldMap AgencyMap { get; private set; }

        /// <summary>
        /// Gets the calendar date fieldmap.
        /// </summary>
        public FieldMap CalendarDateMap { get; private set; }

        /// <summary>
        /// Gets the calendar fieldmap.
        /// </summary>
        public FieldMap CalendarMap { get; private set; }

        /// <summary>
        /// Gets or sets the date time reader.
        /// </summary>
        public Func<string, DateTime> DateTimeReader { get; set; }

        /// <summary>
        /// Gets or sets the date time writer.
        /// </summary>
        public Func<DateTime, string> DateTimeWriter { get; set; }

        /// <summary>
        /// Gets the fare attribute fieldmap.
        /// </summary>
        public FieldMap FareAttributeMap { get; private set; }

        /// <summary>
        /// Gets the fare rule fieldmap.
        /// </summary>
        public FieldMap FareRuleMap { get; private set; }

        /// <summary>
        /// Gets the feed info fieldmap.
        /// </summary>
        public FieldMap FeedInfoMap { get; private set; }

        /// <summary>
        /// Gets the frequency fieldmap.
        /// </summary>
        public FieldMap FrequencyMap { get; private set; }

        /// <summary>
        /// Gets the level fieldmap.
        /// </summary>
        public FieldMap LevelMap { get; private set; }

        /// <summary>
        /// Gets or sets the line preprocessor.
        /// </summary>
        public Func<string, string> LinePreprocessor { get; set; }

        /// <summary>
        /// Gets the pathway fieldmap.
        /// </summary>
        public FieldMap PathwayMap { get; private set; }

        /// <summary>
        /// Gets the route fieldmap.
        /// </summary>
        public FieldMap RouteMap { get; private set; }

        /// <summary>
        /// Gets the shape fieldmap.
        /// </summary>
        public FieldMap ShapeMap { get; private set; }

        /// <summary>
        /// Gets the stop fieldmap.
        /// </summary>
        public FieldMap StopMap { get; private set; }

        /// <summary>
        /// Gets the stop time fieldmap.
        /// </summary>
        public FieldMap StopTimeMap { get; private set; }

        /// <summary>
        /// Gets or sets the time of day reader.
        /// </summary>
        public Func<string, TimeOfDay> TimeOfDayReader { get; set; }

        /// <summary>
        /// Gets or sets the time of day writer.
        /// </summary>
        public Func<TimeOfDay, string> TimeOfDayWriter { get; set; }

        /// <summary>
        /// Gets the transfer fieldmap.
        /// </summary>
        public FieldMap TransferMap { get; private set; }

        /// <summary>
        /// Gets the trip fieldmap.
        /// </summary>
        public FieldMap TripMap { get; private set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Returns the file dependency tree, mapping each GTFS file name to the set of file names it depends on.
        /// </summary>
        /// <returns>
        /// A dictionary where each key is a GTFS file name and the value is the set of file names
        /// that must be read before it.
        /// </returns>
        public virtual Dictionary<string, HashSet<string>> GetDependencyTree()
        {
            var dependencyTree = new Dictionary<string, HashSet<string>>();

            // fare_rules => (routes)
            var dependencies = new HashSet<string>
            {
                "routes"
            };
            dependencyTree.Add("fare_rules", dependencies);

            // frequencies => (trips)
            dependencies = ["trips"];
            dependencyTree.Add("frequencies", dependencies);

            // routes => (agencies)
            dependencies = ["agency"];
            dependencyTree.Add("routes", dependencies);

            // stop_times => (trips)
            dependencies = ["trips"];
            dependencyTree.Add("stop_times", dependencies);

            // trips => (routes)
            dependencies = ["routes"];
            dependencyTree.Add("trips", dependencies);

            // transfers => (stops)
            dependencies = ["stops"];
            dependencyTree.Add("transfers", dependencies);

            return dependencyTree;
        }

        /// <summary>
        /// Returns a collection of all required GTFS file names.
        /// </summary>
        /// <returns>A collection of file names that must be present in the GTFS source.</returns>
        public virtual IEnumerable<string> GetRequiredFiles()
        {
            return ["agency", "stops", "routes", "trips", "stop_times"];
        }

        /// <summary>
        /// Returns a collection of required file sets. Each file set contains a
        /// number of files of which at least one should be in the source files set.
        /// </summary>
        /// <returns>
        /// A collection of file name arrays, where each array represents a group of files
        /// of which at least one must be present in the source.
        /// </returns>
        public virtual IEnumerable<string[]> GetRequiredFileSets()
        {
            return
            [
                ["calendar", "calendar_dates"]
            ];
        }

        /// <summary>
        /// Reads all files from the specified GTFS source into the given feed object,
        /// respecting the file dependency order.
        /// </summary>
        /// <param name="feed">The GTFS feed object to populate.</param>
        /// <param name="source">The collection of GTFS source files to read.</param>
        /// <param name="progress">An optional progress reporter receiving values between 0.0 and 1.0.</param>
        /// <returns>The populated GTFS feed object.</returns>
        public T Read(T feed, IEnumerable<IGTFSSourceFile> source, IProgress<double> progress = null)
        {
            // check if all required files are present.
            if (_strict)
            {
                var sourceFileNames = source.Select(x => x.Name).ToArray();

                var missingRequiredFiles = GetRequiredFiles()
                    .Where(x => !sourceFileNames.Contains(x))
                    .ToArray();

                if (missingRequiredFiles.Length > 0)
                {
                    // oeps, file was not found!
                    // TODO: check if we should not return all missing files in the exception?
                    throw new GTFSRequiredFileMissingException(missingRequiredFiles.First());
                }

                var missingRequiredFileSets = GetRequiredFileSets()
                    .Where(requiredFileSet => !requiredFileSet.Any(x => sourceFileNames.Contains(x)))
                    .ToArray();

                if (missingRequiredFileSets.Length > 0)
                {
                    // oeps, no file from file set was found!
                    // TODO: check if we should not return all missing file sets in the exception?
                    throw new GTFSRequiredFileSetMissingException(missingRequiredFileSets.First());
                }
            }

            var sourceArray = source.ToArray();

            // File sizes via reflection on path — not available here.
            // Use uniform file weighting: each file = 1/n of progress.
            // For record-level granularity within files, throttled reports come from Read<TEntity>.
            int filesTotal = sourceArray.Length;
            int filesCompleted = 0;

            // read files one-by-one and in the correct order based on the dependency tree.
            var readFiles = this.ReadCustomFilesBefore();
            var dependencyTree = this.GetDependencyTree();

            while (readFiles.Count < sourceArray.Length)
            {
                // select a new file based on the dependency tree.
                IGTFSSourceFile selectedFile = null;

                foreach (var file in source)
                {
                    if (!readFiles.Contains(file.Name))
                    {
                        // file has not been read yet!
                        if (!dependencyTree.TryGetValue(file.Name, out var dependencies))
                        {
                            // there is no entry in the dependency tree, file is independant.
                            selectedFile = file;
                            break;
                        }
                        else
                        {
                            // file depends on other file, check if they have been read already.
                            if (dependencies.All(x => readFiles.Contains(x)))
                            {
                                // all dependencies have been read.
                                selectedFile = file;
                                break;
                            }
                        }
                    }
                }

                // check if there is a next file.
                if (selectedFile == null)
                {
                    throw new Exception(
                        "Could not select a next file based on the current dependency tree and the current file list.");
                }

                // read the file.
                this.Read(selectedFile, feed, progress, filesCompleted, filesTotal);

                filesCompleted++;
                readFiles.Add(selectedFile.Name);
            }

            progress?.Report(1.0);

            return feed;
        }

        /// <summary>
        /// Reads one file and its dependencies from the specified GTFS source into the given feed object.
        /// </summary>
        /// <param name="feed">The GTFS feed object to populate.</param>
        /// <param name="source">The collection of all available GTFS source files.</param>
        /// <param name="file">The specific file to read, along with its dependencies.</param>
        /// <returns>The populated GTFS feed object.</returns>
        public T Read(T feed, IEnumerable<IGTFSSourceFile> source, IGTFSSourceFile file)
        {
            // build the files-to-read list from the dependencies.
            var dependencyTree = this.GetDependencyTree();
            var filesToRead = new List<string>
            {
                file.Name
            };

            // start with a queue of one file and traverse the dependency tree down.
            var queue = new Queue<string>();
            queue.Enqueue(file.Name);
            while (queue.Count > 0)
            {
                // dequeue current file.
                var currentFile = queue.Dequeue();
                filesToRead.Add(currentFile);

                // enqueue dependencies if any.
                if (dependencyTree.TryGetValue(currentFile, out var dependencies))
                {
                    foreach (var dependency in dependencies)
                    {
                        queue.Enqueue(dependency);
                    }
                }
            }

            // loop over the list but starting at the end.
            var readFiles = new HashSet<string>();
            for (int idx = filesToRead.Count - 1; idx >= 0; idx--)
            {
                string fileToRead = filesToRead[idx];
                if (!readFiles.Contains(fileToRead))
                {
                    // files can be in the list more than once.

                    // read the file.
                    this.Read(source.First(x => x.Name.Equals(fileToRead)), feed);
                    readFiles.Add(fileToRead);
                }
            }

            return feed;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Checks whether a required field is present in the file header and throws an exception if it is missing.
        /// </summary>
        /// <param name="header">The source file header to check.</param>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldMap">The field map used to resolve the actual column name.</param>
        /// <param name="column">The expected column name to verify.</param>
        protected virtual void CheckRequiredField(GTFSSourceFileHeader header, string name, FieldMap fieldMap,
            string column)
        {
            if (_strict)
            {
                // do not check the requeted fields stuff when not strict.
                string actual = fieldMap.GetActual(column);
                if (!header.HasColumn(actual))
                {
                    throw new GTFSRequiredFieldMissingException(name, actual);
                }
            }
        }

        /// <summary>
        /// Cleans a raw field value for subsequent parsing into a boolean, integer, double, or date.
        /// Trims surrounding whitespace and removes enclosing double quotes.
        /// </summary>
        /// <param name="value">The raw field value to clean.</param>
        /// <returns>
        /// The cleaned field value, or <c>null</c> if the value is empty or consists only of quote characters.
        /// </returns>
        protected virtual string CleanFieldValue(string value)
        {
            value = value.Trim();

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1].Replace("\"\"", "\"");

            return string.IsNullOrEmpty(value) || value.All(c => c == '"')
                ? null
                : value;
        }

        /// <summary>
        /// Parses a single agency row from a GTFS source file into an <see cref="Agency"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Agency"/> entity.</returns>
        protected virtual Agency ParseAgency(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.

            this.CheckRequiredField(header, header.Name, this.AgencyMap, "agency_name");
            this.CheckRequiredField(header, header.Name, this.AgencyMap, "agency_url");
            this.CheckRequiredField(header, header.Name, this.AgencyMap, "agency_timezone");

            // if we already have another agency, then the agency_id is required
            if (feed.Agencies.Count > 0)
            {
                CheckRequiredField(header, header.Name, AgencyMap, "agency_id");
            }

            // parse/set all fields.
            Agency agency = new Agency();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseAgencyField(header, agency, header.GetColumn(idx), data[idx]);
            }

            return agency;
        }

        /// <summary>
        /// Parses a single agency field and assigns the parsed value to the corresponding property of the entity.
        /// </summary>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="agency">The agency entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseAgencyField(GTFSSourceFileHeader header, Agency agency, string fieldName,
            string value)
        {
            switch (fieldName)
            {
                case "agency_id":
                    agency.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_name":
                    agency.Name = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_lang":
                    agency.LanguageCode = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_phone":
                    agency.Phone = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_timezone":
                    agency.Timezone = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_url":
                    agency.URL = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_email":
                    agency.Email = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_fare_url":
                    agency.FareURL = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single calendar date row from a GTFS source file into a <see cref="CalendarDate"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="CalendarDate"/> entity.</returns>
        protected virtual CalendarDate ParseCalendarDate(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.CalendarDateMap, "service_id");
            this.CheckRequiredField(header, header.Name, this.CalendarDateMap, "date");
            this.CheckRequiredField(header, header.Name, this.CalendarDateMap, "exception_type");

            // parse/set all fields.
            CalendarDate calendarDate = new CalendarDate();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseCalendarDateField(feed, header, calendarDate, header.GetColumn(idx), data[idx]);
            }

            return calendarDate;
        }

        /// <summary>
        /// Parses a single calendar date field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="calendarDate">The calendar date entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseCalendarDateField(T feed, GTFSSourceFileHeader header, CalendarDate calendarDate,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "service_id":
                    calendarDate.ServiceId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "date":
                    calendarDate.Date = this.ReadDateTime(header.Name, fieldName,
                        this.ParseFieldString(header.Name, fieldName, value));
                    break;

                case "exception_type":
                    calendarDate.ExceptionType = this.ParseFieldExceptionType(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single calendar field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="calendar">The calendar entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseCalendarField(T feed, GTFSSourceFileHeader header, Calendar calendar,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "service_id":
                    calendar.ServiceId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "monday":
                    calendar.Monday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "tuesday":
                    calendar.Tuesday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "wednesday":
                    calendar.Wednesday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "thursday":
                    calendar.Thursday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "friday":
                    calendar.Friday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "saturday":
                    calendar.Saturday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "sunday":
                    calendar.Sunday = this.ParseFieldBool(header.Name, fieldName, value).Value;
                    break;

                case "start_date":
                    calendar.StartDate = this.ReadDateTime(header.Name, fieldName,
                        this.ParseFieldString(header.Name, fieldName, value));
                    break;

                case "end_date":
                    calendar.EndDate = this.ReadDateTime(header.Name, fieldName,
                        this.ParseFieldString(header.Name, fieldName, value));
                    break;
            }
        }

        /// <summary>
        /// Parses a single calendar row from a GTFS source file into a <see cref="Calendar"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Calendar"/> entity.</returns>
        protected virtual Calendar ParseCalender(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "service_id");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "monday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "tuesday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "wednesday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "thursday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "friday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "saturday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "sunday");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "start_date");
            this.CheckRequiredField(header, header.Name, this.CalendarMap, "end_date");

            // parse/set all fields.
            Calendar calendar = new Calendar();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseCalendarField(feed, header, calendar, header.GetColumn(idx), data[idx]);
            }

            return calendar;
        }

        /// <summary>
        /// Parses a single fare attribute row from a GTFS source file into a <see cref="FareAttribute"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="FareAttribute"/> entity.</returns>
        protected virtual FareAttribute ParseFareAttribute(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.FareAttributeMap, "fare_id");
            this.CheckRequiredField(header, header.Name, this.FareAttributeMap, "price");
            this.CheckRequiredField(header, header.Name, this.FareAttributeMap, "currency_type");
            this.CheckRequiredField(header, header.Name, this.FareAttributeMap, "payment_method");
            this.CheckRequiredField(header, header.Name, this.FareAttributeMap, "transfers");

            // parse/set all fields.
            FareAttribute fareAttribute = new FareAttribute();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseFareAttributeField(feed, header, fareAttribute, header.GetColumn(idx), data[idx]);
            }

            return fareAttribute;
        }

        /// <summary>
        /// Parses a single fare attribute field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="fareAttribute">The fare attribute entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseFareAttributeField(T feed, GTFSSourceFileHeader header, FareAttribute fareAttribute,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "fare_id":
                    fareAttribute.FareId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "price":
                    fareAttribute.Price = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "currency_type":
                    fareAttribute.CurrencyType = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "payment_method":
                    fareAttribute.PaymentMethod = this.ParseFieldPaymentMethodType(header.Name, fieldName, value);
                    break;

                case "transfers":
                    fareAttribute.Transfers = this.ParseFieldUInt(header.Name, fieldName, value);
                    break;

                case "agency_id":
                    fareAttribute.AgencyId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "transfer_duration":
                    fareAttribute.TransferDuration = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single fare rule row from a GTFS source file into a <see cref="FareRule"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="FareRule"/> entity.</returns>
        protected virtual FareRule ParseFareRule(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.FareRuleMap, "fare_id");

            // parse/set all fields.
            FareRule fareRule = new FareRule();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseFareRuleField(feed, header, fareRule, header.GetColumn(idx), data[idx]);
            }

            return fareRule;
        }

        /// <summary>
        /// Parses a single fare rule field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="fareRule">The fare rule entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseFareRuleField(T feed, GTFSSourceFileHeader header, FareRule fareRule,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "fare_id":
                    fareRule.FareId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_id":
                    fareRule.RouteId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "origin_id":
                    fareRule.OriginId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "destination_id":
                    fareRule.DestinationId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "contains_id":
                    fareRule.ContainsId = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single feed info row from a GTFS source file into a <see cref="FeedInfo"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="FeedInfo"/> entity.</returns>
        protected virtual FeedInfo ParseFeedInfo(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_publisher_name");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_publisher_url");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_lang");

            // parse/set all fields.
            FeedInfo feedInfo = new FeedInfo();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseFeedInfoField(header, feedInfo, header.GetColumn(idx), data[idx]);
            }

            return feedInfo;
        }

        /// <summary>
        /// Parses a color field value into an ARGB integer.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value representing a color (e.g., a hex string).</param>
        /// <returns>The ARGB color value as an integer, or <c>null</c> if the value is empty.</returns>
        protected virtual int? ParseFieldColor(string name, string fieldName, string value)
        {
            // clean first.
            value = this.CleanFieldValue(value);

            try
            {
                return value.ToArgbInt();
            }
            catch (Exception ex)
            {
                // hmm, some unknow exception, field not in correct format, give inner exception as a clue.
                throw new GTFSParseException(name, fieldName, value, ex);
            }
        }

        /// <summary>
        /// Parses a floating-point field value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value.</param>
        /// <returns>The parsed <see cref="double"/> value, or <c>null</c> if the value is empty or whitespace.</returns>
        protected virtual double? ParseFieldDouble(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
            {
                // parsing failed!
                if (_strict)
                {
                    throw new GTFSParseException(name, fieldName, value);
                }

                return null;
            }

            return result;
        }

        /// <summary>
        /// Parses an exception type field value into an <see cref="ExceptionType"/> enumeration value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value (<c>"1"</c> = Added, <c>"2"</c> = Removed).</param>
        /// <returns>The corresponding <see cref="ExceptionType"/> value.</returns>
        protected virtual ExceptionType ParseFieldExceptionType(string name, string fieldName, string value)
        {
            // clean first.
            value = this.CleanFieldValue(value);

            //A value of 1 indicates that service has been added for the specified date.
            //A value of 2 indicates that service has been removed for the specified date.

            return value switch
            {
                "1" => ExceptionType.Added,
                "2" => ExceptionType.Removed,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        /// <summary>
        /// Parses an integer field value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value.</param>
        /// <returns>The parsed <see cref="int"/> value, or <c>null</c> if the value is empty or whitespace.</returns>
        protected virtual int? ParseFieldInt(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            if (!int.TryParse(value, out var result))
            {
                // parsing failed!
                throw new GTFSParseException(name, fieldName, value);
            }

            return result;
        }

        /// <summary>
        /// Parses a payment method type field value into a <see cref="PaymentMethodType"/> enumeration value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value (<c>"0"</c> = OnBoard, <c>"1"</c> = BeforeBoarding).</param>
        /// <returns>The corresponding <see cref="PaymentMethodType"/> value.</returns>
        protected virtual PaymentMethodType ParseFieldPaymentMethodType(string name, string fieldName, string value)
        {
            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Fare is paid on board.
            //1 - Fare must be paid before boarding.

            return value switch
            {
                "0" => PaymentMethodType.OnBoard,
                "1" => PaymentMethodType.BeforeBoarding,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        /// <summary>
        /// Parses a route type field value into a <see cref="RouteTypeExtended"/> enumeration value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value representing the route type code.</param>
        /// <returns>The corresponding <see cref="RouteTypeExtended"/> value.</returns>
        protected virtual RouteTypeExtended ParseFieldRouteType(string name, string fieldName, string value)
        {
            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Tram, Streetcar, Light rail. Any light rail or street level system within a metropolitan area.
            //1 - Subway, Metro. Any underground rail system within a metropolitan area.
            //2 - Rail. Used for intercity or long-distance travel.
            //3 - Bus. Used for short- and long-distance bus routes.
            //4 - Ferry. Used for short- and long-distance boat service.
            //5 - Cable car. Used for street-level cable cars where the cable runs beneath the car.
            //6 - Gondola, Suspended cable car. Typically used for aerial cable cars where the car is suspended from the cable.
            //7 - Funicular. Any rail system designed for steep inclines.
            //11 - Trolleybus. Electric buses that draw power from overhead wires using poles.
            //12 - Monorail. Railway in which the track consists of a single rail or a beam.

            switch (value)
            {
                case "0":
                    return RouteType.Tram.ToExtended();

                case "1":
                    return RouteType.SubwayMetro.ToExtended();

                case "2":
                    return RouteType.Rail.ToExtended();

                case "3":
                    return RouteType.Bus.ToExtended();

                case "4":
                    return RouteType.Ferry.ToExtended();

                case "5":
                    return RouteType.CableCar.ToExtended();

                case "6":
                    return RouteType.Gondola.ToExtended();

                case "7":
                    return RouteType.Funicular.ToExtended();

                case "11":
                    return RouteType.Trolleybus.ToExtended();

                case "12":
                    return RouteType.Gondola.ToExtended();
            }

            if (!int.TryParse(value, out var routeTypeValue))
            {
                throw new GTFSParseException(name, fieldName, value);
            }

            try
            {
                return (RouteTypeExtended)routeTypeValue;
            }
            catch
            {
                throw new GTFSParseException(name, fieldName, value);
            }
        }

        /// <summary>
        /// Parses a string field value by trimming surrounding whitespace.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value.</param>
        /// <returns>
        /// The trimmed string value, or <c>null</c> if the value is empty or consists only of quote characters.
        /// </returns>
        protected virtual string ParseFieldString(string name, string fieldName, string value)
        {
            value = value.Trim();

            return string.IsNullOrEmpty(value) || value.All(c => c == '"')
                ? null
                : value;
        }

        /// <summary>
        /// Parses a transfer type field value into a <see cref="TransferType"/> enumeration value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value.</param>
        /// <returns>The corresponding <see cref="TransferType"/> value.</returns>
        protected virtual TransferType ParseFieldTransferType(string name, string fieldName, string value)
        {
            // clean first.
            value = this.CleanFieldValue(value);

            // - 0 or (empty) - This is a recommended transfer point between two routes.
            // - 1 - This is a timed transfer point between two routes. The departing vehicle is expected to wait for the arriving one, with sufficient time for a passenger to transfer between routes.
            // - 2 - This transfer requires a minimum amount of time between arrival and departure to ensure a connection. The time required to transfer is specified by min_transfer_time.
            // - 3 - Transfers are not possible between routes at this location.

            return value switch
            {
                "0" or "" => TransferType.Recommended,
                "1" => TransferType.TimedTransfer,
                "2" => TransferType.MinimumTime,
                "3" => TransferType.NotPossible,
                "4" => TransferType.InSeat,
                "5" => TransferType.InSeatNotAllowed,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        /// <summary>
        /// Parses an unsigned integer field value.
        /// </summary>
        /// <param name="name">The name of the source file, used in error messages.</param>
        /// <param name="fieldName">The name of the field being parsed.</param>
        /// <param name="value">The raw field value.</param>
        /// <returns>The parsed <see cref="uint"/> value, or <c>null</c> if the value is empty or whitespace.</returns>
        protected virtual uint? ParseFieldUInt(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            if (!uint.TryParse(value, out var result))
            {
                // parsing failed!
                throw new GTFSParseException(name, fieldName, value);
            }

            return result;
        }

        /// <summary>
        /// Parses a single frequency row from a GTFS source file into a <see cref="Frequency"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Frequency"/> entity.</returns>
        protected virtual Frequency ParseFrequency(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "trip_id");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "start_time");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "end_time");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "headway_secs");

            // parse/set all fields.
            Frequency frequency = new Frequency();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseFrequencyField(feed, header, frequency, header.GetColumn(idx), data[idx]);
            }

            return frequency;
        }

        /// <summary>
        /// Parses a single frequency field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="frequency">The frequency entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseFrequencyField(T feed, GTFSSourceFileHeader header, Frequency frequency,
            string fieldName, string value)
        {
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "trip_id");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "start_time");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "end_time");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "headway_secs");
            switch (fieldName)
            {
                case "trip_id":
                    frequency.TripId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "start_time":
                    frequency.StartTime = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "end_time":
                    frequency.EndTime = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "headway_secs":
                    frequency.HeadwaySecs = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "exact_times":
                    frequency.ExactTimes = this.ParseFieldBool(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single level row from a GTFS source file into a <see cref="Level"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Level"/> entity.</returns>
        protected virtual Level ParseLevel(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.

            this.CheckRequiredField(header, header.Name, this.LevelMap, "level_id");
            this.CheckRequiredField(header, header.Name, this.LevelMap, "level_index");

            // parse/set all fields.
            Level level = new Level();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseLevelField(header, level, header.GetColumn(idx), data[idx]);
            }

            return level;
        }

        /// <summary>
        /// Parses a single level field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="level">The level entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseLevelField(GTFSSourceFileHeader header, Level level, string fieldName, string value)
        {
            switch (fieldName)
            {
                case "level_id":
                    level.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "level_index":
                    level.Index = (double)this.ParseFieldDouble(header.Name, fieldName, value);
                    break;

                case "level_name":
                    level.Name = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single pathway row from a GTFS source file into a <see cref="Pathway"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Pathway"/> entity.</returns>
        protected virtual Pathway ParsePathway(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.

            this.CheckRequiredField(header, header.Name, this.PathwayMap, "pathway_id");
            this.CheckRequiredField(header, header.Name, this.PathwayMap, "from_stop_id");
            this.CheckRequiredField(header, header.Name, this.PathwayMap, "to_stop_id");
            this.CheckRequiredField(header, header.Name, this.PathwayMap, "pathway_mode");
            this.CheckRequiredField(header, header.Name, this.PathwayMap, "is_bidirectional");

            // parse/set all fields.
            Pathway pathway = new Pathway();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParsePathwayField(header, pathway, header.GetColumn(idx), data[idx]);
            }

            return pathway;
        }

        /// <summary>
        /// Parses a single pathway field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="pathway">The pathway entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParsePathwayField(GTFSSourceFileHeader header, Pathway pathway, string fieldName,
            string value)
        {
            switch (fieldName)
            {
                case "pathway_id":
                    pathway.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "from_stop_id":
                    pathway.FromStopId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "to_stop_id":
                    pathway.ToStopId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "pathway_mode":
                    pathway.PathwayMode = (PathwayMode)this.ParseFieldPathwayMode(header.Name, fieldName, value);
                    break;

                case "is_bidirectional":
                    pathway.IsBidirectional =
                        (IsBidirectional)this.ParseFieldIsBidirectional(header.Name, fieldName, value);
                    break;

                case "length":
                    pathway.Length = this.ParseFieldDouble(header.Name, fieldName, value);
                    break;

                case "traversal_time":
                    pathway.TraversalTime = this.ParseFieldInt(header.Name, fieldName, value);
                    break;

                case "stair_count":
                    pathway.StairCount = this.ParseFieldInt(header.Name, fieldName, value);
                    break;

                case "max_slope":
                    pathway.MaxSlope = this.ParseFieldDouble(header.Name, fieldName, value);
                    break;

                case "min_width":
                    pathway.MinWidth = this.ParseFieldDouble(header.Name, fieldName, value);
                    break;

                case "signposted_as":
                    pathway.SignpostedAs = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "reversed_signposted_as":
                    pathway.ReversedSignpostedAs = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single route row from a GTFS source file into a <see cref="Route"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Route"/> entity.</returns>
        protected virtual Route ParseRoute(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.RouteMap, "route_id");

            this.CheckRequiredField(header, header.Name, this.RouteMap, "route_short_name");
            this.CheckRequiredField(header, header.Name, this.RouteMap, "route_long_name");
            this.CheckRequiredField(header, header.Name, this.RouteMap, "route_type");

            // parse/set all fields.
            Route route = new Route();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseRouteField(feed, header, route, header.GetColumn(idx), data[idx]);
            }

            return route;
        }

        /// <summary>
        /// Parses a single route field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="route">The route entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseRouteField(T feed, GTFSSourceFileHeader header, Route route, string fieldName,
            string value)
        {
            switch (fieldName)
            {
                case "route_id":
                    route.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "agency_id":
                    route.AgencyId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_short_name":
                    route.ShortName = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_long_name":
                    route.LongName = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_desc":
                    route.Description = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_type":
                    route.Type = this.ParseFieldRouteType(header.Name, fieldName, value);
                    break;

                case "route_url":
                    route.Url = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_color":
                    route.Color = this.ParseFieldColor(header.Name, fieldName, value);
                    break;

                case "route_text_color":
                    route.TextColor = this.ParseFieldColor(header.Name, fieldName, value);
                    break;

                case "continuous_pickup":
                    route.ContinuousPickup = this.ParseFieldContinuousPickupDropOff(header.Name, fieldName, value);
                    break;

                case "continuous_drop_off":
                    route.ContinuousDropOff = this.ParseFieldContinuousPickupDropOff(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single shape row from a GTFS source file into a <see cref="Shape"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Shape"/> entity.</returns>
        protected virtual Shape ParseShape(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.ShapeMap, "shape_id");
            this.CheckRequiredField(header, header.Name, this.ShapeMap, "shape_pt_lat");
            this.CheckRequiredField(header, header.Name, this.ShapeMap, "shape_pt_lon");
            this.CheckRequiredField(header, header.Name, this.ShapeMap, "shape_pt_sequence");

            // parse/set all fields.
            Shape shape = new Shape();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseShapeField(feed, header, shape, header.GetColumn(idx), data[idx]);
            }

            return shape;
        }

        /// <summary>
        /// Parses a single shape field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="shape">The shape entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseShapeField(T feed, GTFSSourceFileHeader header, Shape shape, string fieldName,
            string value)
        {
            switch (fieldName)
            {
                case "shape_id":
                    shape.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "shape_pt_lat":
                    shape.Latitude = this.ParseFieldDouble(header.Name, fieldName, value).Value;
                    break;

                case "shape_pt_lon":
                    shape.Longitude = this.ParseFieldDouble(header.Name, fieldName, value).Value;
                    break;

                case "shape_pt_sequence":
                    shape.Sequence = this.ParseFieldUInt(header.Name, fieldName, value).Value;
                    break;

                case "shape_dist_traveled":
                    shape.DistanceTravelled = this.ParseFieldDouble(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single stop row from a GTFS source file into a <see cref="Stop"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Stop"/> entity.</returns>
        protected virtual Stop ParseStop(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.StopMap, "stop_id");
            this.CheckRequiredField(header, header.Name, this.StopMap, "stop_name");
            this.CheckRequiredField(header, header.Name, this.StopMap, "stop_lat");
            this.CheckRequiredField(header, header.Name, this.StopMap, "stop_lon");

            // parse/set all fields.
            Stop stop = new Stop();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseStopField(feed, header, stop, header.GetColumn(idx), data[idx]);
            }

            return stop;
        }

        /// <summary>
        /// Parses a single stop field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="stop">The stop entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseStopField(T feed, GTFSSourceFileHeader header, Stop stop, string fieldName,
            string value)
        {
            switch (fieldName.Trim())
            {
                case "stop_id":
                    stop.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_code":
                    stop.Code = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_name":
                    stop.Name = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_desc":
                    stop.Description = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_lat":
                    var lat = this.ParseFieldDouble(header.Name, fieldName, value);

                    if (this._strict && !lat.HasValue)
                    {
                        throw new GTFSParseException(header.Name, fieldName, value);
                    }

                    stop.Latitude = lat ?? 0d;
                    break;

                case "stop_lon":
                    var lon = this.ParseFieldDouble(header.Name, fieldName, value);

                    if (this._strict && !lon.HasValue)
                    {
                        throw new GTFSParseException(header.Name, fieldName, value);
                    }

                    stop.Longitude = lon ?? 0d;
                    break;

                case "zone_id":
                    stop.Zone = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_url":
                    stop.Url = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "location_type":
                    stop.LocationType = this.ParseFieldLocationType(header.Name, fieldName, value);
                    break;

                case "parent_station":
                    stop.ParentStation = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_timezone":
                    stop.Timezone = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "wheelchair_boarding":
                    stop.WheelchairBoarding = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "level_id":
                    stop.LevelId = this.ParseFieldString(header.Name, fieldName, value);
                    if (stop.LevelId?.Contains(".0") == true)
                    {
                        stop.LevelId = stop.LevelId.Replace(".0", "");
                    }
                    break;

                case "platform_code":
                    stop.PlatformCode = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single stop time row from a GTFS source file into a <see cref="StopTime"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="StopTime"/> entity.</returns>
        protected virtual StopTime ParseStopTime(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.StopTimeMap, "trip_id");
            this.CheckRequiredField(header, header.Name, this.StopTimeMap, "arrival_time");
            this.CheckRequiredField(header, header.Name, this.StopTimeMap, "departure_time");
            this.CheckRequiredField(header, header.Name, this.StopTimeMap, "stop_id");
            this.CheckRequiredField(header, header.Name, this.StopTimeMap, "stop_sequence");

            // parse/set all fields.
            StopTime stopTime = new StopTime();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseStopTimeField(feed, header, stopTime, header.GetColumn(idx), data[idx]);
            }

            return stopTime;
        }

        /// <summary>
        /// Parses a single stop time field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="stopTime">The stop time entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseStopTimeField(T feed, GTFSSourceFileHeader header, StopTime stopTime,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "trip_id":
                    stopTime.TripId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "arrival_time":
                    stopTime.ArrivalTime = this.ReadTimeOfDay(header.Name, fieldName,
                        this.ParseFieldString(header.Name, fieldName, value));
                    break;

                case "departure_time":
                    stopTime.DepartureTime = this.ReadTimeOfDay(header.Name, fieldName,
                        this.ParseFieldString(header.Name, fieldName, value));
                    break;

                case "stop_id":
                    stopTime.StopId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "stop_sequence":
                    stopTime.StopSequence = this.ParseFieldUInt(header.Name, fieldName, value).Value;
                    break;

                case "stop_headsign":
                    stopTime.StopHeadsign = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "pickup_type":
                    stopTime.PickupType = this.ParseFieldPickupType(header.Name, fieldName, value);
                    break;

                case "drop_off_type":
                    stopTime.DropOffType = this.ParseFieldDropOffType(header.Name, fieldName, value);
                    break;

                case "shape_dist_traveled":
                    stopTime.ShapeDistTravelled = this.ParseFieldDouble(header.Name, fieldName, value);
                    break;

                case "timepoint":
                    stopTime.TimepointType = this.ParseFieldTimepointType(header.Name, fieldName, value);
                    break;

                case "continuous_pickup":
                    stopTime.ContinuousPickup = this.ParseFieldContinuousPickupDropOff(header.Name, fieldName, value);
                    break;

                case "continuous_drop_off":
                    stopTime.ContinuousDropOff = this.ParseFieldContinuousPickupDropOff(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single transfer row from a GTFS source file into a <see cref="Transfer"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Transfer"/> entity.</returns>
        protected virtual Transfer ParseTransfer(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.TransferMap, "from_stop_id");
            this.CheckRequiredField(header, header.Name, this.TransferMap, "to_stop_id");
            this.CheckRequiredField(header, header.Name, this.TransferMap, "transfer_type");

            // parse/set all fields.
            var transfer = new Transfer();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseTransferField(feed, header, transfer, header.GetColumn(idx), data[idx]);
            }

            return transfer;
        }

        /// <summary>
        /// Parses a single transfer field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="transfer">The transfer entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseTransferField(T feed, GTFSSourceFileHeader header, Transfer transfer,
            string fieldName, string value)
        {
            switch (fieldName)
            {
                case "from_stop_id":
                    transfer.FromStopId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "to_stop_id":
                    transfer.ToStopId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "transfer_type":
                    transfer.TransferType = this.ParseFieldTransferType(header.Name, fieldName, value);
                    break;

                case "min_transfer_time":
                    transfer.MinimumTransferTime = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Parses a single trip row from a GTFS source file into a <see cref="Trip"/> entity.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="data">The raw field values for the current row.</param>
        /// <returns>A populated <see cref="Trip"/> entity.</returns>
        protected virtual Trip ParseTrip(T feed, GTFSSourceFileHeader header, string[] data)
        {
            // check required fields.
            this.CheckRequiredField(header, header.Name, this.TripMap, "trip_id");
            this.CheckRequiredField(header, header.Name, this.TripMap, "route_id");
            this.CheckRequiredField(header, header.Name, this.TripMap, "service_id");

            // parse/set all fields.
            Trip trip = new Trip();
            for (int idx = 0; idx < data.Length; idx++)
            {
                this.ParseTripField(feed, header, trip, header.GetColumn(idx), data[idx]);
            }

            return trip;
        }

        /// <summary>
        /// Parses a single trip field and assigns the parsed value to the corresponding property.
        /// </summary>
        /// <param name="feed">The GTFS feed being populated.</param>
        /// <param name="header">The file header containing column index information.</param>
        /// <param name="trip">The trip entity to populate.</param>
        /// <param name="fieldName">The name of the field to parse.</param>
        /// <param name="value">The raw field value.</param>
        protected virtual void ParseTripField(T feed, GTFSSourceFileHeader header, Trip trip, string fieldName,
            string value)
        {
            switch (fieldName)
            {
                case "trip_id":
                    trip.Id = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "route_id":
                    trip.RouteId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "service_id":
                    trip.ServiceId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "trip_headsign":
                    trip.Headsign = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "trip_short_name":
                    trip.ShortName = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "direction_id":
                    trip.Direction = this.ParseFieldDirectionType(header.Name, fieldName, value);
                    break;

                case "block_id":
                    trip.BlockId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "shape_id":
                    trip.ShapeId = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "wheelchair_accessible":
                    trip.AccessibilityType = this.ParseFieldAccessibilityType(header.Name, fieldName, value);
                    break;
            }
        }

        /// <summary>
        /// Reads the given GTFS source file, parses all rows, and adds the resulting entities to the feed.
        /// Reports progress relative to the file's position within the overall file set.
        /// </summary>
        /// <param name="file">The GTFS source file to read.</param>
        /// <param name="feed">The GTFS feed object to populate.</param>
        /// <param name="progress">An optional progress reporter receiving values between 0.0 and 1.0.</param>
        /// <param name="filesCompleted">The number of files already processed before this one.</param>
        /// <param name="filesTotal">The total number of files to process.</param>
        protected virtual void Read(IGTFSSourceFile file, T feed, IProgress<double> progress, int filesCompleted, int filesTotal)
        {
            switch (file.Name.ToLower())
            {
                case "agency":
                    this.Read(file, feed, this.ParseAgency, feed.Agencies.Add, progress, filesCompleted, filesTotal);
                    break;

                case "calendar":
                    this.Read(file, feed, this.ParseCalender, feed.Calendars.Add, progress, filesCompleted, filesTotal);
                    break;

                case "calendar_dates":
                    this.Read(file, feed, this.ParseCalendarDate, feed.CalendarDates.Add, progress, filesCompleted, filesTotal);
                    break;

                case "fare_attributes":
                    this.Read(file, feed, this.ParseFareAttribute, feed.FareAttributes.Add, progress, filesCompleted, filesTotal);
                    break;

                case "fare_rules":
                    this.Read(file, feed, this.ParseFareRule, feed.FareRules.Add, progress, filesCompleted, filesTotal);
                    break;

                case "feed_info":
                    this.Read(file, feed, this.ParseFeedInfo, feed.SetFeedInfo, progress, filesCompleted, filesTotal);
                    break;

                case "routes":
                    this.Read(file, feed, this.ParseRoute, feed.Routes.Add, progress, filesCompleted, filesTotal);
                    break;

                case "shapes":
                    this.Read(file, feed, this.ParseShape, feed.Shapes.Add, progress, filesCompleted, filesTotal);
                    break;

                case "stops":
                    this.Read(file, feed, this.ParseStop, feed.Stops.Add, progress, filesCompleted, filesTotal);
                    break;

                case "stop_times":
                    this.Read(file, feed, this.ParseStopTime, feed.StopTimes.Add, progress, filesCompleted, filesTotal);
                    break;

                case "trips":
                    this.Read(file, feed, this.ParseTrip, feed.Trips.Add, progress, filesCompleted, filesTotal);
                    break;

                case "transfers":
                    this.Read(file, feed, this.ParseTransfer, feed.Transfers.Add, progress, filesCompleted, filesTotal);
                    break;

                case "frequencies":
                    this.Read(file, feed, this.ParseFrequency, feed.Frequencies.Add, progress, filesCompleted, filesTotal);
                    break;

                case "levels":
                    this.Read(file, feed, this.ParseLevel, feed.Levels.Add, progress, filesCompleted, filesTotal);
                    break;

                default:
                    // Console.WriteLine($"The GTFS data does not contain a {file.Name} file.");
                    break;
            }
        }

        /// <summary>
        /// Reads the given GTFS source file and adds all parsed entities to the feed.
        /// </summary>
        /// <param name="file">The GTFS source file to read.</param>
        /// <param name="feed">The GTFS feed object to populate.</param>
        protected virtual void Read(IGTFSSourceFile file, T feed)
            => this.Read(file: file, feed: feed, progress: null, filesCompleted: 0, filesTotal: 1);

        /// <summary>
        /// Returns the set of file names that are considered already read before the standard read loop begins.
        /// Override this method in a subclass to pre-populate the set with any custom files processed beforehand.
        /// </summary>
        /// <returns>
        /// A <see cref="HashSet{T}"/> of file names that have already been processed.
        /// </returns>
        protected virtual HashSet<string> ReadCustomFilesBefore()
        {
            return [];
        }

        #endregion Protected Methods

        #region Private Methods

        private void ParseFeedInfoField(GTFSSourceFileHeader header, FeedInfo feedInfo, string fieldName, string value)
        {
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_publisher_name");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_publisher_url");
            this.CheckRequiredField(header, header.Name, this.FrequencyMap, "feed_lang");

            switch (fieldName)
            {
                case "feed_publisher_name":
                    feedInfo.PublisherName = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "feed_publisher_url":
                    feedInfo.PublisherUrl = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "feed_lang":
                    feedInfo.Lang = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "feed_start_date":
                    feedInfo.StartDate = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "feed_end_date":
                    feedInfo.EndDate = this.ParseFieldString(header.Name, fieldName, value);
                    break;

                case "feed_version":
                    feedInfo.Version = this.ParseFieldString(header.Name, fieldName, value);
                    break;
            }
        }

        private WheelchairAccessibilityType? ParseFieldAccessibilityType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 (or empty) - indicates that there is no accessibility information for the trip
            //1 - indicates that the vehicle being used on this particular trip can accommodate at least one rider in a wheelchair
            //2 - indicates that no riders in wheelchairs can be accommodated on this trip

            return value switch
            {
                "0" => (WheelchairAccessibilityType?)WheelchairAccessibilityType.NoInformation,
                "1" => (WheelchairAccessibilityType?)WheelchairAccessibilityType.SomeAccessibility,
                "2" => (WheelchairAccessibilityType?)WheelchairAccessibilityType.NoAccessibility,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private bool? ParseFieldBool(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            return value switch
            {
                "0" => false,
                "1" => (bool?)true,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private ContinuousPickupDropOff? ParseFieldContinuousPickupDropOff(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = this.CleanFieldValue(value);

            return value switch
            {
                "0" => ContinuousPickupDropOff.Continuous,
                "1" => ContinuousPickupDropOff.NoContinuous,
                "2" => ContinuousPickupDropOff.PhoneAgency,
                "3" => ContinuousPickupDropOff.CoordinateWithDriver,
                _ => throw new GTFSParseException(name, fieldName, value)
            };
        }

        private DirectionType? ParseFieldDirectionType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 - travel in one direction (e.g. outbound travel)
            //1 - travel in the opposite direction (e.g. inbound travel)

            return value switch
            {
                "0" => (DirectionType?)DirectionType.OneDirection,
                "1" => (DirectionType?)DirectionType.OppositeDirection,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private DropOffType? ParseFieldDropOffType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Regularly scheduled drop off
            //1 - No drop off available
            //2 - Must phone agency to arrange drop off
            //3 - Must coordinate with driver to arrange drop off

            return value switch
            {
                "0" => (DropOffType?)DropOffType.Regular,
                "1" => (DropOffType?)DropOffType.NoDropOff,
                "2" => (DropOffType?)DropOffType.PhoneForDropOff,
                "3" => (DropOffType?)DropOffType.DriverForDropOff,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private IsBidirectional? ParseFieldIsBidirectional(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Unidirectional pathway, it can only be used from from_stop_id to to_stop_id.
            //1 - Bidirectional pathway, it can be used in the two directions.

            return value switch
            {
                "0" => (IsBidirectional?)IsBidirectional.Unidirectional,
                "1" => (IsBidirectional?)IsBidirectional.Bidirectional,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private LocationType? ParseFieldLocationType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 or blank - Stop. A location where passengers board or disembark from a transit vehicle.
            //1 - Station. A physical structure or area that contains one or more stop.
            //2 - Entrance/Exit.
            //3 - Generic Node.
            //4 - Boarding Area.

            switch (value)
            {
                case "0":
                    return LocationType.Stop;

                case "1":
                    return LocationType.Station;

                case "2":
                    return LocationType.EntranceExit;

                case "3":
                    return LocationType.GenericNode;

                case "4":
                    return LocationType.BoardingArea;
            }

            if (_strict)
            {
                // invalid location type.
                throw new GTFSParseException(name, fieldName, value);
            }

            return null;
        }

        private PathwayMode? ParseFieldPathwayMode(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //1 - walkway
            //2 - stairs
            //3 - moving sidewalk/travelator
            //4 - escalator
            //5 - elevator
            //6 - fare gate
            //7 - exit gate

            return value switch
            {
                "1" => (PathwayMode?)PathwayMode.Walkway,
                "2" => (PathwayMode?)PathwayMode.Stairs,
                "3" => (PathwayMode?)PathwayMode.Travelator,
                "4" => (PathwayMode?)PathwayMode.Escalator,
                "5" => (PathwayMode?)PathwayMode.Elevator,
                "6" => (PathwayMode?)PathwayMode.FareGate,
                "7" => (PathwayMode?)PathwayMode.ExitGate,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private PickupType? ParseFieldPickupType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return null;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Regularly scheduled pickup
            //1 - No pickup available
            //2 - Must phone agency to arrange pickup
            //3 - Must coordinate with driver to arrange pickup

            return value switch
            {
                "0" => (PickupType?)PickupType.Regular,
                "1" => (PickupType?)PickupType.NoPickup,
                "2" => (PickupType?)PickupType.PhoneForPickup,
                "3" => (PickupType?)PickupType.DriverForPickup,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private TimePointType ParseFieldTimepointType(string name, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // there is no value.
                return TimePointType.None;
            }

            // clean first.
            value = this.CleanFieldValue(value);

            //0 - Times are considered approximate.
            //1 - Times are considered exact.

            return value switch
            {
                "0" => TimePointType.Approximate,
                "1" => TimePointType.Exact,
                _ => throw new GTFSParseException(name, fieldName, value),
            };
        }

        private void Read<TEntity>(IGTFSSourceFile file, T feed, EntityParseDelegate<TEntity> parser,
                                                                                                  EntityAddDelegate<TEntity> addDelegate, IProgress<double> progress, int filesCompleted,
            int filesTotal)
            where TEntity : GTFSEntity
        {
            // set line preprocessor if any.
            file.LinePreprocessor = this.LinePreprocessor;

            // enumerate all lines.
            var enumerator = file.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                // there is no data, and if there is move to the columns.
                return;
            }

            // read the header.
            var headerColumns = new string[enumerator.Current.Length];
            for (int idx = 0; idx < headerColumns.Length; idx++)
            {
                // 'clean' header columns.
                headerColumns[idx] = this.CleanFieldValue(enumerator.Current[idx]);
            }

            var header = new GTFSSourceFileHeader(file.Name, headerColumns);

            // Progress slice for this file: [fileStart, fileEnd) within 0.0-1.0
            var fileStart = (double)filesCompleted / filesTotal;
            var fileEnd = (double)(filesCompleted + 1) / filesTotal;

            long recordCount = 0;
            long lastReportMs = -500; // report immediately on first tick

            var stopwatch = progress != default
                ? Stopwatch.StartNew()
                : default;

            void ReportIfDue()
            {
                if (progress == null) return;
                if (stopwatch!.ElapsedMilliseconds - lastReportMs < 500) return;

                // Approaches fileEnd asymptotically: fast early progress, slows near end
                var withinFile = 1.0 - (1.0 / (1.0 + recordCount / 50_000.0));
                var watchTime = fileStart + withinFile * (fileEnd - fileStart);
                progress.Report(watchTime);

                lastReportMs = stopwatch.ElapsedMilliseconds;
            }

            // read fields and keep them sorted.
            if (typeof(IComparable).IsAssignableFrom(typeof(TEntity)))
            {
                var entities = new List<TEntity>();

                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.All(string.IsNullOrWhiteSpace))
                        continue;

                    var entity = parser.Invoke(feed, header, enumerator.Current);
                    entities.Add(entity);

                    recordCount++;
                    ReportIfDue();
                }

                entities.Sort();

                foreach (var entity in entities)
                {
                    addDelegate.Invoke(entity);
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.All(x => string.IsNullOrWhiteSpace(x)))
                        continue;

                    var entity = parser.Invoke(feed, header, enumerator.Current);
                    addDelegate.Invoke(entity);

                    recordCount++;
                    ReportIfDue();
                }
            }

            // File done: report fileEnd
            progress?.Report(fileEnd);
        }

        private DateTime ReadDateTime(string name, string fieldName, string value)
        {
            try
            {
                return this.DateTimeReader.Invoke(value);
            }
            catch (Exception ex)
            {
                if (_strict)
                {
                    throw new GTFSParseException(name, fieldName, value, ex);
                }

                _logger.LogWarning("Failed to parse Date of day field '{FieldName}' in '{Name}' with value '{Value}': {Message}",
                    fieldName, name, value, ex.Message);

                return DateTime.Today;
            }
        }

        private TimeOfDay? ReadTimeOfDay(string name, string fieldName, string value)
        {
            try
            {
                return this.TimeOfDayReader.Invoke(value);
            }
            catch (Exception ex)
            {
                if (_strict)
                {
                    throw new GTFSParseException(name, fieldName, value, ex);
                }

                _logger.LogWarning("Failed to parse time of day field '{FieldName}' in '{Name}' with value '{Value}': {Message}",
                    fieldName, name, value, ex.Message);

                return null;
            }
        }

        #endregion Private Methods
    }
}