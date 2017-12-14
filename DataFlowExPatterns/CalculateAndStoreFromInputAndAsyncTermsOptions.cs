using Gridsum.DataflowEx;
using Swordfish.NET.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;
using ATAP.Utilities.Logging.Logging;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    /// <summary>
    /// Class SolveAndStoreOptions.
    /// Provides hints and configurations to DataFlowEXPatterns.SolveAndStoreFromInputAndAsyncTerms
    /// Extends the Gridsum.DataflowEx.DataflowOptions
    /// <remarks>
    /// </remarks>
    /// </summary>
    public class SolveAndStoreOptions : Gridsum.DataflowEx.DataflowOptions
    {

        private static SolveAndStoreOptions s_defaultOptions = new SolveAndStoreOptions(DataflowOptions.Default)
        {
            _asyncFetchTimeout = DefaultAsyncFetchTimeout,
            _asyncFetchTimeInterval = DefaultAsyncFetchTimeInterval
        };

        private static SolveAndStoreOptions s_verboseOptions = new SolveAndStoreOptions(DataflowOptions.Verbose)
        {
            _asyncFetchTimeout = DefaultAsyncFetchTimeout,
            _asyncFetchTimeInterval = DefaultAsyncFetchTimeInterval
        };


        private TimeSpan _asyncFetchTimeout;
        private TimeSpan _asyncFetchTimeInterval;


        public SolveAndStoreOptions() : this(DefaultAsyncFetchTimeout, DefaultAsyncFetchTimeInterval, DataflowOptions.Default) { }
        public SolveAndStoreOptions(DataflowOptions dataFlowOptions) : this(DefaultAsyncFetchTimeout, DefaultAsyncFetchTimeInterval, dataFlowOptions) { }
        public SolveAndStoreOptions(TimeSpan asyncFetchTimeout, TimeSpan asyncFetchTimeInterval) : this(asyncFetchTimeout, asyncFetchTimeInterval, DataflowOptions.Default) { }
        public SolveAndStoreOptions(TimeSpan asyncFetchTimeout, TimeSpan asyncFetchTimeInterval, DataflowOptions dataFlowOptions) : base()
        {

            _asyncFetchTimeout = asyncFetchTimeout;
            base.BlockMonitorEnabled = dataFlowOptions.BlockMonitorEnabled;
            base.FlowMonitorEnabled = dataFlowOptions.FlowMonitorEnabled;
            base.MonitorInterval = dataFlowOptions.MonitorInterval;
            base.PerformanceMonitorMode = dataFlowOptions.PerformanceMonitorMode;
            base.RecommendedCapacity = dataFlowOptions.RecommendedCapacity;
            base.RecommendedParallelismIfMultiThreaded = dataFlowOptions.RecommendedParallelismIfMultiThreaded;
        }

        /// <summary>
        /// The interval of the async monitor loop
        /// </summary>
        public TimeSpan AsyncFetchTimeout
        {
            get
            {
                return _asyncFetchTimeout == TimeSpan.Zero ? DefaultAsyncFetchTimeout : _asyncFetchTimeout;
            }
            set
            {
                this._asyncFetchTimeout = value;
            }
        }

        /// <summary>
        /// The interval of the async monitor loop
        /// </summary>
        public TimeSpan AsyncFetchTimeInterval
        {
            get
            {
                return _asyncFetchTimeInterval == TimeSpan.Zero ? DefaultAsyncFetchTimeInterval : _asyncFetchTimeInterval;
            }
            set
            {
                this._asyncFetchTimeInterval  = value;
            }
        }

        /// <summary>
        /// A predefined default setting for DataflowOptions
        /// </summary>
        public new static SolveAndStoreOptions Default
        {
            get
            {
                return s_defaultOptions;
            }
        }

        /// <summary>
        /// The default amount of time to wait for an async Fetch to complete, 30 seconds
        /// </summary>
        public static TimeSpan DefaultAsyncFetchTimeout
        {
            get
            {
                return TimeSpan.FromSeconds(30);
            }
        }

        public static TimeSpan DefaultAsyncFetchTimeInterval
        {
            get
            {
                return TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// A predefined verbose setting for DataflowOptions
        /// </summary>
        public new static SolveAndStoreOptions Verbose
        {
            get
            {
                return s_verboseOptions;
            }
        }
        #region Configure this class to use ATAP.Utilities.Logging
        internal static ILog Log { get; set; }

        static SolveAndStoreOptions()
        {
            Log = LogProvider.For<SolveAndStoreOptions>();
        }
        #endregion Configure this class to use ATAP.Utilities.Logging
    }
}

