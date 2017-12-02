using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms;
using Gridsum.DataflowEx;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Common.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DatFlowExPatternsUnitTests {
    public static class Utils { 
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ILog GetNamespaceLogger()
    {
        var frame = new StackFrame(1);
        var callingMethod = frame.GetMethod();
        return LogManager.GetLogger(callingMethod.DeclaringType.Namespace);
    }
}
    public class Fixture : IDisposable {
        #region MOQs
        // a MOQ for the async web calls used for Term1
        public IWebGet mockTerm1;
        #endregion
        #region Logger
        public ILog m_logger = Utils.GetNamespaceLogger();
        #endregion
        public void Dispose() {
        }

        public TestDataAnalysisResults TestDataAnalysisResultsFromJSONInput(string _input) {
            HashSet<string> uK1 = new HashSet<string>();
            HashSet<string> uK1K2Pair = new HashSet<string>();
            JsonConvert.DeserializeObject<(string k1, string k2, string c1, double d)[]>(_input)
                .ToList()
                .ForEach(x => {
                    // One nice thing about HashSets, they won't complain if you try to add a duplicate, so this ends up being the unique values from the _input
 uK1.Add(x.k1);
                    uK1K2Pair.Add(x.k1 + x.k2); });
            return new TestDataAnalysisResults((uK1, uK1K2Pair));
        }

        // common method and the methods results type, used to count test theory data input
        public class TestDataAnalysisResults {
            (HashSet<string> UK1, HashSet<string> UK1K2Pair) _value;

            public TestDataAnalysisResults((HashSet<string> UK1, HashSet<string> UK1K2Pair) value) {
                Value = value;
            }

            public (HashSet<string> UK1, HashSet<string> UK1K2Pair) Value { get => _value; set => _value =
                value; }
        }

        #region dictionaries that hold the information written by event handlers
        /// <summary>
        /// The is the dictionary that holds the information written by the event handlers that are reporting changes to the ResultsCOD
        /// During testing, this "stands in" for a GUI visual control that would normally receive these events
        /// </summary>
        public ConcurrentDictionary<string, string> IsFetchingIndividualTermKeyCODEvents = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> resultsCODEvents = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> SigIsReadyToCalculateAndStoreCODEvents = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> term1CODEvents = new ConcurrentDictionary<string, string>();
        #endregion
        #region Event handlers for the CODs found in the CalculateAndStoreFromInputAndAsyncTermsObservableData class
        /// <summary>
        /// a message formatter that lays out the information written by event handlers.
        /// </summary>
        /// <param name="CODName">The name of teh ConcurrentObservableDictionary on which the event happened.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        /// <returns>System.String.</returns>
        public string Message(string CODName, NotifyCollectionChangedEventArgs e) {
            string s = $"Ticks: {DateTime.Now.Ticks} Event: Notify{CODName}CollectionChanged  Action: {e.Action}  ";
            switch(e.Action) {
                case NotifyCollectionChangedAction.Add:
                    s += $"NumItemsToAdd { e.NewItems.Count}";
                    break;
                case NotifyCollectionChangedAction.Move:
                    s += $"Move Collection Changed event recording: Details not implemented";
                    break;
                case NotifyCollectionChangedAction.Remove:
                    s += $"NumItemsToDel {e.OldItems.Count}";
                    break;
                case NotifyCollectionChangedAction.Replace:
                    s += $"Replace Collection Changed event recording: Details not implemented";
                    break;
                case NotifyCollectionChangedAction.Reset:
                    s += $"Reset Collection Changed event recording: Details not implemented";
                    break;
                default:
                    break;
            }
            return s;
        }

        public void onIsFetchingIndividualTermKeyCODCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            IsFetchingIndividualTermKeyCODEvents[Message("IsFetchingIndividualTermKey",
                                                         e)] = DateTime.Now.ToLongTimeString();
        }

        public void onNestedPropertyChanged(object sender, PropertyChangedEventArgs e) {
            resultsCODEvents[$"Ticks: {DateTime.Now.Ticks} Event: NestedPropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onPropertyChanged(object sender, PropertyChangedEventArgs e) {
            resultsCODEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        // These event handler will be attached/detached from the ObservableConcurrentDictionary via that class' constructor and dispose method
        public void onResultsCODCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            resultsCODEvents[Message("Outer", e)] = DateTime.Now.ToLongTimeString();
        }

        //These event handlers will be attached to each innerDictionary
        public void onResultsNestedCODPropertyChanged(object sender, NotifyCollectionChangedEventArgs e) {
            resultsCODEvents[Message("Nested", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onSigIsReadyToCalculateAndStoreCODCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SigIsReadyToCalculateAndStoreCODEvents[Message("SigIsReadyToCalculateAndStore",
                                                           e)] = DateTime.Now.ToLongTimeString();
        }

        public void onTerm1PropertyChanged(object sender, PropertyChangedEventArgs e) {
            term1CODEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        // These event handler will be attached/detached from the Term1Dictionary via that class' constructor and dispose method
        public void onTermCOD1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            term1CODEvents[Message("Term1", e)] = DateTime.Now.ToLongTimeString();
        }
        #endregion  
    }

    public class CalculateAndStoreFromInputAndAsyncTermsTestsBasic : IClassFixture<Fixture> {
        Fixture _fixture;
        readonly ITestOutputHelper output;

        //private static Logger _logger = LogManager.GetLogger("CalculateAndStoreFromInputAndAsyncTermsTestsBasic");
        /// <summary>
        /// ctor. Initializes a new instance of the <see cref="CalculateAndStoreFromInputAndAsyncTermsTestsBasic" /> class.
        /// 
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="fixture">The fixture.</param>
        public CalculateAndStoreFromInputAndAsyncTermsTestsBasic(ITestOutputHelper output, Fixture fixture) {
            this.output = output;
            this._fixture = fixture;
        }

        #region
        [Theory]
        [InlineData("{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}")]
        public async void CalculateAndStoreSingleInputStringFormattedAsJSONToObservableDataTest(string inTestData) {
            // arrange
            _fixture.m_logger.Debug("starting test");
            // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
            _fixture.resultsCODEvents.Clear();
            // since the term1CODEvents list in the fixture is shared between tests, the list needs to be cleared
            _fixture.term1CODEvents.Clear();
            // Create a Mock for the WebGet service
            var mockTerm1 = new Mock<IWebGet>();
            mockTerm1
            .Setup(webGet => webGet.AsyncWebGet<double>("A"))
                .Callback(() => Task.Delay(new TimeSpan(0, 1, 0)))
                .ReturnsAsync(100.0);

            IInputMessage<string, double> result = new InputMessage<string, double>(("init", "init", new Dictionary<string, double>()));
            var action = new Action<IInputMessage<string, double>>(im => result =
                im);
            using (CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData = 
                new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsCODCollectionChanged,
                                                                    _fixture.onResultsNestedCODPropertyChanged,
                                                                    _fixture.onTermCOD1CollectionChanged,
                                                                    _fixture.onSigIsReadyToCalculateAndStoreCODCollectionChanged,
                                                                    _fixture.onIsFetchingIndividualTermKeyCODCollectionChanged))
            {
                var calculateAndStoreSingleInputStringFormattedAsJSONToObservableData = new CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData(calculateAndStoreFromInputAndAsyncTermsObservableData, mockTerm1.Object, CalculateAndStoreFromInputAndAsyncTermsOptions.Default);

                // act
                var sendAsyncResults = calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.InputBlock.SendAsync(inTestData);
                await sendAsyncResults;
                // inform the head of the network that there is no more data
                calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.InputBlock.Complete();
                // wait for the network to indicate completion
                await calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.CompletionTask;
            }
            // assert
            // send the observed events to test output
            _fixture.resultsCODEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {_fixture.resultsCODEvents[x]}"));

            // Count the number of inner and outer CollectionChanged events that occurred
            var numInnerNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
                                                      .ToList()
                                                      .Count;
            var numOuterNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
                                                      .ToList()
                                                      .Count;

            Assert.NotNull(result);
        }

        /// <summary>
        /// Class CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData.
        /// </summary>
        /// <seealso cref="Gridsum.DataflowEx.Dataflow{System.String}" />
        public class CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData : Dataflow<string> {
            ITargetBlock<string> _headBlock;

            public CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) :base(calculateAndStoreFromInputAndAsyncTermsOptions){

                var _accepterJSON = new ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions.Verbose);
                var _terminator = new CalculateAndStoreFromInputAndAsyncTerms(calculateAndStoreFromInputAndAsyncTermsObservableData, webGet, calculateAndStoreFromInputAndAsyncTermsOptions);
                _accepterJSON.Name = "_accepterJSON";
                _terminator.Name = "_terminator";
                this.RegisterChild(_accepterJSON);
                this.RegisterChild(_terminator);
                _accepterJSON.LinkTo(_terminator);
                _terminator.RegisterDependency(_accepterJSON);
                this._headBlock = _accepterJSON.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }
        #endregion
        #region
        public class ParseSingleInputStringFormattedAsJSONCollectionToAction : Dataflow<string> {
            ITargetBlock<string> _headBlock;

            public ParseSingleInputStringFormattedAsJSONCollectionToAction(Action<InputMessage<string, double>> action) : base(CalculateAndStoreFromInputAndAsyncTermsOptions.Default) {
                var _accepterJSON = new ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection(CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                var _terminator = DataflowUtils.FromDelegate<InputMessage<string, double>>(action);
                _accepterJSON.Name = "_accepterJSON";
                _terminator.Name = "_terminator";
                this.RegisterChild(_accepterJSON);
                this.RegisterChild(_terminator);
                _accepterJSON.LinkTo(_terminator);
                _terminator.RegisterDependency(_accepterJSON);
                this._headBlock = _accepterJSON.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }

        [Theory]
        [InlineData("[{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}},{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}},{\"Item1\":\"k1\",\"Item2\":\"k3\",\"Item3\":{\"C\":13.0}},{\"Item1\":\"k1\",\"Item2\":\"k4\",\"Item3\":{\"D\":14.0}},{\"Item1\":\"k1\",\"Item2\":\"k5\",\"Item3\":{\"A\":15.0,\"B\":15.1,\"C\":15.2,\"D\":15.3}},{\"Item1\":\"k2\",\"Item2\":\"k2\",\"Item3\":{\"A\":22.0,\"B\":22.1}},{\"Item1\":\"k2\",\"Item2\":\"k3\",\"Item3\":{\"A\":23.0,\"E\":22.4}}]")]
        public async void ParseSingleInputStringFormattedAsJSONCollectionToInputMessageTest(string inTestData) {
            // arrange
            List<InputMessage<string, double>> result = new List<InputMessage<string, double>>();
            var action = new Action<InputMessage<string, double>>(im => result.Add(im));
            var parseSingleInputStringFormattedAsJSONCollectionToAction = new ParseSingleInputStringFormattedAsJSONCollectionToAction(action);

            // act
            var sendAsyncResults = parseSingleInputStringFormattedAsJSONCollectionToAction.InputBlock.SendAsync(inTestData);
            await sendAsyncResults;
            // inform the head of the network that there is no more data
            parseSingleInputStringFormattedAsJSONCollectionToAction.InputBlock.Complete();
            // wait for the network to indicate completion
            await parseSingleInputStringFormattedAsJSONCollectionToAction.CompletionTask;

            // assert
            Assert.NotNull(result);
        }
        #endregion
        #region
        // Ensure that the dataflow ParseSingleInputStringFormattedAsJSONToInputMessage will take in a string and put out an InputMessage
        public class ParseSingleInputStringFormattedAsJSONToAction : Dataflow<string> {
            ITargetBlock<string> _headBlock;
            public ParseSingleInputStringFormattedAsJSONToAction(Action<IInputMessage<string, double>> action) : this(action, CalculateAndStoreFromInputAndAsyncTermsOptions.Default)
            {
            }
                public ParseSingleInputStringFormattedAsJSONToAction(Action<IInputMessage<string, double>> action, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
                var _accepterJSON = new ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                var _terminator = DataflowUtils.FromDelegate<IInputMessage<string, double>>(action);
                _accepterJSON.Name = "_accepterJSON";
                _terminator.Name = "_terminator";
                this.RegisterChild(_accepterJSON);
                this.RegisterChild(_terminator);
                _accepterJSON.LinkTo(_terminator);
                _terminator.RegisterDependency(_accepterJSON);
                this._headBlock = _accepterJSON.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }

        [Theory]
        [InlineData("{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}")]
        public async void ParseSingleInputStringFormattedAsJSONToInputMessageTest(string inTestData) {
            // arrange
            // arrange
            //_fixture.m_logger.Debug("starting test");
            IInputMessage<string, double> result = new InputMessage<string, double>(("init", "init", new Dictionary<string, double>()));
            var action = new Action<IInputMessage<string, double>>(im => result =
                im);
            var parseSingleInputStringFormattedAsJSONToAction = new ParseSingleInputStringFormattedAsJSONToAction(action, CalculateAndStoreFromInputAndAsyncTermsOptions.Verbose);

            // act
            var sendAsyncResults = parseSingleInputStringFormattedAsJSONToAction.InputBlock.SendAsync(inTestData);
            await sendAsyncResults;
            // inform the head of the network that there is no more data
            parseSingleInputStringFormattedAsJSONToAction.InputBlock.Complete();
            // wait for the network to indicate completion
            await parseSingleInputStringFormattedAsJSONToAction.CompletionTask;

            // assert
            Assert.NotNull(result);
        }
        #endregion
        /*
        // sendAsyncResults has returned
        switch (sendAsyncResults.Status)
        {
        case TaskStatus.Canceled:
        break;
        case TaskStatus.Faulted:
        break;
        case TaskStatus.RanToCompletion:
        break;
        default:
        throw new Exception("ToDo make this better exception handling");
        }

        */
    /*
    [Theory]
    [InlineData("{\"k1\",\"k2\",{\"c1\":10.0,\"c2\":20.0}}")]
    //[InlineData("k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=1,1.21;")]
    //[InlineData("k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=2,1.21;")]
    //[InlineData("k1=2,k2=2,c1=1,2.21;k1=2,k2=1,c1=1,2.11;k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=1,1.21;")]
    //[InlineData("k1=2,k2=2,c1=1,2.21;k1=2,k2=1,c1=2,2.11;k1=1,k2=1,c1=1,1.11;k1=1,k2=2,c1=2,1.21;")]
    public async void ResultsUsingTerm1(string testInStr) {
    //arrange
    // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
    _fixture.resultsCODEvents.Clear();
    // since the term1CODEvents list in the fixture is shared between tests, the list needs to be cleared
    _fixture.term1CODEvents.Clear();
    // Create a Mock for the WebGet service
    var mockTerm1 = new Mock<IWebGet>();
    mockTerm1
    .Setup(webGet => webGet.GetHRAsync("c1"))
    .Callback(() => Task.Delay(new TimeSpan(0, 1, 0)))
    .ReturnsAsync(100.0);
    
    //act
    // log start
    // _logger.Debug("Logging");
    // Create the Observable data structures and their event handlers
    using(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsCODCollectionChanged,
    _fixture.onResultsNestedCODPropertyChanged,
    _fixture.onTermCOD1CollectionChanged,
    _fixture.onSigIsReadyToCalculateAndStoreCODCollectionChanged,
    _fixture.onIsFetchingIndividualTermKeyCODCollectionChanged)) {
    // Create a new DataFlowEx network that combines the ParseInputStringFormattedAsJSONToInputMessage and CalculateAndStoreFromInputAndAsyncTerms networks
    var parseStringToTupleThenResultsFromInputAnd1Term = new ParseStringToTupleThenResultsFromInputAnd1Term(calculateAndStoreFromInputAndAsyncTermsObservableData,
    mockTerm1.Object,
    CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
    
    // Split the testInStr string on the ;, and send each substring into the head of the pipeline
    var REouter = new Regex("(?<oneTuple>.*?;)");
    var matchOuter = REouter.Match(testInStr);
    while(matchOuter.Success) {
    // SendAsync returns a task
    var r = parseStringToTupleThenResultsFromInputAnd1Term.InputBlock.SendAsync(matchOuter.Groups["oneTuple"].Value);
    // ToDo wrap this in a try catch and handle any aggregate exceptions
    await r;
    // r has returned
    switch(r.Status) {
    case TaskStatus.Canceled:
    break;
    case TaskStatus.Faulted:
    break;
    case TaskStatus.RanToCompletion:
    break;
    default:
    throw new Exception("ToDo make this better exception handling");
    }
    
    matchOuter = matchOuter.NextMatch();
    }
    
    // inform the head of the DataFlowEX network that there is no more data
    parseStringToTupleThenResultsFromInputAnd1Term.InputBlock.Complete();
    
    // wait for the DataFlowEX network to indicate completion
    await parseStringToTupleThenResultsFromInputAnd1Term.CompletionTask;
    } // the COD will be disposed of at this point
    
    //Ensure COD events have a chance to propagate
    await Task.Delay(100);
    
    // send the observed events to test output
    _fixture.resultsCODEvents.Keys.OrderBy(x => x)
    .ToList()
    .ForEach(x => output.WriteLine($"{x} : {_fixture.resultsCODEvents[x]}"));
    
    // Count the number of inner and outer CollectionChanged events that occurred
    var numInnerNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
    .ToList()
    .Count;
    var numOuterNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
    .ToList()
    .Count;
    // find the number of unique values of K1 and the number of k1k2 pairs in the test's input data
    // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
    // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
    var matchUniqueK1Values = new Regex("(?<k1>.*?),(?<k2>.*?),(?<c1>.*?),.*?;").Match(testInStr);
    var uniqueK1Values = new HashSet<string>();
    var uniqueK1K2PairValues = new HashSet<string>();
    while(matchUniqueK1Values.Success) {
    // One nice thing about HashSets, they won't complain if you try to add a duplicate
    uniqueK1Values.Add(matchUniqueK1Values.Groups["k1"].Value);
    uniqueK1K2PairValues.Add(matchUniqueK1Values.Groups["k1"].Value +
    matchUniqueK1Values.Groups["k2"].Value);
    
    matchUniqueK1Values = matchUniqueK1Values.NextMatch();
    }
    // number of unique values of K1 in the test's input data
    var numUniqueK1Values = uniqueK1Values.Count;
    // number of unique values of K1K2 pairs in the test's input data
    var numUniqueK1K2PairValues = uniqueK1K2PairValues.Count;
    // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
    Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
    // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
    Assert.Equal(numUniqueK1K2PairValues,
    numInnerNotifyCollectionChanged);
    // since the fixture is shared between test, the fixture needs to be cleared
    _fixture.resultsCODEvents.Clear();
    // since the term1CODEvents list in the fixture is shared between test, the list needs to be cleared
    _fixture.term1CODEvents.Clear();
    }
    
    [Theory]
    [InlineData("[{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}},{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}},{\"Item1\":\"k1\",\"Item2\":\"k3\",\"Item3\":{\"C\":13.0}},{\"Item1\":\"k1\",\"Item2\":\"k4\",\"Item3\":{\"D\":14.0}},{\"Item1\":\"k1\",\"Item2\":\"k5\",\"Item3\":{\"A\":15.0,\"B\":15.1,\"C\":15.2,\"D\":15.3}},{\"Item1\":\"k2\",\"Item2\":\"k2\",\"Item3\":{\"A\":22.0,\"B\":22.1}},{\"Item1\":\"k2\",\"Item2\":\"k3\",\"Item3\":{\"A\":23.0,\"E\":22.4}}]")]
    public async void RFromJSONInputAnd1TermTest1(string testInStr) {
    //arrange
    // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
    _fixture.resultsCODEvents.Clear();
    // since the term1CODEvents list in the fixture is shared between tests, the list needs to be cleared
    _fixture.term1CODEvents.Clear();
    
    // Create a Mock for the WebGet service
    var mockTerm1 = new Mock<IWebGet>();
    mockTerm1
    .Setup(webGet => webGet.GetHRAsync("2"))
    .Callback(() => Task.Delay(new TimeSpan(0, 1, 0)))
    .ReturnsAsync(100.0);
    // log start
    // _logger.Debug("Logging");
    
    // act
    // Create the Observable data structures and their event handlers
    using(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsCODCollectionChanged,
    _fixture.onResultsNestedCODPropertyChanged,
    _fixture.onTermCOD1CollectionChanged,
    _fixture.onSigIsReadyToCalculateAndStoreCODCollectionChanged,
    _fixture.onIsFetchingIndividualTermKeyCODCollectionChanged)) {
    CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions();
    // Create the new DataFlowEx network 
    var rFromJSONInputAnd1Term = new ParseJSONStringCollectionToInputMessage(calculateAndStoreFromInputAndAsyncTermsObservableData,
    mockTerm1.Object,
    calculateAndStoreFromInputAndAsyncTermsOptions);
    // Send the test data to the network
    var task = rFromJSONInputAnd1Term.InputBlock.SendAsync(testInStr);
    
    // inform the head of the DataFlowEX network that there is no more data
    rFromJSONInputAnd1Term.InputBlock.Complete();
    
    // wait for the DataFlowEX network to indicate completion
    await rFromJSONInputAnd1Term.CompletionTask;
    // ToDo ensure the network completed without fault
    } // All of the data structures needed by the network should be disposed at this point
    
    //Ensure COD events have a chance to propagate
    await Task.Delay(100);
    
    // assert
    // Count the number of inner and outer CollectionChanged events that occurred
    var numInnerNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyNestedCollectionChanged"))
    .ToList()
    .Count;
    var numOuterNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyOuterCollectionChanged"))
    .ToList()
    .Count;
    
    // find the number of unique values of K1 and the number of k1k2 pairs in the test's input data
    var r = _fixture.TestDataAnalysisResultsFromJSONInput(testInStr);
    // number of unique values of K1 in the test's input data
    var numUniqueK1Values = r.Value.UK1.Count;
    // number of unique values of K1K2 pairs in the test's input data
    var numUniqueK1K2PairValues = r.Value.UK1K2Pair.Count;
    
    // There should be as many outer NotifyCollectionChanged events are there are unique values of K1 in the input data.
    // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
    Assert.Equal(numUniqueK1Values, numOuterNotifyCollectionChanged);
    // There should be as many inner NotifyCollectionChanged events are there are unique values of K1K2 pairs in the input data.
    Assert.Equal(numUniqueK1K2PairValues,
    numInnerNotifyCollectionChanged);
    
    // Cleanup
    // since the fixture is shared between test, the fixture needs to be cleared
    _fixture.resultsCODEvents.Clear();
    // since the term1CODEvents list in the fixture is shared between test, the list needs to be cleared
    _fixture.term1CODEvents.Clear();
    }
    
    
    class ParseStringToTupleThenResultsFromInputAnd1Term : Dataflow<string> {
    // Head
    ITargetBlock<string> _headBlock;
    
    // Constructor
    public ParseStringToTupleThenResultsFromInputAnd1Term(CalculateAndStoreFromInputAndAsyncTermsObservableData calculateAndStoreFromInputAndAsyncTermsObservableData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
    // Create the DataFlowEx network that accepts a long string (test data) and breaks it into individual inputs for the following network
    var _accepter = new ParseInputStringFormattedAsJSONToInputMessage();
    // Create the DataFlowEx network that calculates a Results COD from a formula and a term
    // The instance calculateAndStoreFromInputAndAsyncTermsObservableData supplies the Results COD and the term1 COD
    CalculateAndStoreFromInputAndAsyncTerms _terminator = new CalculateAndStoreFromInputAndAsyncTerms(calculateAndStoreFromInputAndAsyncTermsObservableData,
    webGet,
    calculateAndStoreFromInputAndAsyncTermsOptions);
    
    _accepter.Name = "_accepter";
    _terminator.Name = "_terminator";
    
    // Link Dataflow 
    // Link _accepter to _terminator when the message has isReadyToCalculate = true
    _accepter.LinkTo(_terminator);
    
    // Link completion
    _terminator.RegisterDependency(_accepter);
    
    this.RegisterChild(_accepter);
    this.RegisterChild(_terminator);
    
    this._headBlock = _accepter.InputBlock;
    }
    
    public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
    }
    */    }
}