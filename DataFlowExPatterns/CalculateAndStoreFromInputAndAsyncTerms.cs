using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
    public partial class CalculateAndStoreFromInputAndAsyncTerms : Dataflow<InputMessage<string>> {
        // Head of this dataflow graph
        ITargetBlock<InputMessage<string>> _headBlock;

        // a thread-safe place to keep track of which individual key values of the set of key values of Term1 (sig.IndividualTerms) are FetchingIndividualTermKey
        ConcurrentObservableDictionary<string, Task> _isFetchingIndividualTermKey;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are ReadyToCalculate
        ConcurrentObservableDictionary<string, byte> _sigIsReadyToCalculateAndStore;
        CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
        IWebGet _webGet;
        CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;


        // Constructor
        public CalculateAndStoreFromInputAndAsyncTerms(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            _isFetchingIndividualTermKey = calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualTermKey;
            _sigIsReadyToCalculateAndStore = calculateAndStoreFromInputAndAsyncTermsObservableData.SigIsReadyToCalculateAndStore;
            _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
            _webGet = webGet;
            _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;

            // The terminal block performs both the Compute  and the Store operations
            var _terminator = new ActionBlock<InternalMessage<string>>(_input => {
                // do the calculation for all KeyValuePairs in terms1
 var r1 = 0.0;
                _input.Value.terms1.ToList()
                    .ForEach(kvp => { r1 += kvp.Value /
                                              calculateAndStoreFromInputAndAsyncTermsObservableData.TermCOD1[kvp.Key]; });
                // Store the pr value
                calculateAndStoreFromInputAndAsyncTermsObservableData.RecordR(_input.Value.k1,
                                  _input.Value.k2,
                                  Convert.ToDecimal(r1)); }).ToDataflow(calculateAndStoreFromInputAndAsyncTermsOptions);

            // this block accepts messages where isReadyToCalculate is false, and buffers them
            DynamicBuffers _dynamicBuffers = new DynamicBuffers(calculateAndStoreFromInputAndAsyncTermsObservableData, webGet, calculateAndStoreFromInputAndAsyncTermsOptions                                                               );

            // This is the method called, under a number of different conditions, to determine if the Async tasks that fetch a particular t1 of Term1 has completed
            void CheckAsyncTasks()
            {
                // Get the collection of the output predicates for TransientBuffer subflows in the DynamicBuffers dataflow that are not linked to _terminator
                ConcurrentDictionary<string, DynamicBuffers.TransientBuffer> _waitingTBSigs = new ConcurrentDictionary<string, DynamicBuffers.TransientBuffer>();
                var b = _dynamicBuffers.Blocks;
                var c = _dynamicBuffers.Children;
                // This is where the issues/question has been raised on the dataFlowEx project
                // _dynamicBuffers.Children.Where<IDataflowDependency>(child=>child.Blocks.Where<IDataflowBlock>(block=>block.GetBufferCount().Item2 > 0));
                // get the collection of keys corresponding to the collection of term1 async tasks that have not completed.
                ConcurrentDictionary<string, byte> _waiting1TermFetchSigs = new ConcurrentDictionary<string, byte>();
                calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualTermKey
                    .Where(kvp => kvp.Value.IsCompleted == false)
                    .ToList()
                    .ForEach(kvp => _waiting1TermFetchSigs.TryAdd(kvp.Key,
                                                                  default(byte)));

                // iterate over all TransientBuffer subflows whose predicate sig does not match the collection of sigs generated by all uncompleted tasks
                // Decompose the sig into the list of original individual keys that came in with the terms1
                // Get the collection of those individual keys that are no longer waiting
                // Create all possible signatures
                ConcurrentDictionary<string, byte> allSigs = new ConcurrentDictionary<string, byte>();
                IEnumerable<string> ss = _waitingTBSigs.Keys
                    .Except(_waiting1TermFetchSigs.Keys);
                // for each of those TransientBuffer subflows, link them to the _terminator dataflow

                _waitingTBSigs
                    .Where(kvp => ss.Contains(kvp.Key))
                    .ToList()
                    .ForEach(kvp => kvp.Value.LinkTo(_terminator));
                // IEnumerable<DynamicBuffers.TransientBuffer> _readyToDeQueue =
                // for each of those TransientBuffer subflows, link them to the _terminator dataflow
                //foreach (DynamicBuffers.TransientBuffer tb in _readyToDeQueue) tb.LinkTo(_terminator);

            };

            // ToDo also check on the async tasks when a timer expires
            // Create a timer, attach its callback to CheckAsyncTasks, setup its expiration, repeat indefinitely

            // foreach InputMessage<string>, create an internal message that adds the terms1 signature and the bool used by the routing predicate
            // the output is k1, k2, c1, bool, and the output is routed on the bool value
            var _accepter = new TransformBlock<InputMessage<string>, InternalMessage<string>>(_input => {
                // ToDo also check on the async tasks check when an upstream completion occurs
                // ToDo need a default value for how long to wait for an async fetch to complete after an upstream completion occurs
                // ToDo need a constructor and a property that will let a caller change the default value for how long to wait for an async fetch to complete after an upstream completion occurs
                // ToDo add exception handling to ensure the tasks, as well as the async method's resources, are released if any blocks in the dataflow fault
                // CheckAsyncTasks();

                // Work on the _input
                // create a signature from the set of keys found in terms1
 KeySignature<string> sig = new KeySignature<string>(_input.Value.terms1.Keys);

                // Is the sig.largest in the cReadyToCalculate dictionary? set the output bool accordingly
                bool isReadyToCalculate = SigIsReadyToCalculateAndStore.ContainsKey(sig.Longest());

                // Pass the message along to the next block, which will be either the _terminator, or the _dynamicBuffers
                return new InternalMessage<string>((_input.Value.k1, _input.Value.k2, _input.Value.terms1, sig, isReadyToCalculate)); }).ToDataflow();


            _accepter.Name = "_accepter";
            _terminator.Name = "_terminator";
            _dynamicBuffers.Name = "_dynamicBuffers";

            // Link the data flow
            // Link _accepter to _terminator when the InternalMessage.Value has isReadyToCalculate = true
            _accepter.LinkTo(_terminator, m1 => m1.Value.isReadyToCalculate);
            // Link _accepter to _dynamicBuffers when the  InternalMessage.Value has isReadyToCalculate = false
            _accepter.LinkTo(_dynamicBuffers,
                             m1 => !m1.Value.isReadyToCalculate);
            // data flow linkage of the dynamically created TransientBuffer children to the _terminator is complex and handled elsewhere

            // Link the completion tasks
            _dynamicBuffers.RegisterDependency(_accepter);
            _terminator.RegisterDependency(_accepter);
            // Completion linkage of the dynamically created TransientBuffer children to the _terminator is complex and handled elsewhere

            this.RegisterChild(_accepter);
            this.RegisterChild(_terminator);
            this.RegisterChild(_dynamicBuffers);

            // set the InputBlock for this dataflow graph to be the InputBlock of teh _acceptor
            this._headBlock = _accepter.InputBlock;
        // ToDo: start PreparingToCalculate to pre-populate the initial list of C key signatures
        }
        public ConcurrentObservableDictionary<string, Task> IsFetchingIndividualTermKey
        {
            get => _isFetchingIndividualTermKey; set => _isFetchingIndividualTermKey =
value;
        }

        public ConcurrentObservableDictionary<string, byte> SigIsReadyToCalculateAndStore
        {
            get => _sigIsReadyToCalculateAndStore; set => _sigIsReadyToCalculateAndStore =
value;
        }

        public IWebGet WebGet { get => _webGet; set => _webGet = value; }
        public override ITargetBlock<InputMessage<string>> InputBlock { get { return this._headBlock; } }

        // The class used as the message between the _acceptor, The _DynamicBuffers, and the _terminator
        public class InternalMessage<TKeyTerm1> {
            (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) _value;

            public InternalMessage((string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) value) {
                _value = value;
            }

            public (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) Value { get => _value; set => _value =
                value; }
        }

        // ToDo: replace hard coded string with the type passed when the parent dataflow is declared
        public class DynamicBuffers : DataDispatcher<InternalMessage<string>, KeySignature<string>> {
            // a thread-safe place to keep track of which individual key values of the set of key values of Term1 (sig.IndividualTerms) are FetchingIndividualTermKey
            ConcurrentObservableDictionary<string, Task> _isFetchingIndividualTermKey;
            // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are ReadyToCalculate
            ConcurrentObservableDictionary<string, byte> _sigIsReadyToCalculateAndStore;
            CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
            IWebGet _webGet;
            CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;

            public DynamicBuffers(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(@out => @out.Value.sig) {
                _isFetchingIndividualTermKey = calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualTermKey;
                _sigIsReadyToCalculateAndStore = calculateAndStoreFromInputAndAsyncTermsObservableData.SigIsReadyToCalculateAndStore;
                _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
                _webGet = webGet;
                _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;
            }

            /// <summary>
            /// This function will create one instance of a TransientBuffer,and will only be called once for each distinct sig (the first time)
            /// </summary>
            /// <param name="sig">The sig.</param>
            /// <returns>Dataflow&lt;InternalMessage&lt;System.String&gt;&gt;.</returns>
            protected override Dataflow<InternalMessage<string>> CreateChildFlow(KeySignature<string> sig) {
                // dynamically create a subflow buffer. The dispatchKey is based upon the value of sig
                // pass sig to the TransientBuffer constructor so the TransientBuffer can create the async tasks to fetch each individual term of the sig
                // pas the constructor a handle to the ConcurrentObservableDictionary<string, Task>
                var _buffer = new TransientBuffer(sig, _calculateAndStoreFromInputAndAsyncTermsObservableData, _webGet, _calculateAndStoreFromInputAndAsyncTermsOptions);
                // Store the sig._individualTerms collection and this buffer into SigIsWaitingForCompletion COD
                // SigIsWaitingForCompletion.TryAdd
                // no need to call RegisterChild(_buffer) here as DataDispatcher will call automatically
                return _buffer;
            }

            public ConcurrentObservableDictionary<string, Task> IsFetchingIndividualTermKey { get => _isFetchingIndividualTermKey; set => _isFetchingIndividualTermKey =
                value; }

            public ConcurrentObservableDictionary<string, byte> SigIsReadyToCalculateAndStore { get => _sigIsReadyToCalculateAndStore; set => _sigIsReadyToCalculateAndStore =
                value; }

            public IWebGet WebGet { get => _webGet; set => _webGet = value; }

            /// <summary>
            /// Transient Buffer node for a single sig
            /// </summary>
            public class TransientBuffer : Dataflow<InternalMessage<string>, InternalMessage<string>> {
                // The TPL block that buffers the data.
                BufferBlock<InternalMessage<string>> _buffer;
                ConcurrentObservableDictionary<string, Task> _isFetchingIndividualTermKey;
                ConcurrentObservableDictionary<string, byte> _sigIsReadyToCalculateAndStore;
                CalculateAndStoreFromInputAndAsyncTermsObservableData _calculateAndStoreFromInputAndAsyncTermsObservableData;
                IWebGet _webGet;
                CalculateAndStoreFromInputAndAsyncTermsOptions _calculateAndStoreFromInputAndAsyncTermsOptions;


                public TransientBuffer(KeySignature<string> sig, CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
                    _isFetchingIndividualTermKey = calculateAndStoreFromInputAndAsyncTermsObservableData.IsFetchingIndividualTermKey;
                    _sigIsReadyToCalculateAndStore = calculateAndStoreFromInputAndAsyncTermsObservableData.SigIsReadyToCalculateAndStore;
                    _calculateAndStoreFromInputAndAsyncTermsObservableData = calculateAndStoreFromInputAndAsyncTermsObservableData;
                    _webGet = webGet;
                    _calculateAndStoreFromInputAndAsyncTermsOptions = calculateAndStoreFromInputAndAsyncTermsOptions;

                    _buffer = new BufferBlock<InternalMessage<string>>();
                    RegisterChild(_buffer);
                    // critical section
                    // iterate each individual term of the sig, and if it not already present in _isFetchingIndividualTermKeyCOD
                    sig.IndividualTerms.Where(term => !IsFetchingIndividualTermKey.ContainsKey(term))
                        .ToList()
                        .ForEach(term => {
                            // call the async function that fetches the information for each individual term in the sig
                            // record the individual term and the task in the COD isFetchingIndividualTermKey passed as a parameter during the ctor
 IsFetchingIndividualTermKey[term] = webGet.GetHRAsync(term);
                            // attach the event handler onTaskStatusPropertyChanged to the TaskStatus property of every task being fetched, whether it was created here or it already existed in the COD
                            //  isFetchingIndividualTermKey[term].
 });
                }

                void onPropertyChanged(object sender, PropertyChangedEventArgs e) {
                // go through the list of async fetch tasks for all the individual terms that need to complete in order for this transientblock to release its output buffered messages
                // if all tasks are complete
                //         set sigIsReadyToCalculateAndStore for this sig.longest to true
                //  link the data flow for this block to the _terminator
                //  attach an event handler to teh buffer's count property such that when it reaches zero, the event handler sets this transientblock's status to completed
                // todo figure out how cancellation will work
                // ToDo figure out how faulting and exception handling will work
                // 
                //receivedEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
                }

                protected override void CleanUp(Exception e) {
                    base.CleanUp(e);
                // ToDo Cleanup any messages on the transient blocks
                // remove this TransientBlock's event Handlers from all Tasks representing term fetches, for the individual terms for this TBs sig
                }

                public override ITargetBlock<InternalMessage<string>> InputBlock { get { return this._buffer; } }

                public ConcurrentObservableDictionary<string, Task> IsFetchingIndividualTermKey { get => _isFetchingIndividualTermKey; set => _isFetchingIndividualTermKey =
                    value; }

                public override ISourceBlock<InternalMessage<string>> OutputBlock { get { return this._buffer; } }

                public ConcurrentObservableDictionary<string, byte> SigIsReadyToCalculateAndStore { get => _sigIsReadyToCalculateAndStore; set => _sigIsReadyToCalculateAndStore =
                    value; }

                public IWebGet WebGet { get => _webGet; set => _webGet = value; }
            }
        }
    }

    //JsonConvert.DeserializeObject<(string k1, string k2, string c1, double d)[]>
    public class InputStringAsJSON<TKeyTerm1>
    {
        /// <summary>
        /// The value backing field
        /// </summary>
        (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1) _value;

        public InputStringAsJSON((string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1) value)
        {
            _value = value;
        }

        public (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1) Value
        {
            get => _value;
        }
    }
    public class ParseSingleInputStringFormattedAsJSONToInputMessage : Dataflow<string, InputMessage<string>> {
        // Head and tail 
        TransformBlock<string, InputMessage<string>> _transformer;
        public ParseSingleInputStringFormattedAsJSONToInputMessage() : this(CalculateAndStoreFromInputAndAsyncTermsOptions.Default) { }
        public ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            // create the output via a TransformBlock
            _transformer = new TransformBlock<string, InputMessage<string>>(_input =>
            {
                (string k1, string k2, IReadOnlyDictionary<string, double> terms1) _temp;
                try
                {
                    _temp = JsonConvert.DeserializeObject<(string k1, string k2, Dictionary<string, double> terms1)>(_input);
                }
                catch
                {
                    throw new ArgumentException($"{_input} does not match the needed input pattern");
                }
                return new InputMessage<string>(_temp);
                //return _temp;
            }
            );

           RegisterChild(_transformer);

        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<InputMessage<string>> OutputBlock { get { return _transformer; } }
    }

    public class ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection : Dataflow<string, InputMessage<string>>
    {
        // Head and tail 
        TransformManyBlock<string, InputMessage<string>> _transformer;
        public ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection() : this(CalculateAndStoreFromInputAndAsyncTermsOptions.Default) { }

        public ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection(CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
            // create the output via a TransformManyBlock
            //_transformer = new TransformManyBlock<string, InputMessage<string>>(_input =>
            _transformer = new TransformManyBlock<string, InputMessage<string>>(_input =>
            {
                IEnumerable<InputMessage<string>> _coll;
                try
                {
                    _coll = JsonConvert.DeserializeObject<IEnumerable<InputMessage<string>>>(_input);
                }
                catch
                {
                    throw new ArgumentException($"{_input} does not match the needed input pattern");
                }

                 return _coll;
                // use the constructor that returns an IEnumerable><InputMessage<string>>
                //return new InputMessage<string>(_coll);
                //return _coll.ToList().ForEach(j=> new InputMessage<string>(j));
            }
            );

            RegisterChild(_transformer);

        }

        public override ITargetBlock<string> InputBlock { get { return _transformer; } }

        public override ISourceBlock<InputMessage<string>> OutputBlock { get { return _transformer; } }
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
                if (_sigIsReadyToCalculateAndStoreCOD.ContainsKey(_input.c1)) return _input;

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
                // await all of the tasks needed to put all of values for all the keys of terms1 into the TermCOD1
                // this will create a synchronizationContext at this point
                // when any individual task that fetches the value of term1 for any single key c finishes
                // processing resumes here
                // populate the Term1 COD for the single key c that just finished
                calculateAndStoreFromInputAndAsyncTermsObservableData.TermCOD1[_input.c1] =
                // update the _sigIsReadyToCalculateAndStoreCOD for the single key c that just finished
                _sigIsReadyToCalculateAndStoreCOD[_input.c1] = default;
                // does this the single key c that just finished complete the set of c's needed to dequeue a class of messages
                // if so, release all the messages in that queue to the output.
                // See if all the values of the keys in this message's HR dictionary are already in _isFetchingIndividualTermKeyCOD
                // and if not, create the async tasks that will populate them
                if (!_isFetchingIndividualTermKeyCOD.ContainsKey(_input.c1))
                {
                    //todo make this into something that returns an awaitable task
                    _isFetchingIndividualTermKeyCOD[_input.c1] = default;

                }
                // populate the Term1 COD for the keys c1..cn in the dictionary HR
                calculateAndStoreFromInputAndAsyncTermsObservableData.TermCOD1[_input.c1] = 10.0;
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

