using System;
using System.Threading.Tasks.Dataflow;
using System.Linq;
using System.Text.RegularExpressions;
using Gridsum.DataflowEx;
using DataObjects;
using Swordfish.NET.Collections;
using System.Threading.Tasks;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace DataFlowExPatterns
{
    public class RFromInputAnd1Term : Gridsum.DataflowEx.Dataflow<(string k1, string k2, string c1, double hr)>
    {
        // A thread-safe place to keep track of which values of t1 are ReadyToCalculate
        ConcurrentObservableDictionary<string, byte> t1AreReadyToCalculate = new ConcurrentObservableDictionary<string, byte>();
        // a thread-safe place to keep track of which values of t1 are FetchingTerm1
        ConcurrentObservableDictionary<string, Task> t1AreFetchingTerm1 = new ConcurrentObservableDictionary<string, Task>();

        static string SetsOfTerm1KeyFunctionDelimiter = ",";
        static string SetsOfTerm1KeyFunction(IEnumerable<string> _keys) {
            return _keys.Aggregate(new StringBuilder(), (current, next) => current.Append(SetsOfTerm1KeyFunctionDelimiter).Append(next)).ToString();
        }

        // Head
        private ITargetBlock<(string k1, string k2, string c1, double hr)> _headBlock;

        // WaitQueue
        private IPropagatorBlock<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate), (string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)> _placeholder;

        // Constructor
        public RFromInputAnd1Term(Rand1Term rand1Term, IWebGet webGet) : base(DataflowOptions.Default)
        {
            
            // create the individual results via a transform block
            // the output is k1, k2, c1, bool, and the output is routed on the bool value
            var _accepter = new TransformBlock<(string k1, string k2, string c1, double hr), (string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>(_input =>
            {
                // create a signature from the set of keys found in hRs
                // ToDo don't use a string, use a collection of keys
                string sig = SetsOfTerm1KeyFunction(new List<string> { _input.c1 });

                // Are all the keys of hRs in the cReadyToCalculate dictionary? set the output bool accordingly
                // ToDo don't use a string, use a collection of keys, and get back the collection of keys NOT in t1AreReadyToCalculate
                bool isReadyToCalculate = t1AreReadyToCalculate.ContainsKey(_input.c1);
                if (!isReadyToCalculate)
                {
                    // are all the keys NOT in t1AreReadyToCalculate present in the t1AreFetchingTerm1 dictionary?
                    // if so, just send the message, it will be routed to the buffer
                    // if not, start an async tasks to Fetch each individual key that doesn't have an entry in the t1AreFetchingTerm1 dictionary
                    // ToDo don't use a string, use a collection of keys, and get back the collection of keys NOT in t1AreFetchingTerm1
                    if (!t1AreFetchingTerm1.ContainsKey(_input.c1))
                    {
                        // iterate the collection of keys NOT in t1AreFetchingTerm1
                        //foreach (var c in hRs.Keys){
                        if (!t1AreFetchingTerm1.ContainsKey(_input.c1))
                        {
                            // start an asynchronous WebGet Fetch to fetch the Term1 data for c, and store the task returned
                            t1AreFetchingTerm1[_input.c1] = webGet.GetHRAsync(_input.c1);
                        }
                        //}
                    }
                }
                return (_input.k1, _input.k2, _input.c1, _input.hr, sig, isReadyToCalculate);
            }).ToDataflow();

            // The terminal block performs both the ComputeAndStore operations
            var _terminator = new ActionBlock<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>(_input =>
            {
                // do the calculation based on hr and on Term1
                decimal pr = Convert.ToDecimal(_input.hr / rand1Term.term1COD[_input.c1]);
                rand1Term.RecordR(_input.k1, _input.k2, pr);
            }).ToDataflow();

            // a collection of DataFlowEx "buffers" to hold specific classes of messages
            ConcurrentDictionary<string, Dataflow<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>> _buffers = new ConcurrentDictionary<string, Dataflow<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>>();
            // this block accepts messages where isReadyToCalculate is false, and buffers them
            var _waitQueue = new TransformBlock<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate), (string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>(_input =>
            {
                // if all the keys in this messages hRs dictionary are in the keys of AreReadyToCalculate, just forward the message
                // Put the test here again at the top of the block in case the termn was populated between the time the message left the _accepter and got sent to the _waitQueue
                // ToDo don't use a string, use a collection of keys
                if (t1AreReadyToCalculate.ContainsKey(_input.c1)) return _input;

                // does a dataflowEX exist for the class of messages having this set of keys in its hRs dictionary?
                if (_buffers.ContainsKey(_input.sig))
                {
                    // if so, put the message on that dataflowEX (the LinkTo Predicate will ensure the right dataflowEx gets this message)
                    return _input;
                } else
                {
                    // if not, create a dataflowEX for this class of messages and put the message into it.
                    _buffers[_input.sig] = new BufferBlock<(string k1, string k2, string c1, double hr, string sig, bool isReadyToCalculate)>().ToDataflow();
                    _buffers[_input.sig].Name = _input.sig;
                    this._placeholder.LinkTo(_buffers[_input.sig]. , @out => @out.sig== _input.sig);
                    _terminator.RegisterDependency(_buffers[_input.sig]);

                }
                //
                // await all of the tasks needed to put all of values for all the keys of hRs into the term1COD
                // this will create a synchronizationContext at this point
                // when any individual task that fetches the value of term1 for any single key c finishes
                // processing resumes here
                // populate the Term1 COD for the single key c that just finished
                rand1Term.term1COD[_input.c1] =
                // update the t1AreReadyToCalculate for the single key c that just finished
                t1AreReadyToCalculate[_input.c1] = default;
                // does this the single key c that just finished complete the set of c's needed to dequeue a class of messages
                // if so, release all the messages in that queue to the output.
                // See if all the values of the keys in this message's HR dictionary are already in t1AreFetchingTerm1
                // and if not, create the async tasks that will populate them
                if (!t1AreFetchingTerm1.ContainsKey(_input.c1))
                {
                    //todo make this into something that returns an awaitable task
                    t1AreFetchingTerm1[_input.c1] = default;

                }
                // populate the Term1 COD for the keys c1..cn in the dictionary HR
                rand1Term.term1COD[_input.c1] = 10.0;
                return _input;
            }).ToDataflow();

            _accepter.Name = "_accepter";
            _terminator.Name = "_terminator";
            _waitQueue.Name = "_waitQueue";

            // Link dataflow 
            // Link _accepter to _terminator when the message has isReadyToCalculate = true
            _accepter.LinkTo(_terminator, mc => mc.isReadyToCalculate);
            // Link _accepter to _waitQueue when the message has isReadyToCalculate = false
            _accepter.LinkTo(_waitQueue, mc => !mc.isReadyToCalculate);

            // Link _waitQueue to _terminator. messages will be sent when the conditions needed to calculate it are ready
            _waitQueue.LinkTo(_terminator);

            // Link completion
            _waitQueue.RegisterDependency(_accepter);
            _terminator.RegisterDependency(_accepter);
            _terminator.RegisterDependency(_waitQueue);

            this.RegisterChild(_accepter);
            this.RegisterChild(_terminator);
            this.RegisterChild(_waitQueue);

            this._headBlock = _accepter.InputBlock;
            this._placeholder = _waitQueue.InputBlock;

            // ToDo: start PreparingToCalculate to prepoulate the initial list of C key signatures
        }

        public override ITargetBlock<(string k1, string k2, string c1, double hr)> InputBlock { get { return this._headBlock; } }

    }

    public class ParseStringToTuple : Gridsum.DataflowEx.Dataflow<string, (string k1, string k2, string c1, double hr)>
    {
        // declare this RegEx outside the transform network so it only will be compiled once
        Regex REinner = new Regex("(?<k1>.*?),(?<k2>.*?),(?<c1>.*?),(?<hr>.*?);");

        // Head and tail 
        private ITargetBlock<string> _headBlock;
        private ISourceBlock<(string k1, string k2, string c1, double hr)> _tailBlock;

        public ParseStringToTuple() : base(DataflowOptions.Default)
        {
            // create the individual results via a transform block
            // the output is k1, k2, c1
            var _transformer = new TransformBlock<string, (string k1, string k2, string c1, double hr)>(_input =>
            {
                var match = REinner.Match(_input);
                if (match.Success)
                {
                    return (match.Groups["k1"].Value,
                                match.Groups["k2"].Value, match.Groups["c1"].Value,
                                double.Parse(match.Groups["hr"].Value));
                }
                throw new ArgumentException($"{_input} does not match the needed input pattern");
            }).ToDataflow();

            _transformer.Name = "_accepter";

            this.RegisterChild(_transformer);
            this._headBlock = _transformer.InputBlock;
            this._tailBlock = _transformer.OutputBlock;

        }
        public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        public override ISourceBlock<(string k1, string k2, string c1, double hr)> OutputBlock { get { return this._tailBlock; } }

    }


}
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

