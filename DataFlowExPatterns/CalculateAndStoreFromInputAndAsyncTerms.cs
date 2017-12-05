using System;
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
        // External http client library.
        IWebGet _webGet;
        Timer asyncFetchCheckTimer;
        Dataflow _terminator;

        // Constructor
        public CalculateAndStoreFromInputAndAsyncTerms(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            Log.Trace("Constructor starting");

            _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
            _webGet = webGet;
            _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;

            // The terminal block performs both the Compute  and the Store operations
            Log.Trace("Creating _terminator");
            var _terminator = new ActionBlock<InternalMessage<string>>(_input => { Log.Trace("_terminator received InternalMessage");
                // do the calculation for all KeyValuePairs in terms1
                var r1 = 0.0;
                _input.Value.terms1.ToList()
                    .ForEach(kvp => { r1 += kvp.Value /
                                              calculateAndStoreFromInputAndAsyncTermsObservableData.Term1COD[kvp.Key]; });
                // Store the pr value
                calculateAndStoreFromInputAndAsyncTermsObservableData.RecordR(_input.Value.k1,
                                                                              _input.Value.k2,
                                                                              Convert.ToDecimal(r1)); }).ToDataflow(calculateAndStoreFromInputAndAsyncTermsOptions);

            // this block accepts messages where isReadyToCalculate is false, and buffers them
            Log.Trace("Creating _dynamicBuffers");
            DynamicBuffers _dynamicBuffers = new DynamicBuffers(calculateAndStoreFromInputAndAsyncTermsObservableData,
                                                                webGet,
                                                                calculateAndStoreFromInputAndAsyncTermsOptions);

            // foreach IInputMessage<TKeyTerm1,TValueTerm1>, create an internal message that adds the terms1 signature and the bool used by the routing predicate
            // the output is k1, k2, c1, bool, and the output is routed on the bool value
            Log.Trace("Creating _accepter");
            var _accepter = new TransformBlock<IInputMessage<string, double>, InternalMessage<string>>(_input => {
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
                KeySignature<string> sig = new KeySignature<string>(_input.Value.terms1.Keys);

                // Is the sig.largest in the SigIsReadyTerm1COD dictionary? set the output bool accordingly
                bool isReadyToCalculate = SigIsReadyTerm1COD.ContainsKey(sig.Longest());

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


        #region Dataflow Input and Output blocks Accessors
        public override ITargetBlock<IInputMessage<string, double>> InputBlock { get { return this._headBlock; } }
        #endregion Dataflow Input and Output blocks Accessors
        // ToDo:  make abstract and replace hard coded string with the type passed when the dataflow pattern is declared
        public class DynamicBuffers : DataDispatcher<InternalMessage<string>, KeySignature<string>> {
            CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
            CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;
            IWebGet _webGet;

            public DynamicBuffers(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(@out => @out.Value.sig) {
                Log.Trace("Constructor Starting");
                _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
                _webGet = webGet;
                _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;
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
            protected override Dataflow<InternalMessage<string>> CreateChildFlow(KeySignature<string> sig) {
                // dynamically create a TransientBuffer buffer. The dispatchKey is based upon the value of sig
                // pass sig to the TransientBuffer constructor, and wget, and the ObservableData class
                // so the TransientBuffer can create the async tasks to fetch each individual term of the sig
                Log.Trace("CreateChildFlow is creating _buffer");

                var _buffer = new TransientBuffer(sig,
                                                  _calculateAndStoreFromInputAndAsyncTermsObservableData,
                                                  WebGet,
                                                  _calculateAndStoreFromInputAndAsyncTermsOptions);
                // Store the sig._individualElements collection and this buffer into sigIsWaitingForCompletion COD
                Log.Trace("CreateChildFlow is storing {0} in IsFetchingSigOfTerm1",
                          sig.Longest());
                try
                {
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
            internal class TransientBuffer : Dataflow<InternalMessage<string>, InternalMessage<string>> {
                // The TPL block that buffers the data.
                BufferBlock<InternalMessage<string>> _buffer;
                CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
                CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;

                public TransientBuffer(KeySignature<string> sig, CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
                    Log.Trace("Constructor Starting");
                    Log.Trace("ElementSet: {0}", sig.Longest().ToString());
                    _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
                    _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;

                    Log.Trace("Creating _buffer");
                    _buffer = new BufferBlock<InternalMessage<string>>();

                    Log.Trace("Registering _buffer");
                    RegisterChild(_buffer);
                    // critical section
                    // iterate each individual element of the sig, and get those that are not already present in the COD IsFetchingIndividualElementsOfTerm1
                    foreach(var element in sig.IndividualElements)
                    {
                        if (!IsFetchingIndividualElementsOfTerm1.ContainsKey(element))
                        {
                            // For each element that is not already being fetched, start the async task to fetch it
                            Log.Trace($"Fetching AsyncWebGet for {element} and storing away task");
                            // call the async function that fetches the information for each individual element in the elementSet
                            // record the individual element it's corresponding task in the IsFetchingIndividualElementsOfTerm1
                            IsFetchingIndividualElementsOfTerm1[element] = webGet.AsyncWebGet<double>(element);
                        }
                    }
                    // Record into IsFetchingSigOfTerm1COD the sig.Longest as key, and for the data,
                    // create a COD whose keys are the keys of sig.IndividualElements in the IsFetchingSigOfTerm1COD
                    Log.Trace("Creating new concurrent dictionary");
                    var x = new ConcurrentObservableDictionary<string, byte>();
                     foreach (var element in sig.IndividualElements)
                    {
                        Log.Trace($"Creating an entry for {element}" );
                        x[element] = default;
                    }
                    Log.Trace($"Creating an entry for IsFetchingSigOfTerm1COD. Key is {sig.Longest()} data is x");
                    IsFetchingSigOfTerm1COD[sig.Longest()] = x;
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
                #region ObservableDataAccessors

                ConcurrentObservableDictionary<string, Task> IsFetchingIndividualElementsOfTerm1
                {
                    get => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualElementsOfTerm1; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualElementsOfTerm1 =
value;
                }
                ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> IsFetchingSigOfTerm1COD { get => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingSigOfTerm1COD; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingSigOfTerm1COD =
                    value; }
                #endregion ObservableDataAccessors    
                #region Dataflow Input and Output blocks Accessors
                public override ITargetBlock<InternalMessage<string>> InputBlock { get { return this._buffer; } }

                public override ISourceBlock<InternalMessage<string>> OutputBlock { get { return this._buffer; } }
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

        #region critical section that periodicly checks on the status of the outstanding tasks that are fetching terms
        // This is the method called, under a number of different conditions, to determine if the Async tasks that fetch a particular Term1 has completed
        /// <summary>
        /// Checks the asynchronous tasks.
        /// </summary>
        /// 
        void CheckAsyncTasks()
        {
            Log.Trace("Starting the CheckAsyncTasks method");
            bool unfinished;
            // iterate each individual term of the sig, and get those that are not already present in the COD IsFetchingIndividualElementsOfTerm1
            IsFetchingSigOfTerm1COD.Keys.ToList().ForEach(sigLongest => {
                unfinished = false;
                Log.Trace("Iterating IsFetchingSigOfTerm1COD.Keys, now on {0}", sigLongest);
                IsFetchingSigOfTerm1COD[sigLongest].Keys.ToList().ForEach(element =>
                {
                    Log.Trace("Iterating IsFetchingSigOfTerm1COD[{0}].Keys, now on {1}", sigLongest, element);
                    unfinished &= IsFetchingIndividualElementsOfTerm1[element].IsCompleted;
                    if (!unfinished)
                    {
                        Log.Trace("sigLongest {0} is now finished", sigLongest);
                        // if sigLongest is finished, but not yet a key in SigIsReadyTerm1COD then this is the first loop where it is finally ready
                        if (!SigIsReadyTerm1COD.ContainsKey(sigLongest))
                        {
                            // attach the transientBlock to the _terminator  
                            Log.Trace("attaching buffer {0} to terminator block, based on sigLongest {1}", 1, sigLongest);
                            // attach completion first
                            //_terminator.RegisterDependency(_buffer);
                            // attach data linkage second
                           // _buffer.LinkTo(_terminator);
                            // put sigLongest into the hashset ElementSetsOfTerm1Ready =>SigIsReadyTerm1COD
                            Log.Trace("sigLongest {0} is now in the SigIsReadyTerm1COD", sigLongest);
                            SigIsReadyTerm1COD[sigLongest] = default(byte);
                            //remove this sigLongest from the IsFetchingSigOfTerm1COD => FetchingElementSetsOfTerm1COD dictionary
                            IsFetchingSigOfTerm1COD.Remove(sigLongest);
                            Log.Trace("sigLongest {0} has been removed from the SigIsReadyTerm1COD", sigLongest);
                        }
                    }
                    else
                    {
                        Log.Trace("sigLongest {0} is still unfinished", sigLongest);
                    }
                });
            });


            Log.Trace("Leaving the CheckAsyncTasks method");
        }
        #endregion

        void AsyncFetchCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Trace("Starting the AsyncFetchCheckTimer_Elapsed");
            AsyncFetchCheckTimer.Stop();
            CheckAsyncTasks();
            AsyncFetchCheckTimer.Start();
            Log.Trace("Leaving the AsyncFetchCheckTimer_Elapsed");
        }

        #region timer Accessors
         Timer AsyncFetchCheckTimer
        {
            get => asyncFetchCheckTimer; set => asyncFetchCheckTimer =
value;
        }
        #endregion timer Accessors
        #region ObservableDataAccessors

        ConcurrentObservableDictionary<string, Task> IsFetchingIndividualElementsOfTerm1 { get => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualElementsOfTerm1; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualElementsOfTerm1 =
            value; }

        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> IsFetchingSigOfTerm1COD
        {
            get => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingSigOfTerm1COD; set => _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingSigOfTerm1COD =
value;
        }

        ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1COD
        { get => this._calculateAndStoreFromInputAndAsyncTermsObservableData.SigIsReadyTerm1COD; set => this._calculateAndStoreFromInputAndAsyncTermsObservableData.SigIsReadyTerm1COD =
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

        protected override void CleanUp(Exception e)
        {
            Log.Trace("Starting Cleanup and calling base.Cleanup");
            base.CleanUp(e);
            Log.Trace("Cleanup after base");
            // dispose of the asyncFetchCheckTimer
            Log.Trace("Disposing the AsyncFetchCheckTimer");
            AsyncFetchCheckTimer.Dispose();
            Log.Trace("Cleanup complete");
        }

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
/* This block of comments were an earlier version prior to using DataDispatcher
            // a collection of DataFlowEx "buffers" to hold specific classes of messages
           // ConcurrentDictionary<string, Dataflow<InternalMessage>> _buffers = new ConcurrentDictionary<string, Dataflow<InternalMessage>>();
            
            // Dataflow<InternalMessage> _emitterBlock;
            // this block accepts messages where isReadyToCalculate is false, and buffers them
            var _waitQueue = new BufferBlock<InternalMessage>(_input =>
            {
                 // Emitter
                
                // if all the keys in this messages terms1 dictionary are in the keys of AreReadyToCalculate, just forward the message
                // Put the test here again at the top of the block in case the termn was populated between the time the message left the _accepter and got sent to the _waitQueue
                // ToDo don't use a string, use a collection of keys
                if (_sigIsReadyTerm1COD.ContainsKey(_input.c1)) return _input;

                // does a dataflowEX exist for the class of messages having this set of keys in its terms1 dictionary?
                if (_buffers.ContainsKey(_input.sig))
                {
                    // if so, put the message on that dataflowEX (the LinkTo Predicate will ensure the right dataflowEx gets this message)
                    return _input;
                } else
                {
                    // if not, create a dataflowEX for this class of messages and put the message into it.
                    _buffers[_input.sig] = new BufferBlock<InternalMessage>().ToDataflow();
                    _buffers[_input.sig].Name = _input.sig;
                    _emitterBlock.LinkTo(_buffers[_input.sig] , @out => @out.sig== _input.sig);
                    _terminator.RegisterDependency(_buffers[_input.sig]);

                }
                //
                // await all of the tasks needed to put all of values for all the keys of terms1 into the Term1COD
                // this will create a synchronizationContext at this point
                // when any individual task that fetches the value of term1 for any single key c finishes
                // processing resumes here
                // populate the Term1 COD for the single key c that just finished
                calculateAndStoreFromInputAndAsyncTermsObservableData.Term1COD[_input.c1] =
                // update the _sigIsReadyTerm1COD for the single key c that just finished
                _sigIsReadyTerm1COD[_input.c1] = default;
                // does this the single key c that just finished complete the set of c's needed to dequeue a class of messages
                // if so, release all the messages in that queue to the output.
                // See if all the values of the keys in this message's HR dictionary are already in _isFetchingIndividualElementsOfTerm1COD
                // and if not, create the async tasks that will populate them
                if (!_isFetchingIndividualElementsOfTerm1COD.ContainsKey(_input.c1))
                {
                    //todo make this into something that returns an awaitable task
                    _isFetchingIndividualElementsOfTerm1COD[_input.c1] = default;

                }
                // populate the Term1 COD for the keys c1..cn in the dictionary HR
                calculateAndStoreFromInputAndAsyncTermsObservableData.Term1COD[_input.c1] = 10.0;
                return _input;
            }).ToDataflow();
            */

/*
 
                    // this block accepts messages where isReadyToCalculate is false, and buffers them
                    var bufferHeterogeneousMessagesC1IsN = new TransformBlock<(string k1, string k2, string c1, double hr, bool isReadyToCalculate), (string k1, string k2, string c1, double hr, bool isReadyToCalculate)>(_input =>
                    {
                        return _input;
                    });
                    // this block accepts messages where isReadyToCalculate is false, and buffers them
                    var bufferHomogeneousMessagesC1Is2 = new TransformBlock<(string k1, string k2, string c1, double hr, bool isReadyToCalculate), (string k1, string k2, string c1, double hr, bool isReadyToCalculate)>(_input =>
                    {
                        return _input;
                    });
                    // Link Accept1 to bufferHetrogeniusMessages when the message has isReadyToCalculate = false
                    Accept1.LinkTo(bufferHeterogeneousMessagesC1IsN, mc => !mc.isReadyToCalculate);
                    // Link bufferHeterogeneousMessagesC1IsN to bufferHomogeneousMessagesC1Is2 when the message has c1=2
                    bufferHeterogeneousMessagesC1IsN.LinkTo(bufferHomogeneousMessagesC1Is2, mc => mc.c1 == "c1=2");
                    // Link bufferHomogeneousMessagesC1Is2 to calculateResults
                    bufferHomogeneousMessagesC1Is2.LinkTo(calculateResults);
                    //Link calculateResults to populateResults
                    calculateResults.LinkTo(populateResults);

                    // Link together the completion/continuation tasks
                    Accept1.Completion.ContinueWith(t =>
                    {
                        if (t.IsFaulted) ((IDataflowBlock)calculateResults).Fault(t.Exception); else calculateResults.Complete();
                    });
                    Accept1.Completion.ContinueWith(t =>
                    {
                        if (t.IsFaulted) ((IDataflowBlock)calculateResults).Fault(t.Exception); else calculateResults.Complete();
                        if (t.IsFaulted) ((IDataflowBlock)bufferHeterogeneousMessagesC1IsN).Fault(t.Exception); else bufferHeterogeneousMessagesC1IsN.Complete();
                    });

                    Task.WhenAll(calculateResults.Completion, bufferHeterogeneousMessagesC1IsN.Completion)
                    .ContinueWith(t =>
                    {
                        populateResults.Complete();
                    });

                    bufferHeterogeneousMessagesC1IsN.Completion.ContinueWith(t =>
                    {
                        if (t.IsFaulted) ((IDataflowBlock)bufferHomogeneousMessagesC1Is2).Fault(t.Exception); else bufferHomogeneousMessagesC1Is2.Complete();
                    });

                    bufferHomogeneousMessagesC1Is2.Completion.ContinueWith(t =>
                    {
                        if (t.IsFaulted) ((IDataflowBlock)calculateResults).Fault(t.Exception); else calculateResults.Complete();
                    });

                    calculateResults.Completion.ContinueWith(t =>
                    {
                        if (t.IsFaulted) ((IDataflowBlock)populateResults).Fault(t.Exception); else populateResults.Complete();
                    });

    */

/*
             #region critical section that periodicly checks on the status of the outstanding tasks that are fetching terms
        // This is the method called, under a number of different conditions, to determine if the Async tasks that fetch a particular Term1 has completed
        /// <summary>
        /// Checks the asynchronous tasks.
        /// </summary>
        void CheckAsyncTasks()
        {
            Log.Trace("Starting the CheckAsyncTasks method");
            // critical section
            bool unfinished;
            // iterate each individual term of the sig, and get those that are not already present in the COD IsFetchingIndividualElementsOfTerm1
            IsFetchingSigOfTerm1COD.Keys
                .Where(sigLongest =>
                { 
                    unfinished = false;
                    IsFetchingSigOfTerm1COD[sigLongest].Keys
                        .Where(element =>
                        {
                            unfinished &= IsFetchingIndividualElementsOfTerm1[element].IsCompleted;
                        if (!unfinished)
                    {
                        // if sig is finished, and Ready is false this is the first loop where it is finally ready
                        if (!SigIsReadyTerm1COD[sigLongest])
                        {
                            // attach the transientBlock to the _terminator  
                            // set the SigIsReadyTerm1COD[sigLongest] to true
                            SigIsReadyTerm1COD[sigLongest] = true;
                            //remove this sigLongest from the IsFetchingSigOfTerm1COD dictionary
                            IsFetchingSigOfTerm1COD.Remove(sigLongest)
                        }
                    }
                }

                        // Get the collection of keys from 
                        //Log.Trace("keycollction of waitingforsi");
                        //             _calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingSigOfTerm1COD.ToString());

                // Get the collection of the output predicates for TransientBuffer subflows in the DynamicBuffers dataflow that are not linked to _terminator
                // ConcurrentDictionary<string, DynamicBuffers.TransientBuffer> _waitingTBSigs = new ConcurrentDictionary<string, DynamicBuffers.TransientBuffer>();
                //var b = _dynamicBuffers.Blocks;
                //var c = _dynamicBuffers.Children;
                // This is where the issues/question has been raised on the dataFlowEx project
                // _dynamicBuffers.Children.Where<IDataflowDependency>(child=>child.Blocks.Where<IDataflowBlock>(block=>block.GetBufferCount().Item2 > 0));
                // get the collection of keys corresponding to the collection of term1 async tasks that have not completed.
                // ConcurrentDictionary<string, byte> _waiting1TermFetchSigs = new ConcurrentDictionary<string, byte>();
                //_calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualElementsOfTerm1
                //    .Where(kvp => kvp.Value.IsCompleted == false)
                //    .ToList()
                //    .ForEach(kvp => _waiting1TermFetchSigs.TryAdd(kvp.Key,
                //                                                  default(byte)));

                // iterate over all TransientBuffer subflows whose predicate sig does not match the collection of sigs generated by all uncompleted tasks
                // Decompose the sig into the list of original individual keys that came in with the terms1
                // Get the collection of those individual keys that are no longer waiting
                // Create all possible signatures
                //ConcurrentDictionary<string, byte> allSigs = new ConcurrentDictionary<string, byte>();
                //IEnumerable<string> ss = _waitingTBSigs.Keys
                //    .Except(_waiting1TermFetchSigs.Keys);
                //// for each of those TransientBuffer subflows, link them to the _terminator dataflow

                //_waitingTBSigs
                //    .Where(kvp => ss.Contains(kvp.Key))
                //    .ToList()
                //    .ForEach(kvp => kvp.Value.LinkTo(_terminator));
                // IEnumerable<DynamicBuffers.TransientBuffer> _readyToDeQueue =
                // for each of those TransientBuffer subflows, link them to the _terminator dataflow

            try
            {
                //foreach (DynamicBuffers.TransientBuffer tb in _readyToDeQueue) tb.LinkTo(_terminator);
            }
            catch (Exception)
            {

                throw;
            }
            // if all tasks are complete
            //         set sigIsReadyToCalculateAndStore for this sig.longest to true
            //  link the data flow for this block to the _terminator

            Log.Trace("Leaving the CheckAsyncTasks method");
        }

        void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // go through the list of async fetch tasks for all the individual terms that need to complete in order for this TransientBuffer to release its output buffered messages
            // if all tasks are complete
            //         set sigIsReadyToCalculateAndStore for this sig.longest to true
            //  link the data flow for this block to the _terminator
            //  attach an event handler to teh buffer's count property such that when it reaches zero, the event handler sets this TransientBuffer's status to completed
            // todo figure out how cancellation will work
            // ToDo figure out how faulting and exception handling will work
            // 
            //receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        // check on the async fetch tasks when a timer expires
        // Create a timer, attach its callback to CheckAsyncTasks, setup its expiration, repeat indefinitely


        #endregion critical section that periodicly checks on the status of the outstanding tasks that are fetching terms

 * */
