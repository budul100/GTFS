// The MIT License (MIT)

// Copyright (c) 2017 Ben Abelshausen

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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GTFS.Logging
{
    /// <summary>
    /// Provides logging for the GTFS library.
    /// </summary>
    public static class Logger
    {
        #region Private Fields

        private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

        #endregion Private Fields

        #region Public Delegates

        /// <summary>
        /// Defines the legacy log action function.
        /// </summary>
        [Obsolete("Use Logger.UseLoggerFactory instead. This member will be removed in a future version.")]
        public delegate void LogActionFunction(string origin, string level, string message,
            Dictionary<string, object> parameters);

        #endregion Public Delegates

        #region Public Properties

        /// <summary>
        /// Use UseLoggerFactory instead.
        /// </summary>
        [Obsolete("Use Logger.UseLoggerFactory instead. This member will be removed in a future version.")]
        public static LogActionFunction LogAction { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Configures the logger factory used by the GTFS library.
        /// </summary>
        public static void UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory
                ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        #endregion Public Methods

        #region Internal Methods

        internal static ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        #endregion Internal Methods
    }
}