using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using ATAP.Utilities.Logging.Logging;
using Gridsum.DataflowEx;
using Newtonsoft.Json;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    /// <summary>
    /// This DataFlow will calculate a Result (decimal), for each input message, and store that into a ConcurrentObservableDictionary (COD) 
    ///   that was declared in the accompanying ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms project and passed into this dataflow's constructor
    ///   The overall structure of teh graph starts with a Head block (_acceptor), and ends with a Action block that performs a calculation and stores the results (_terminator)
    ///   The calculation being performed depends on Terms that are retrieved via an async fetch. 
    ///   The terms being retrieved are defined by the keys to the terms1 IReadOnlyDictionary that are part of the message
    ///   If all of async data needed, specified by all of the keys of the terms1, has been received, the message is sent from the _acceptor to the _terminator
    ///   Any message whose terms1 keys' async data, specified by all of the keys of the terms1, has not yet been retrieved, the message is shunted to a DataDispatcher.
    ///   If the set of all of the keys of the terms1 of the message has never before been seen by the DataDispatcher, then the DataDispatcher dynamically creates a TransientBuffer for that set of terms1 keys
    ///   When all of the async tasks that retrieve the data for each key found in terms1 completes, all of the messages buffered in a TransientBuffer are released to the _terminator
    ///   When all of the messages buffered in a TransientBuffer are released to the _terminator, the TransientBuffer is disposed of.
    ///   ToDo: a maxtimeToWait for the async fetch task to complete, after the _acceptor receives a Complete signal, before declaring a transient block faulted. 
    /// </summary>
    public partial class CalculateAndStoreFromInputAndAsyncTerms : Dataflow<IInputMessage<string, double>> {
        // External data, defined in class, populated in ctor.
        CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
        CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;
        // Head of this dataflow graph
        ITargetBlock<IInputMessage<string, double>> _headBlock;
        Dataflow<IInternalMessage<string>> _terminator;
        // A thread-safe place to keep the TransientBuffers associated with each ElementSet of term1
        ConcurrentDictionary<string, Dataflow<IInternalMessage<string>, IInternalMessage<string>>> _transientBuffersForElementSetsOfTerm1;
        // External http client library.
        IWebGet _webGet;
        Timer asyncFetchCheckTimer;

        // Constructor
        public CalculateAndStoreFromInputAndAsyncTerms(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            Log.Trace("Constructor starting");

            _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
            _webGet = webGet;
            _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;
            // Create a place to store the 
            _transientBuffersForElementSetsOfTerm1 = new ConcurrentDictionary<string, Dataflow<IInternalMessage<string>, IInternalMessage<string>>>();

            // The terminal block performs both the Compute  and the Store operations
            Log.Trace("Creating _terminator");
            _terminator = new ActionBlock<IInternalMessage<string>>(_input => { Log.Trace("_terminator received InternalMessage");
                // do the calculation for all KeyValuePairs in terms1
                var r1 = 0.0;
                _input.Value.terms1.ToList()
                    .ForEach(kvp => { r1 += kvp.Value /
                                              calculateAndStoreFromInputAndAsyncTermsObservableData.FetchedIndividualElementsOfTerm1[kvp.Key]; });
                // Store the pr value
                calculateAndStoreFromInputAndAsyncTermsObservableData.RecordR(_input.Value.k1,
                                                                              _input.Value.k2,
                                                                              Convert.ToDecimal(r1)); }).ToDataflow(calculateAndStoreFromInputAndAsyncTermsOptions);


            // this block accepts messages where isReadyToCalculate is false, and buffers them
            Log.Trace("Creating _dynamicBuffers");
            DynamicBuffers _dynamicBuffers = new DynamicBuffers(calculateAndStoreFromInputAndAsyncTermsObservableData,
                                                                webGet,
                                                                calculateAndStoreFromInputAndAsyncTermsOptions,
                                                                _transientBuffersForElementSetsOfTerm1);

            // foreach IInputMessage<TKeyTerm1,TValueTerm1>, create an internal message that adds the terms1 signature and the bool used by the routing predicate
            // the output is k1, k2, c1, bool, and the output is routed on the bool value
            Log.Trace("Creating _accepter");
            var _accepter = new TransformBlock<IInputMessage<string, double>, IInternalMessage<string>>(_input => {
                // ToDo also check on the async tasks check when an upstream completion occurs
                // ToDo need a default value for how long to wait for an async fetch to complete after an upstream completion occurs
                // ToDo need a constructor and a property that will let a caller change the default value for how long to wait for an async fetch to complete after an upstream completion occurs
                // ToDo add exception handling to ensure the tasks, as well as the async method's resources, are released if any blocks in the dataflow fault
                // CheckAsyncTasks();
 Log.Trace("Accepter received IInputMessage");

                #region create AsyncFetchCheckTimer and connect callback
                // Create a timer that is used to check on the async fetch tasks, the  async fetch tasks check-for-completion loop timer
                // the timer has its interval from the options passed into this constructor, it will restart and the event handler will stop the timer and start the timer each time
                // ToDo add the timer that checks on the health of the async fetch tasks check-for-completion loop every DefaultAsyncFetchTimeout interval, expecting it to provide a heartbeat, 
                // The Cleanup method will call this timers Dispose method
                // the event handler's job is to call CheckAsyncTasks which will check for completed fetches and link the child Transient buffers to the _terminator
                Log.Trace("creating and starting the AsyncFetchCheckTimer");
                AsyncFetchCheckTimer = new Timer(_calculateAndStoreFromInputAndAsyncTermsOptions.AsyncFetchTimeInterval.TotalMilliseconds);
                AsyncFetchCheckTimer.AutoReset = true;
                // set the event handler (callback) for this timer to the function for async fetch tasks check-for-completion loop
                AsyncFetchCheckTimer.Elapsed += new ElapsedEventHandler(AsyncFetchCheckTimer_Elapsed);
                AsyncFetchCheckTimer.Start();
                #endregion create AsyncFetchCheckTimer and connect callback
                // Work on the _input

                // create a signature from the set of keys found in terms1
                // Work on the _input
                // create a signature from the set of keys found in terms1
                KeySignature<string> sig = new KeySignature<string>(_input.Value.terms1.Keys);

                // Is the sig.largest in the ElementSetsOfTerm1Ready dictionary? set the output bool accordingly
                bool isReadyToCalculate = ElementSetsOfTerm1Ready.ContainsKey(sig.Longest());

                // Pass the message along to the next block, which will be either the _terminator, or the _dynamicBuffers
                return new InternalMessage<string>((_input.Value.k1, _input.Value.k2, _input.Value.terms1, sig, isReadyToCalculate)); }).ToDataflow();

            _accepter.Name = "_accepter";
            _terminator.Name = "_terminator";
            _dynamicBuffers.Name = "_dynamicBuffers";

            // Link the data flow
            Log.Trace("Linking dataflow between blocks");

            // Link _accepter to _terminator when the InternalMessage.Value has isReadyToCalculate = true
            _accepter.LinkTo(_terminator, m1 => m1.Value.isReadyToCalculate);
            // Link _accepter to _dynamicBuffers when the  InternalMessage.Value has isReadyToCalculate = false
            _accepter.LinkTo(_dynamicBuffers,
                             m1 => !m1.Value.isReadyToCalculate);
            // data flow linkage of the dynamically created TransientBuffer children to the _terminator is complex and handled elsewhere

            // Link the completion tasks
            Log.Trace("Linking completion between blocks");
            _dynamicBuffers.RegisterDependency(_accepter);
            _terminator.RegisterDependency(_accepter);
            // Completion linkage of the dynamically created TransientBuffer children to the _terminator is complex and handled elsewhere

            Log.Trace("Registering Children");
            this.RegisterChild(_accepter);
            this.RegisterChild(_terminator);
            this.RegisterChild(_dynamicBuffers);

            // set the InputBlock for this dataflow graph to be the InputBlock of the _acceptor
            this._headBlock = _accepter.InputBlock;
            // ToDo: start PreparingToCalculate to pre-populate the initial list of C key signatures
            Log.Trace("Constructor Finished");
        }

        void AsyncFetchCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Log.Trace("Starting the AsyncFetchCheckTimer_Elapsed");
            AsyncFetchCheckTimer.Stop();
            CheckAsyncTasks();
            AsyncFetchCheckTimer.Start();
            Log.Trace("Leaving the AsyncFetchCheckTimer_Elapsed");
        }

        #region critical section that periodicly checks on the status of the outstanding tasks that are fetching terms
        // This is the method called, under a number of different conditions, to determine if the Async tasks that fetch a particular Term1 has completed
        /// <summary>
        /// Checks the asynchronous tasks.
        /// </summary>
        /// 
        void CheckAsyncTasks() {
            Log.Trace("Starting the CheckAsyncTasks method");
            bool unfinished;
            // iterate each individual term of the sig, and get those that are not already present in the COD FetchingIndividualElementsOfTerm1
            FetchingElementSetsOfTerm1.Keys.ToList()
                .ForEach(sigLongest => { unfinished = false;
                    Log.Trace("Iterating FetchingElementSetsOfTerm1.Keys, now on {0}",
                              sigLongest);
                    FetchingElementSetsOfTerm1[sigLongest].Keys.ToList()
                        .ForEach(element => { Log.Trace("Iterating FetchingElementSetsOfTerm1[{0}].Keys, now on {1}",
                                                        sigLongest,
                                                        element);
                            unfinished &= FetchingIndividualElementsOfTerm1[element].IsCompleted;
                            if(!unfinished) {
                                        Log.Trace("sigLongest {0} is now finished",
                                                  sigLongest);
                                // if sigLongest is finished, but not yet a key in ElementSetsOfTerm1Ready then this is the first loop where it is finally ready
                                if(!ElementSetsOfTerm1Ready.ContainsKey(sigLongest)) {
                                            // attach the transientBlock to the _terminator  
                                            Log.Trace("attaching buffer {0} to terminator block, based on sigLongest {1}",
                                                      1,
                                                      sigLongest);
                                    // attach completion first
                                    _terminator.RegisterDependency(TransientBuffersForElementSetsOfTerm1[sigLongest]);
                                    // attach data linkage second
                                    TransientBuffersForElementSetsOfTerm1[sigLongest].LinkTo(_terminator);
                                    // put sigLongest into ElementSetsOfTerm1Ready
                                    Log.Trace("sigLongest {0} is now in the ElementSetsOfTerm1Ready",
                                              sigLongest);
                                    ElementSetsOfTerm1Ready[sigLongest] = default(byte);
                                    //remove this sigLongest from the FetchingElementSetsOfTerm1 dictionary
                                    FetchingElementSetsOfTerm1.Remove(sigLongest);
                                    Log.Trace("sigLongest {0} has been removed from the ElementSetsOfTerm1Ready",
                                              sigLongest);
                                }
                            }
                            else {
                                        Log.Trace("sigLongest {0} is still unfinished",
                                                  sigLongest);
                            } }); });


            Log.Trace("Leaving the CheckAsyncTasks method");
        }
        #endregion
        #region timer Accessors
        Timer AsyncFetchCheckTimer { get => asyncFetchCheckTimer; set => asyncFetchCheckTimer =
            value; }
        #endregion timer Accessors

        protected override void CleanUp(Exception e) {
            Log.Trace("Starting Cleanup and calling base.Cleanup");
            base.CleanUp(e);
            Log.Trace("Cleanup after base");
            // dispose of the asyncFetchCheckTimer
            Log.Trace("Disposing the AsyncFetchCheckTimer");
            AsyncFetchCheckTimer.Dispose();
            Log.Trace("Cleanup complete");
        }

        internal ConcurrentDictionary<string, Dataflow<IInternalMessage<string>, IInternalMessage<string>>> TransientBuffersForElementSetsOfTerm1 { get => _transientBuffersForElementSetsOfTerm1; set => _transientBuffersForElementSetsOfTerm1 =
            value; }

        #region Dataflow Input and Output blocks Accessors
        public override ITargetBlock<IInputMessage<string, double>> InputBlock { get { return this._headBlock; } }
        #endregion Dataflow Input and Output blocks Accessors
        // ToDo:  make abstract and replace hard coded string with the type passed when the dataflow pattern is declared
        class DynamicBuffers : DataDispatcher<IInternalMessage<string>, KeySignature<string>> {
            CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
            CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;
            ConcurrentDictionary<string, Dataflow<IInternalMessage<string>, IInternalMessage<string>>> _transientBuffersForElementSetsOfTerm1;
            IWebGet _webGet;

            public DynamicBuffers(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions, ConcurrentDictionary<string, Dataflow<IInternalMessage<string>, IInternalMessage<string>>> transientBuffersForElementSetsOfTerm1) : base(@out => @out.Value.sig) {
                Log.Trace("Constructor Starting");
                _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
                _webGet = webGet;
                _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;
                _transientBuffersForElementSetsOfTerm1 = transientBuffersForElementSetsOfTerm1;
                Log.Trace("Constructor Finished");
            }

            #region WebGet accessors
            IWebGet WebGet { get => _webGet; set => _webGet = value; }
            #endregion WebGet accessors

            protected override void CleanUp(Exception e) {
                Log.Trace("Starting Cleanup and calling base.Cleanup");
                base.CleanUp(e);
                Log.Trace("Cleanup after base");
                Log.Trace("Cleanup complete");
            }

            /// <summary>
            /// This function will create one instance of a TransientBuffer,and will only be called once for each distinct sig (the first time)
            /// </summary>
            /// <param name="sig">The sig.</param>
            /// <returns>Dataflow&lt;InternalMessage&lt;System.String&gt;&gt;.</returns>
            protected override Dataflow<IInternalMessage<string>> CreateChildFlow(KeySignature<string> sig) {
                // dynamically create a TransientBuffer buffer. The dispatchKey is based upon the value of sig
                // pass sig to the TransientBuffer constructor, and wget, and the ObservableData class
                // so the TransientBuffer can create the async tasks to fetch each individual term of the sig
                Log.Trace("CreateChildFlow is creating _buffer");

                /*var _buffer = new TransientBuffer(sig,
                                                  _calculateAndStoreFromInputAndAsyncTermsObservableData,
                                                  WebGet,
                                                  _calculateAndStoreFromInputAndAsyncTermsOptions);
                */
                var _buffer = new DynamicBuffers.TransientBuffer(sig, this);
                // Store the sig._individualElements collection and this buffer into sigIsWaitingForCompletion COD
                Log.Trace("CreateChildFlow is storing {0} in IsFetchingSigOfTerm1",
                          sig.Longest());
                try
                {
                    _transientBuffersForElementSetsOfTerm1[sig.Longest()] = _buffer;
                }
                catch
                {
                    log.Error("error when trying to store the sig.Longest value");
                    throw new Exception("error when trying to store the sig.Longest value");
                }
                // no need to call RegisterChild(_buffer) here as DataDispatcher will call automatically
                Log.Trace("CreateChildFlow has created _buffer and is returning");
                // return the TransientBuffer
                return _buffer;
            }

            /// <summary>
            /// Transient Buffer node for a single sig
            /// </summary>
            class TransientBuffer : Dataflow<IInternalMessage<string>, IInternalMessage<string>> {
                // The TPL block that buffers the data.
                BufferBlock<IInternalMessage<string>> _buffer;
                DynamicBuffers _parent;

                public TransientBuffer(KeySignature<string> sig, DynamicBuffers parent) : base(parent._calculateAndStoreFromInputAndAsyncTermsOptions) {
                    Log.Trace("Constructor Starting");
                    this._parent = parent;
                    Log.Trace($"ElementSet: {sig.Longest().ToString()}");

                    Log.Trace("Creating _buffer");
                    _buffer = new BufferBlock<IInternalMessage<string>>();

                    Log.Trace("Registering _buffer");
                    RegisterChild(_buffer);
                    // critical section
                    // iterate each individual element of the sig, and get those that are not already present in the COD FetchingIndividualElementsOfTerm1
                    foreach(var element in sig.IndividualElements) {
                        if(!parent._calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingIndividualElementsOfTerm1.ContainsKey(element)) {
                            // For each element that is not already being fetched, start the async task to fetch it
                            Log.Trace($"Fetching AsyncWebGet for {element} and storing the task in FetchingIndividualElementsOfTerm1 indexed by {element}");
                            // call the async function that fetches the information for each individual element in the elementSet
                            // record the individual element and it's corresponding task in the FetchingIndividualElementsOfTerm1
                            parent._calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingIndividualElementsOfTerm1[element] = parent._webGet.AsyncWebGet<double>(element);
                        }
                    }
                    // Record into FetchingElementSetsOfTerm1 the sig.Longest as key, and for the data,
                    // create a COD whose keys are the keys of sig.IndividualElements in the FetchingElementSetsOfTerm1
                    Log.Trace("Creating new concurrent dictionary");
                    var x = new ConcurrentObservableDictionary<string, byte>();
                    foreach(var element in sig.IndividualElements) {
                        Log.Trace($"Creating an entry for {element}");
                        x[element] = default;
                    }
                    Log.Trace($"Creating an entry for FetchingElementSetsOfTerm1. Key is {sig.Longest()} data is x");
                    parent._calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingElementSetsOfTerm1[sig.Longest()] = x;
                    Log.Trace("Constructor Finished");
                }

                protected override void CleanUp(Exception e) {
                    Log.Trace("Starting Cleanup and calling base.Cleanup");
                    base.CleanUp(e);
                    Log.Trace("Cleanup after base");
                    // ToDo Cleanup any messages on the transient blocks
                    // remove this TransientBlock's event Handlers from all Tasks representing term fetches, for the individual terms for this TBs sig
                    Log.Trace("Cleanup complete");
                }

                #region Dataflow Input and Output blocks Accessors
                public override ITargetBlock<IInternalMessage<string>> InputBlock { get { return this._buffer; } }

                public override ISourceBlock<IInternalMessage<string>> OutputBlock { get { return this._buffer; } }
                #endregion Dataflow Input and Output blocks Accessors
                #region Configure this class to use ATAP.Utilities.Logging
                // Internal class logger for this class
                static ILog log;

                static TransientBuffer() {
                    log = LogProvider.For<TransientBuffer>();
                }

                internal static ILog Log { get => log; set => log = value; }
                #endregion Configure this class to use ATAP.Utilities.Logging
            }

            #region Configure this class to use ATAP.Utilities.Logging
            // Internal class logger for this class
            static ILog log;

            static DynamicBuffers() {
                log = LogProvider.For<DynamicBuffers>();
            }

            internal static ILog Log { get => log; set => log = value; }
            #endregion Configure this class to use ATAP.Utilities.Logging
        }

        #region ObservableDataAccessors
        ConcurrentObservableDictionary<string, Task> FetchingIndividualElementsOfTerm1 { get => _calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingIndividualElementsOfTerm1; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingIndividualElementsOfTerm1 =
            value; }

        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> FetchingElementSetsOfTerm1 { get => _calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingElementSetsOfTerm1; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.FetchingElementSetsOfTerm1 =
            value; }

        ConcurrentObservableDictionary<string, byte> ElementSetsOfTerm1Ready { get => this._calculateAndStoreFromInputAndAsyncTermsObservableData.ElementSetsOfTerm1Ready; set => this._calculateAndStoreFromInputAndAsyncTermsObservableData.ElementSetsOfTerm1Ready =
            value; }
        #endregion ObservableDataAccessors
        #region Configure this class to use ATAP.Utilities.Logging
        // Internal class logger for this class
        static ILog log;

        static CalculateAndStoreFromInputAndAsyncTerms() {
            log = LogProvider.For<CalculateAndStoreFromInputAndAsyncTerms>();
        }

        internal static ILog Log { get => log; set => log = value; }
        #endregion Configure this class to use ATAP.Utilities.Logging
    }

    public class ParseSingleInputStringFormattedAsJSONToInputMessage : Dataflow<string, IInputMessage<string, double>> {
        // Head and tail 
        TransformBlock<string, IInputMessage<string, double>> _transformer;

        public ParseSingleInputStringFormattedAsJSONToInputMessage() : this(CalculateAndStoreFromInputAndAsyncTermsOptions.Default) {
        }

        public ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            Log.Trace("Constructor starting");
            // create the output via a TransformBlock
            _transformer = new TransformBlock<string, IInputMessage<string, double>>(_input => { (string k1, string k2, IReadOnlyDictionary<string, double> terms1) _temp;
                try
                {
                    Log.Trace("Deserialize Starting");
                    _temp = JsonConvert.DeserializeObject<(string k1, string k2, Dictionary<string, double> terms1)>(_input);
                    Log.Trace("Deserialize Finished");
                }
                catch
                {
                    ArgumentException e = new ArgumentException($"{_input} does not match the needed input pattern");
                    Log.WarnException("Exception", e, "unused");
                    throw e;
                }
                return new InputMessage<string, double>(_temp); });

            RegisterChild(_transformer);
            Log.Trace("Constructor Finished");
        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<IInputMessage<string, double>> OutputBlock { get { return _transformer; } }

        #region Configure this class to use ATAP.Utilities.Logging
        internal static ILog Log { get; set; }

        static ParseSingleInputStringFormattedAsJSONToInputMessage() {
            Log = LogProvider.For<ParseSingleInputStringFormattedAsJSONToInputMessage>();
        }
        #endregion Configure this class to use ATAP.Utilities.Logging
    }

    public class ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection : Dataflow<string, InputMessage<string, double>> {
        // Head and tail 
        TransformManyBlock<string, InputMessage<string, double>> _transformer;

        public ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection() : this(CalculateAndStoreFromInputAndAsyncTermsOptions.Default) {
        }

        public ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection(CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            Log.Trace("Constructor starting");
            // create the output via a TransformManyBlock
            _transformer = new TransformManyBlock<string, InputMessage<string, double>>(_input => { IEnumerable<InputMessage<string, double>> _coll;
                try
                {
                    Log.Trace("Deserialize Starting");
                    _coll = JsonConvert.DeserializeObject<IEnumerable<InputMessage<string, double>>>(_input);
                    Log.Trace("Deserialize Finished");
                }
                catch
                {
                    ArgumentException e = new ArgumentException($"{_input} does not match the needed input pattern");
                    Log.WarnException("Exception", e, "unused");
                    throw e;
                }
                return _coll; });
            RegisterChild(_transformer);
            Log.Trace("Constructor Finished");
        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<InputMessage<string, double>> OutputBlock { get { return _transformer; } }

        #region Configure this class to use ATAP.Utilities.Logging
        // Internal class logger for this class
        static ILog log;

        static ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection() {
            log = LogProvider.For<ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection>();
        }

        internal static ILog Log { get => log; set => log = value; }
        #endregion Configure this class to use ATAP.Utilities.Logging
    }
}





