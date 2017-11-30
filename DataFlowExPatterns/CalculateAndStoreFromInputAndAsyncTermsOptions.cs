using Gridsum.DataflowEx;
using Swordfish.NET.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        private static CalculateAndStoreFromInputAndAsyncTermsOptions s_defaultOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions()
        {
            AsyncFetchTimeout = DefaultAsyncFetchTimeout
        };

        private static CalculateAndStoreFromInputAndAsyncTermsOptions s_verboseOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions()
        {
            AsyncFetchTimeout = DefaultAsyncFetchTimeout,
        };


        private TimeSpan _asyncFetchTimeout;


        public CalculateAndStoreFromInputAndAsyncTermsOptions() : this( DefaultAsyncFetchTimeout, DataflowOptions.Default) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(DataflowOptions dataFlowOptions) : this(DefaultAsyncFetchTimeout, dataFlowOptions) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(TimeSpan asyncFetchTimeout) : this(asyncFetchTimeout, DataflowOptions.Default) { }
        public CalculateAndStoreFromInputAndAsyncTermsOptions(TimeSpan asyncFetchTimeout, DataflowOptions dataFlowOptions) : base()
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
    }
}

