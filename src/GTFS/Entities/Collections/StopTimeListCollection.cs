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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GTFS.Entities.Collections
{
    /// <summary>
    /// A collection of StopTimes.
    /// </summary>
    public class StopTimeListCollection
        : IStopTimeCollection
    {
        #region Private Fields

        private readonly List<StopTime> _entities;
        private readonly Dictionary<string, List<int>> _tripIndex;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Creates a unique entity collection based on a list.
        /// </summary>
        /// <param name="entities"></param>
        public StopTimeListCollection(List<StopTime> entities)
        {
            _entities = entities;
            _tripIndex = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            RebuildIndex();
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets the number of entities.
        /// </summary>
        public int Count
        {
            get
            {
                return _entities.Count;
            }
        }

        #endregion Public Properties

        #region Public Indexers

        /// <summary>
        /// Gets or sets the entity at the given idx.
        /// </summary>
        public StopTime this[int idx]
        {
            get
            {
                return _entities[idx];
            }

            set
            {
                _entities[idx] = value;
            }
        }

        #endregion Public Indexers

        #region Public Methods

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="stopTime"></param>
        public void Add(StopTime stopTime)
        {
            var index = _entities.Count;
            _entities.Add(stopTime);
            AddToIndex(stopTime.TripId, index);
        }

        /// <summary>
        /// Adds a range of stop times and updates the trip index accordingly.
        /// </summary>
        public void AddRange(IEnumerable<StopTime> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        /// <summary>
        /// Gets all stop times.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StopTime> Get()
        {
            return _entities;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the entities.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<StopTime> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the entities.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        /// <summary>
        /// Gets all stop times for the given stop.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StopTime> GetForStop(string stopId)
        {
            return _entities.Where(x =>
            {
                return x.StopId == stopId;
            });
        }

        /// <summary>
        /// Gets all stop times for the given trip.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StopTime> GetForTrip(string tripId)
        {
            if (!_tripIndex.TryGetValue(tripId, out var indices))
                return [];

            return indices.Select(i => _entities[i]);
        }

        /// <summary>
        /// Gets all stop times for the given trips.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<StopTime> GetForTrips(IEnumerable<string> tripIds)
        {
            return _entities.Where(e =>
            {
                return tripIds.Contains(e.TripId);
            });
        }

        /// <summary>
        /// Removes all stop times for the given trip.
        /// </summary>
        public void Remove(string tripId)
        {
            if (!_tripIndex.ContainsKey(tripId))
                return;

            _entities.RemoveAll(x => x.TripId == tripId);
            RebuildIndex();
        }

        /// <summary>
        /// Replaces the internal list of stop_times with a new, empty list.
        /// </summary>
        /// <returns></returns>
        public void RemoveAll()
        {
            _entities.Clear();
            _tripIndex.Clear();
        }

        /// <summary>
        /// Removes all stop times for the given stop.
        /// </summary>
        /// <returns></returns>
        public int RemoveForStop(string stopId)
        {
            var count = _entities.RemoveAll(x => x.StopId == stopId);
            if (count > 0) RebuildIndex();

            return count;
        }

        /// <summary>
        /// Removes all stop times for the given trip.
        /// </summary>
        /// <returns></returns>
        public int RemoveForTrip(string tripId)
        {
            var count = _entities.RemoveAll(x => x.TripId == tripId);
            if (count > 0) RebuildIndex();

            return count;
        }

        /// <summary>
        /// Removes all stop times for the given trips.
        /// </summary>
        public void RemoveForTrips(IEnumerable<string> tripIds)
        {
            var idSet = new HashSet<string>(tripIds, StringComparer.Ordinal);
            var count = _entities.RemoveAll(x => idSet.Contains(x.TripId));

            if (count > 0) RebuildIndex();
        }

        /// <summary>
        /// Removes all stop times for the given trip matching the given stop sequences.
        /// </summary>
        public void RemoveRange(string tripId, IEnumerable<uint> stopSequences)
        {
            var sequences = new HashSet<uint>(stopSequences);
            _entities.RemoveAll(x => x.TripId == tripId && sequences.Contains(x.StopSequence));
            RebuildIndex();
        }

        /// <summary>
        /// Removes the given stop times from the collection.
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        public void RemoveRange(IEnumerable<StopTime> entities)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the stop time identified by the given stop and trip id.
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        public bool Update(string stopId, string tripId, StopTime newEntity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the stop time identified by the given stop id, trip id and stop sequence.
        /// </summary>
        /// <exception cref="NotImplementedException"/>
        public bool Update(string stopId, string tripId, uint stopSequence, StopTime newEntity)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods

        #region Private Methods

        private void AddToIndex(string tripId, int index)
        {
            if (!_tripIndex.TryGetValue(tripId, out var indices))
            {
                indices = [];
                _tripIndex[tripId] = indices;
            }
            indices.Add(index);
        }

        private void RebuildIndex()
        {
            _tripIndex.Clear();
            for (int i = 0; i < _entities.Count; i++)
            {
                AddToIndex(_entities[i].TripId, i);
            }
        }

        #endregion Private Methods
    }
}