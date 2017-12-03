using Gridsum.DataflowEx;
using Swordfish.NET.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;
using ATAP.Utilities.Logging.Logging;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms
{
    /// <summary>
    /// Class CalculateAndStoreFromInputAndAsyncTermsOptions.
    /// Provides hints and configurations to DataFlowEXPatterns.CalculateAndStoreFromInputAndAsyncTerms
    /// Extends the Gridsum.DataflowEx.DataflowOptions
    /// <remarks>
    /// </remarks>
    /// </summary>
    public class CalculateAndStoreFromInputAndAsyncTermsOptions : Gridsum.DataflowEx.DataflowOptions
    {

        private static CalculateAndStoreFromInputAndAsyncTermsOptions s_defaultOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions(DataflowOptions.Default)
        {
            _asyncFetchTimeout = DefaultAsyncFetchTimeout,
            _asyncFetchTimeInterval = DefaultAsyncFetchTimeInterval
        };

        private static CalculateAndStoreFromInputAndAsyncTermsOptions s_verboseOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions(DataflowOptions.Verbose)
        {
            _asyncFetchTimeout = DefaultAsyncFetchTimeout,
            _asyncFetchTimeInterval = DefaultAsyncFetchTimeInterval
        };


        private TimeSpan _asyncFetchTimeout;
        private TimeSpan _asyncFetchTimeInterval;


        public CalculateAndStoreFromInputAndAsyncTermsOptions() : this(DefaultAsyncFetchTimeout, DefaultAsyncFetchTimeInterval, DataflowOptions.Default) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(DataflowOptions dataFlowOptions) : this(DefaultAsyncFetchTimeout, DefaultAsyncFetchTimeInterval, dataFlowOptions) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(TimeSpan asyncFetchTimeout, TimeSpan asyncFetchTimeInterval) : this(asyncFetchTimeout, asyncFetchTimeInterval, DataflowOptions.Default) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(TimeSpan asyncFetchTimeout, TimeSpan asyncFetchTimeInterval, DataflowOptions dataFlowOptions) : base()
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
        public static CalculateAndStoreFromInputAndAsyncTermsOptions Default
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
        public static CalculateAndStoreFromInputAndAsyncTermsOptions Verbose
        {
            get
            {
                return s_verboseOptions;
            }
        }
        #region Configure this class to use ATAP.Utilities.Logging
        internal static ILog Log { get; set; }

        static CalculateAndStoreFromInputAndAsyncTermsOptions()
        {
            Log = LogProvider.For<CalculateAndStoreFromInputAndAsyncTermsOptions>();
        }
        #endregion Configure this class to use ATAP.Utilities.Logging
    }
}

