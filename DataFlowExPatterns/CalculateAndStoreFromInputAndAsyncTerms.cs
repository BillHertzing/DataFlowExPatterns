﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using ATAP.Utilities.Logging.Logging;
using Gridsum.DataflowEx;
using Newtonsoft.Json;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms {
    /// <summary>
    /// This DataFlow will solve a Result (decimal), for each input message, and store that into a ConcurrentObservableDictionary (COD) 
    ///   that was declared in the accompanying ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms project and passed into this dataflow's constructor
    ///   The overall structure of teh graph starts with a Head block (_acceptor), and ends with a Action block that performs a calculation and stores the results (_bSolveStore)
    ///   The calculation being performed depends on Terms that are retrieved via an async fetch. 
    ///   The terms being retrieved are defined by the keys to the terms1 IReadOnlyDictionary that are part of the message
    ///   If all of async data needed, specified by all of the keys of the terms1, has been received, the message is sent from the _acceptor to the _bSolveStore
    ///   Any message whose terms1 keys' async data, specified by all of the keys of the terms1, has not yet been retrieved, the message is shunted to a DataDispatcher.
    ///   If the set of all of the keys of the terms1 of the message has never before been seen by the DataDispatcher, then the DataDispatcher dynamically creates a TransientBuffer for that set of terms1 keys
    ///   When all of the async tasks that retrieve the data for each key found in terms1 completes, all of the messages buffered in a TransientBuffer are released to the _bSolveStore
    ///   When all of the messages buffered in a TransientBuffer are released to the _bSolveStore, the TransientBuffer is disposed of.
    ///   ToDo: a maxtimeToWait for the async fetch task to complete, after the _acceptor receives a Complete signal, before declaring a transient block faulted. 
    /// </summary>
    public abstract partial class SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult> : Dataflow<IInputMessage<ITStoreP, ITSolveP>> {
        internal ILog Log { get; }

        Dataflow<IInputMessage<ITStoreP, ITSolveP>, IInternalMessage> _bAccepter;
        DynamicBuffers _bDynamicBuffers;
        Dataflow<IInternalMessage> _bSolveStore;
        SolveAndStoreObservableData<ITStoreP, ITSolveP, TResult> _solveAndStoreObservableData;
        SolveAndStoreOptions _solveAndStoreOptions;
        // Head of this dataflow graph
        ITargetBlock<IInputMessage<ITStoreP, ITSolveP>> _headBlock;
        // A thread-safe place to keep the TransientBuffers associated with each ElementSet of each term
        //ToDo another layer of indirection - array or dictionary - not sure...
        //ConcurrentDictionary<string, Dataflow<IInternalMessage, IInternalMessage>> _transientBuffersForElementSets;
        ConcurrentDictionary<ImmutableHashSet<string>, Dataflow<IInternalMessage, IInternalMessage>>[] _transientBuffersForElementSets;
        // External http client library.
        IWebGet _webGet;
        // how often to check on the completion  elements being fetched
        Timer asyncFetchCheckTimer;
        Action<IInputMessage<ITStoreP, ITSolveP>> _solveAndStoreFunc;
        // Constructor
        public SolveAndStoreFromInputAndAsyncTerms(SolveAndStoreObservableData<ITStoreP, ITSolveP, TResult> solveAndStoreObservableData, IWebGet webGet, Action<IInputMessage<ITStoreP, ITSolveP>> solveAndStoreFunc, SolveAndStoreOptions solveAndStoreOptions) : base(solveAndStoreOptions) {

            Log = LogProvider.GetLogger( nameof(SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult>));
            Log.Trace($"Constructor for {nameof(SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult>)} starting");

            _solveAndStoreObservableData = solveAndStoreObservableData;
            _webGet = webGet;
            _solveAndStoreOptions = solveAndStoreOptions;
            _solveAndStoreFunc = solveAndStoreFunc;
            // Create a place to store the TransientBuffers that buffer messages while waiting For all elements that make up the ElementSets Of Term1 to finish fetching
            //ToDo change string to KeySignature.HashSet
            // Number of terms that require async fetches for their terms

            _transientBuffersForElementSets = new ConcurrentDictionary < ImmutableHashSet<string>, Dataflow < IInternalMessage, IInternalMessage >>[]  { new ConcurrentDictionary<ImmutableHashSet<string>, Dataflow<IInternalMessage, IInternalMessage>>(),new ConcurrentDictionary<ImmutableHashSet<string>, Dataflow<IInternalMessage, IInternalMessage>>()};
            new ConcurrentDictionary<ImmutableHashSet<string>, Dataflow<IInternalMessage, IInternalMessage>> ();

            // foreach IInputMessage, create an IInternal message that adds the KeySignature for the SolveP for each term  and the bool used by the routing predicate
            Log.Trace("Creating _bAccepter");
            _bAccepter = new TransformBlock<IInputMessage<ITStoreP, ITSolveP>, IInternalMessage> (_input => 
                {
                    Log.Trace("Accepter received IInputMessage");
                    // ToDo also check on the async tasks check when an upstream completion occurs
                    // ToDo add exception handling to ensure the tasks, as well as the async method's resources, are released if any blocks in the dataflow fault

                    // create a HashSet from the set of keys found in terms1
                    KeySignature<string> sig = new KeySignature<string>(_input.SolveP);

                    // Is the sig.largest in the ElementSetsOfTerm1Ready dictionary? set the output bool accordingly
                    bool isReadyToSolve = _solveAndStoreObservableData.ElementSetsOfTerm1Ready.ContainsKey(sig.Longest());

                    // Pass the message along to the next block, which will be either the _bSolveStore, or the _bDynamicBuffers
                    return new InternalMessage (_input.StoreP, _input.SolveP,  sig, isReadyToSolve);
                }).ToDataflow();

            // this block accepts messages where isReadyToSolve is false, and buffers them
            Log.Trace("Creating _bDynamicBuffers");
            _bDynamicBuffers = new DynamicBuffers(this);

            // The terminator block performs both the Solve and the Store operations
            Log.Trace("Creating _bSolveStore");
            _bSolveStore = new ActionBlock<IInternalMessage>(_input => {
                Log.Trace($"_bSolveStore received InternalMessage having signature {_input.sig.Longest()}");
                // solve the equation for the input and all terms and then store it
                _solveAndStoreFunc(_input);
                /*
                var r1 = 0.0;
                foreach(var kvp in _input.SolveP) {
                    r1 += kvp.Value /
                        _solveAndStoreObservableData.FetchedIndividualElementsOfTerm1[kvp.Key];
                }

                // Store the results value
                solveAndStoreObservableData.RecordR(_input.StoreP,
                                                                              Convert.ToDecimal(r1));
                */
            }).ToDataflow(solveAndStoreOptions);


            #region create asyncFetchCheckTimer and connect callback
            // Create a timer that is used to check on the async fetch tasks, the  async fetch tasks check-for-completion loop timer
            // the timer has its interval from the options passed into this constructor, it will restart and the event handler will stop the timer and start the timer each time
            // ToDo add the timer that checks on the health of the async fetch tasks check-for-completion loop every DefaultAsyncFetchTimeout interval, expecting it to provide a heartbeat, 
            // The Cleanup method will call this timers Dispose method
            // the event handler's job is to call CheckAsyncTasks which will check for completed fetches and link the child Transient buffers to the _bSolveStore
            Log.Trace("creating and starting the asyncFetchCheckTimer");
            asyncFetchCheckTimer = new Timer(_solveAndStoreOptions.AsyncFetchTimeInterval.TotalMilliseconds);
            asyncFetchCheckTimer.AutoReset = true;
            // set the event handler (callback) for this timer to the function for async fetch tasks check-for-completion loop
            asyncFetchCheckTimer.Elapsed += new ElapsedEventHandler(asyncFetchCheckTimer_Elapsed);
            asyncFetchCheckTimer.Start();
            #endregion create asyncFetchCheckTimer and connect callback
            _bAccepter.Name = "_bAccepter";
            _bSolveStore.Name = "_bSolveStore";
            _bDynamicBuffers.Name = "_bDynamicBuffers";

            // Link the data flow
            Log.Trace("Linking dataflow between blocks");
            // Link _bAccepter to _bSolveStore when the InternalMessage.Value has isReadyToSolve = true
            _bAccepter.LinkTo(_bSolveStore,
                              internalMessage => internalMessage.IsReadyToSolve);
            // Link _bAccepter to _bDynamicBuffers when the  InternalMessage.Value has isReadyToSolve = false
            _bAccepter.LinkTo(_bDynamicBuffers,
                              internalMessage => !internalMessage.IsReadyToSolve);
            // data flow linkage of the dynamically created TransientBuffer children to the _bSolveStore is complex and handled elsewhere

            // Link the completion tasks
            Log.Trace("Linking completion between blocks");
            _bDynamicBuffers.RegisterDependency(_bAccepter);
            _bSolveStore.RegisterDependency(_bAccepter);
            _bSolveStore.RegisterDependency(_bDynamicBuffers);
            // Completion linkage of the dynamically created TransientBuffer children to the _bSolveStore is complex and handled elsewhere

            Log.Trace("Registering Children");
            this.RegisterChild(_bAccepter);
            this.RegisterChild(_bSolveStore);
            this.RegisterChild(_bDynamicBuffers);

            // set the InputBlock for this dataflow graph to be the InputBlock of the _acceptor
            this._headBlock = _bAccepter.InputBlock;
            // ToDo: add an optional constructor parameter that supplies an initial list of elements that can start pre-fetching for each term
            Log.Trace("Constructor Finished");
        }

        void asyncFetchCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            Log.Trace("Starting the asyncFetchCheckTimer_Elapsed");
            asyncFetchCheckTimer.Stop();
            CheckAsyncTasks();
            asyncFetchCheckTimer.Start();
            Log.Trace("Leaving the asyncFetchCheckTimer_Elapsed");
        }

        #region critical section CheckAsyncTasks that periodicly checks on the status of the outstanding tasks that are fetching terms
        // This is the method called, under a number of different conditions, to determine if the Async tasks that fetch a particular Term1 has completed
        /// <summary>
        /// Checks the asynchronous tasks.
        /// </summary>
        void CheckAsyncTasks() {
            Log.Trace("Starting the CheckAsyncTasks method");
            bool unfinished;
            // iterate each individual term of the sig, and get those that are not already present in the COD FetchingIndividualElementsOfTerm1
            foreach(var sigLongest in _solveAndStoreObservableData.FetchingElementSetsOfTerm1.Keys) {
                unfinished = false;
                Log.Trace($"Iterating FetchingElementSetsOfTerm1.Keys, now on {sigLongest}");
                foreach(var element in _solveAndStoreObservableData.FetchingElementSetsOfTerm1[sigLongest].Keys) {
                    Log.Trace($"Iterating FetchingElementSetsOfTerm1[{sigLongest}].Keys, now on {element}");
                    // If the element is completed, store the results into FetchedIndividualElementsOfTerm1 if that key does not yet exist (not the most efficient way)
                    //ToDo improve this algorithm in some way so it doesn't have to iterate teh entire dictionary every element every time
                    if(_solveAndStoreObservableData.FetchingIndividualElementsOfTerm1[element].IsCompleted
                        && !_solveAndStoreObservableData.FetchedIndividualElementsOfTerm1.ContainsKey(element)) {
                        _solveAndStoreObservableData.FetchedIndividualElementsOfTerm1[element] = _solveAndStoreObservableData.FetchingIndividualElementsOfTerm1[element].Result;
                    }
                    unfinished &= _solveAndStoreObservableData.FetchingIndividualElementsOfTerm1[element].IsCompleted;
                }
                if(!unfinished) {
                    Log.Trace($"sigLongest {sigLongest} is now finished");
                    // if sigLongest is finished, but not yet a key in ElementSetsOfTerm1Ready then this is the first loop where it is finally ready
                    if(!_solveAndStoreObservableData.ElementSetsOfTerm1Ready.ContainsKey(sigLongest)) {
                        // attach the transientBlock data linkage to the _bSolveStore  
                        Log.Trace($"attaching buffer {_transientBuffersForElementSets[sigLongest].Name} to _bsolve block, based on sigLongest {sigLongest}");
                        _transientBuffersForElementSets[sigLongest].LinkTo(_bSolveStore);
                        // put sigLongest into ElementSetsOfTerm1Ready
                        Log.Trace($"sigLongest {sigLongest} is now in the ElementSetsOfTerm1Ready");
                        _solveAndStoreObservableData.ElementSetsOfTerm1Ready[sigLongest] = default;
                        //remove this sigLongest from the FetchingElementSetsOfTerm1 dictionary
                        _solveAndStoreObservableData.FetchingElementSetsOfTerm1.Remove(sigLongest);
                        Log.Trace($"sigLongest {sigLongest} has been removed from the ElementSetsOfTerm1Ready");
                    }
                    else {
                        Log.Trace($"sigLongest {sigLongest} is finished AND it is a key in ElementSetsOfTerm1Ready");
                    }
                }
                else {
                    Log.Trace($"sigLongest {sigLongest} is still unfinished");
                }
            }


            Log.Trace("Leaving the CheckAsyncTasks method");
        }
        #endregion

        protected override void CleanUp(Exception e) {
            Log.Trace("Starting SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult> Cleanup and calling base.Cleanup");
            base.CleanUp(e);
            Log.Trace("Cleanup after base");
            // dispose of the asyncFetchCheckTimer
            Log.Trace("Disposing the asyncFetchCheckTimer");
            asyncFetchCheckTimer.Dispose();
            Log.Trace("Cleanup complete");
        }

        #region Dataflow Input and Output blocks Accessors
        public override ITargetBlock<IInputMessage<ITStoreP, ITSolveP>> InputBlock { get { return this._headBlock; } }

        public Action<IInputMessage<ITStoreP, ITSolveP>> SolveAndStoreFunc { get => _solveAndStoreFunc; set => _solveAndStoreFunc = value; }
        #endregion Dataflow Input and Output blocks Accessors

        class DynamicBuffers : DataDispatcher<IInternalMessage, KeySignature<string>> {
            internal ILog Log { get; }

            SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult> _parent;

            public DynamicBuffers(SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult> parent) : base(@out => @out.sig) {
                Log = LogProvider.GetLogger(nameof(DynamicBuffers));

                Log.Trace("Constructor Starting");
                _parent = parent;
                Log.Trace("Constructor Finished");
            }

            protected override void CleanUp(Exception e) {
                Log.Trace("Starting DynamicBuffers Cleanup and calling base.Cleanup");
                base.CleanUp(e);
                Log.Trace("Cleanup after base");
                Log.Trace("Cleanup complete");
            }

            /// <summary>
            /// This function will create one instance of a TransientBuffer,and will only be called once for each distinct sig (the first time)
            /// </summary>
            /// <param name="sig">The sig.</param>
            /// <returns>Dataflow&lt;InternalMessage&lt;System.String&gt;&gt;.</returns>
            protected override Dataflow<IInternalMessage> CreateChildFlow(KeySignature<string> sig) {
                // dynamically create a TransientBuffer buffer. The dispatchKey is based upon the value of sig
                // the TransientBuffer will create the async tasks to fetch each individual term of the sig
                Log.Trace("CreateChildFlow is creating _buffer");
                var _transientBuffer = new DynamicBuffers.TransientBuffer(sig,
                                                                          this);

                // Link the completion 
                // attach completion of _bSolveStore to this _transientBuffer
                _parent._bSolveStore.RegisterDependency(_transientBuffer);
                // The child flow created within a DataDispatcher is automatically attached to the action block which creates it
                // so no need to RegisterDependency of this _transientBuffer to the _bDynamicBuffers

                // Store the hashSet of elements for Term1 (as key) and this _transientBuffer (as value) into _transientBuffersForElementSets
                Log.Trace($"CreateChildFlow is storing {_transientBuffer.Name} in _transientBuffersForElementSets keyed by {sig.Longest()}");
                try
                {
                    _parent._transientBuffersForElementSets[0][sig.Longest()] = _transientBuffer;
                }
                catch
                {
                    Log.Error($"error when trying to store {_transientBuffer.Name} in _transientBuffersForElementSets keyed by {sig.Longest()}");
                    throw new Exception($"error when trying to store {_transientBuffer.Name} in _transientBuffersForElementSets keyed by {sig.Longest()}");
                }


                // no need to call RegisterChild(_buffer) here as DataDispatcher will call automatically
                Log.Trace($"CreateChildFlow has created {_transientBuffer.Name} and is returning");
                // return the TransientBuffer
                return _transientBuffer;
            }

            /// <summary>
            /// Transient Buffer node for a single sig
            /// </summary>
            class TransientBuffer : Dataflow<IInternalMessage, IInternalMessage> {
                internal ILog Log { get; }

                // The TPL block that buffers the data.
                BufferBlock<IInternalMessage> _buffer;
                DynamicBuffers _parent;

                public TransientBuffer(KeySignature<string> sig, DynamicBuffers parent) : base(parent._parent._solveAndStoreOptions) {
                    Log = LogProvider.GetLogger(nameof(TransientBuffer));

                    Log.Trace("Constructor Starting");
                    this._parent = parent;
                    Log.Trace($"ElementSet: {sig.Longest().ToString()}");

                    Log.Trace("Creating _buffer");
                    _buffer = new BufferBlock<IInternalMessage>();

                    Log.Trace("Registering _buffer");
                    RegisterChild(_buffer);
                    // critical section
                    // iterate each individual element of the sig, and get those that are not already present in the COD FetchingIndividualElementsOfTerm1
                    foreach(var element in sig.IndividualElements) {
                        if(!parent._parent._solveAndStoreObservableData.FetchingIndividualElementsOfTerm1.ContainsKey(element)) {
                            // For each element that is not already being fetched, start the async task to fetch it
                            Log.Trace($"Fetching AsyncWebGet for {element} and storing the task in FetchingIndividualElementsOfTerm1 indexed by [{termid}][{element}]");
                            // call the async function that fetches the information for each individual element in the elementSet
                            // record the individual element and it's corresponding task in the FetchingIndividualElementsOfTerm1
                            parent._parent._solveAndStoreObservableData.FetchingIndividualElementsOfTerm1[element] = parent._parent._webGet.AsyncWebGet<double>(element);
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
                    parent._parent._solveAndStoreObservableData.FetchingElementSetsOfTerm1[sig.Longest()] = x;
                    // If the asyncFetchCheckTimer is not enabled, enable it now.
                    if(!parent._parent.asyncFetchCheckTimer.Enabled) {
                        parent._parent.asyncFetchCheckTimer.Enabled = true;
                    }

                    Log.Trace("Constructor Finished");
                }

                protected override void CleanUp(Exception e) {
                    Log.Trace("Starting TransientBuffer Cleanup and calling base.Cleanup");
                    base.CleanUp(e);
                    Log.Trace("Cleanup after base");
                    // ToDo Cleanup any messages on the transient blocks
                    // remove this TransientBlock's event Handlers from all Tasks representing term fetches, for the individual terms for this TBs sig
                    Log.Trace("Cleanup complete");
                }

                #region Dataflow Input and Output blocks Accessors
                public override ITargetBlock<IInternalMessage> InputBlock { get { return this._buffer; } }

                public override ISourceBlock<IInternalMessage> OutputBlock { get { return this._buffer; } }
                #endregion Dataflow Input and Output blocks Accessors
            }
        }
    }

    public abstract class JSONSingleIMToInputMessage<ITStoreP, ITSolveP> : Dataflow<string, InputMessage<ITStoreP, ITSolveP>> {
        internal ILog Log { get; }

        // Head and tail 
        TransformBlock<string, InputMessage<ITStoreP, ITSolveP>> _transformer;

        public JSONSingleIMToInputMessage() : this(SolveAndStoreOptions.Default) {
        }

        public JSONSingleIMToInputMessage(SolveAndStoreOptions solveAndStoreOptions) : base(solveAndStoreOptions) {
            Log = LogProvider.GetLogger(nameof(JSONSingleIMToInputMessage<ITStoreP, ITSolveP>));

            Log.Trace("Constructor starting");
            // create the output via a TransformBlock
            _transformer = new TransformBlock<string, InputMessage<ITStoreP, ITSolveP>>(_input => {
                InputMessage<ITStoreP, ITSolveP> im;
                try
                {
                    Log.Trace("Deserialize Starting");
                    im = JsonConvert.DeserializeObject<InputMessage<ITStoreP, ITSolveP>>(_input);
                    Log.Trace("Deserialize Finished");
                }
                catch
                {
                    ArgumentException e = new ArgumentException($"{_input} does not match the needed input pattern");
                    Log.WarnException("Exception", e, "unused");
                    throw e;
                }
                return im; });

            RegisterChild(_transformer);
            Log.Trace("Constructor Finished");
        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<InputMessage<ITStoreP, ITSolveP>> OutputBlock { get { return _transformer; } }
    }
    /*
    public class JSONCollectionIMToInputMessageCollection<ITStoreP, ITSolveP, TResult> : Dataflow<string, IEnumerable<IInputMessage<ITStoreP, ITSolveP>>> {
        internal ILog Log { get; }

        // Head and tail 
        TransformManyBlock<string, IEnumerable<IInputMessage<ITStoreP, ITSolveP>>> _transformer;

        public JSONCollectionIMToInputMessageCollection() : this(SolveAndStoreOptions.Default) {
        }

        public JSONCollectionIMToInputMessageCollection(SolveAndStoreOptions solveAndStoreOptions) : base(solveAndStoreOptions) {
                    Log = LogProvider.GetLogger( nameof(JSONCollectionIMToInputMessageCollection<ITStoreP, ITSolveP, TResult>));

            Log.Trace("Constructor for JSONCollectionIMToInputMessageCollection starting");
            // create the output via a TransformManyBlock
            _transformer = new TransformManyBlock<string, IInputMessage<ITStoreP, ITSolveP>>(new Func<string, IEnumerable<IInputMessage<ITStoreP, ITSolveP>>>(this.splitter),
                                                                                                      solveAndStoreOptions.ToExecutionBlockOption())
                .ToDataflow<string, IEnumerable<IInputMessage<ITStoreP, ITSolveP>>>(solveAndStoreOptions,
                                                                                "_transformer");
            RegisterChild(_transformer);
            Log.Trace("Constructor for JSONCollectionIMToInputMessageCollection Finished");
        }

        // Have to use a named method in order to use Yield to return an IEnumerable
        IEnumerable<IInputMessage<ITStoreP, ITSolveP>> splitter(string _input) {
            Log.Trace("Deserialize Starting");
            IEnumerable<IInputMessage<ITStoreP, ITSolveP>> _imcoll;
            try
            {
                _imcoll = JsonConvert.DeserializeObject<IEnumerable<IInputMessage<ITStoreP, ITSolveP>>>(_input);
            }
            catch
            {
                ArgumentException e = new ArgumentException($"{_input} does not match the needed input pattern");
                Log.WarnException("Exception", e, "unused");
                throw e;
            }
            Log.Trace("Deserialize Finished");
            foreach(var im in _imcoll)
            {
                yield return im;
            }
        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<IEnumerable<IInputMessage<ITStoreP, ITSolveP>>> OutputBlock { get { return _transformer; } }
    }
    */
}





