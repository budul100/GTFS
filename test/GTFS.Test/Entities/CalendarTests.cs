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
using NUnit.Framework;

namespace GTFS.Test.Entities
{
    /// <summary>
    /// Contains tests for the calendar entity and related functionality.
    /// </summary>
    [TestFixture]
    public class CalendarTests
    {
        #region Public Methods

        /// <summary>
        /// Tests the mask day properties relation.
        /// </summary>
        [Test]
        public void TestMask()
        {
            var calendar = new Calendar
            {
                Mask = 0
            };

            Assert.That(calendar.Monday, Is.EqualTo(false));
            Assert.That(calendar.Tuesday, Is.EqualTo(false));
            Assert.That(calendar.Wednesday, Is.EqualTo(false));
            Assert.That(calendar.Thursday, Is.EqualTo(false));
            Assert.That(calendar.Friday, Is.EqualTo(false));
            Assert.That(calendar.Saturday, Is.EqualTo(false));
            Assert.That(calendar.Sunday, Is.EqualTo(false));

            calendar.Mask = 1;

            Assert.That(calendar.Monday, Is.EqualTo(true));
            Assert.That(calendar.Tuesday, Is.EqualTo(false));
            Assert.That(calendar.Wednesday, Is.EqualTo(false));
            Assert.That(calendar.Thursday, Is.EqualTo(false));
            Assert.That(calendar.Friday, Is.EqualTo(false));
            Assert.That(calendar.Saturday, Is.EqualTo(false));
            Assert.That(calendar.Sunday, Is.EqualTo(false));

            calendar.Mask = 64;

            Assert.That(calendar.Monday, Is.EqualTo(false));
            Assert.That(calendar.Tuesday, Is.EqualTo(false));
            Assert.That(calendar.Wednesday, Is.EqualTo(false));
            Assert.That(calendar.Thursday, Is.EqualTo(false));
            Assert.That(calendar.Friday, Is.EqualTo(false));
            Assert.That(calendar.Saturday, Is.EqualTo(false));
            Assert.That(calendar.Sunday, Is.EqualTo(true));

            calendar.Mask = 1 + 4 + 8 + 16;

            Assert.That(calendar.Monday, Is.EqualTo(true));
            Assert.That(calendar.Tuesday, Is.EqualTo(false));
            Assert.That(calendar.Wednesday, Is.EqualTo(true));
            Assert.That(calendar.Thursday, Is.EqualTo(true));
            Assert.That(calendar.Friday, Is.EqualTo(true));
            Assert.That(calendar.Saturday, Is.EqualTo(false));
            Assert.That(calendar.Sunday, Is.EqualTo(false));

            calendar.Mask = 0;
            calendar.Monday = true;
            Assert.That(calendar.Mask, Is.EqualTo(1));

            calendar.Mask = 0;
            calendar.Sunday = true;
            Assert.That(calendar.Mask, Is.EqualTo(64));

            calendar.Mask = 0;
            calendar.Monday = true;
            calendar.Wednesday = true;
            calendar.Thursday = true;
            calendar.Friday = true;
            Assert.That(calendar.Mask, Is.EqualTo(1 + 4 + 8 + 16));
        }

        /// <summary>
        /// Tests merging two calendar objects and have them still represent the same information.
        /// </summary>
        [Test]
        public void TestTryMerge()
        {
            // two simple consequitive weeks.
            var calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            var calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 30),
                EndDate = new System.DateTime(2015, 12, 06)
            };

            Assert.That(calendar1.TryMerge(calendar2, out var merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            // two calendars one completely overlapping the other.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 12, 06)
            };

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            // two calendars one completely overlapping the other.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 12, 06)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 29)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            // two calendars one completely overlapping the other.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 1 + 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 12, 06)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(1 + 64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 23)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(1 + 64));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 23)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 06)));

            // two calendars with conflicting masks.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 2 + 64,
                StartDate = new System.DateTime(2015, 11, 23),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 1 + 64,
                StartDate = new System.DateTime(2015, 11, 30),
                EndDate = new System.DateTime(2015, 12, 06)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out _), Is.False);
            Assert.That(calendar2.TryMerge(calendar1, out _), Is.False);

            // two calendars both spanning more than one week.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 127,
                StartDate = new System.DateTime(2015, 11, 16),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 127,
                StartDate = new System.DateTime(2015, 11, 30),
                EndDate = new System.DateTime(2015, 12, 13)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(127));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 16)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 13)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(127));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 16)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 13)));

            // two calendars first one week with a few don't-case other more than a week.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 64 + 32 + 16 + 8,
                StartDate = new System.DateTime(2015, 11, 26),
                EndDate = new System.DateTime(2015, 11, 29)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 127,
                StartDate = new System.DateTime(2015, 11, 30),
                EndDate = new System.DateTime(2015, 12, 13)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(127));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 26)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 13)));

            Assert.That(calendar2.TryMerge(calendar1, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(127));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2015, 11, 26)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2015, 12, 13)));

            // two calendars first two weeks with a few don't-cares other the week right after.
            calendar1 = new Calendar()
            {
                ServiceId = "0",
                Mask = 127,
                StartDate = new System.DateTime(2016, 01, 01),
                EndDate = new System.DateTime(2016, 01, 10)
            };
            calendar1.TrimDates();
            calendar2 = new Calendar()
            {
                ServiceId = "0",
                Mask = 127,
                StartDate = new System.DateTime(2016, 01, 11),
                EndDate = new System.DateTime(2016, 01, 17)
            };
            calendar2.TrimDates();

            Assert.That(calendar1.TryMerge(calendar2, out merge), Is.True);
            Assert.That(merge.ServiceId, Is.EqualTo("0"));
            Assert.That(merge.Mask, Is.EqualTo(127));
            Assert.That(merge.StartDate, Is.EqualTo(new System.DateTime(2016, 01, 01)));
            Assert.That(merge.EndDate, Is.EqualTo(new System.DateTime(2016, 01, 17)));
        }

        #endregion Public Methods
    }
}