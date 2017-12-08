using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms;
using FluentAssertions;
using Gridsum.DataflowEx;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace DatFlowExPatternsUnitTests {
    public class Fixture : IDisposable {
        #region MOQs
        // a MOQ for the async web calls used for Term1
        public Mock<IWebGet> mockTerm1;
        #endregion

        public ISSDataConcrete iSSData;

        public Fixture() {
            mockTerm1 = new Mock<IWebGet>();
            mockTerm1.Setup(webGet => webGet.AsyncWebGet<double>("A"))
                .Callback(() => Task.Delay(new TimeSpan(0, 0, 1)))
                .ReturnsAsync(100.0);
            mockTerm1.Setup(webGet => webGet.AsyncWebGet<double>("B"))
                .Callback(() => Task.Delay(new TimeSpan(0, 0, 1)))
                .ReturnsAsync(200.0);
            mockTerm1.Setup(webGet => webGet.AsyncWebGet<double>("C"))
                .Callback(() => Task.Delay(new TimeSpan(0, 0, 1)))
                .ReturnsAsync(300.0);
            mockTerm1.Setup(webGet => webGet.AsyncWebGet<double>("D"))
                .Callback(() => Task.Delay(new TimeSpan(0, 0, 1)))
                .ReturnsAsync(400.0);
            mockTerm1.Setup(webGet => webGet.AsyncWebGet<double>("E"))
                .Callback(() => Task.Delay(new TimeSpan(0, 0, 1)))
                .ReturnsAsync(50.0);
            ISSDataConcrete iSSData = new ISSDataConcrete(onResultsLevel0CODCollectionChanged,
                                                                                     onResultsLevel1CODCollectionChanged,
                                                                                     onFetchedIndividualElementsOfTerm1CollectionChanged,
                                                                                     onSigIsReadyTerm1CollectionChanged,
                                                                                     onFetchingIndividualElementsOfTerm1CollectionChanged,
                                                                                     onFetchingElementSetsOfTerm1CollectionChanged);
        }

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
        public ConcurrentDictionary<string, string> fetchingIndividualElementsOfTerm1Events = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> fetchingElementSetsOfTerm1Events = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> resultsCODEvents = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> elementSetsOfTerm1ReadyEvents = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> fetchedIndividualElementsOfTerm1Events = new ConcurrentDictionary<string, string>();
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

        #region CollectionChanged Event Handlers
        public void onFetchedIndividualElementsOfTerm1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            fetchedIndividualElementsOfTerm1Events[Message("Term1", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onFetchingIndividualElementsOfTerm1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            fetchingIndividualElementsOfTerm1Events[Message("FetchingIndividualElementsOfTerm1",
                                                            e)] = DateTime.Now.ToLongTimeString();
        }

        public void onFetchingElementSetsOfTerm1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            fetchingElementSetsOfTerm1Events[Message("IsFetchingSigOfTerm1",
                                                     e)] = DateTime.Now.ToLongTimeString();
        }

        public void onResultsLevel0CODCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            resultsCODEvents[Message("Level0", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onResultsLevel1CODCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            resultsCODEvents[Message("Level1", e)] = DateTime.Now.ToLongTimeString();
        }

        public void onSigIsReadyTerm1CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            elementSetsOfTerm1ReadyEvents[Message("SigIsReadyTerm1",
                                                  e)] = DateTime.Now.ToLongTimeString();
        }
        #endregion CollectionChanged Event Handlers
        #region PropertyChanged Event Handlers
        //These event handlers will be attached to each innerDictionary

        public void onTerm1PropertyChanged(object sender, PropertyChangedEventArgs e) {
            fetchedIndividualElementsOfTerm1Events[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onFetchingElementSetsOfTerm1PropertyChanged(object sender, PropertyChangedEventArgs e) {
            fetchingElementSetsOfTerm1Events[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onFetchingIndividualElementsOfTerm1PropertyChanged(object sender, PropertyChangedEventArgs e) {
            fetchingIndividualElementsOfTerm1Events[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onSigIsReadyTerm1PropertyChanged(object sender, PropertyChangedEventArgs e) {
            elementSetsOfTerm1ReadyEvents[$"Ticks: {DateTime.Now.Ticks} Event: PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onResultsLevel0CODPropertyChanged(object sender, PropertyChangedEventArgs e) {
            resultsCODEvents[$"Ticks: {DateTime.Now.Ticks} Event: Level0PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }

        public void onResultsLevel1CODPropertyChanged(object sender, PropertyChangedEventArgs e) {
            resultsCODEvents[$"Ticks: {DateTime.Now.Ticks} Event: Level1PropertyChanged  PropertyName {e.PropertyName}"] = DateTime.Now.ToLongTimeString();
        }
        #endregion PropertyChanged Event Handlers
        #endregion Event handlers for the CODs found in the CalculateAndStoreFromInputAndAsyncTermsObservableData class
        #region Concrete instance of CalculateAndStoreFromInputAndAsyncTermsObservableData
        public class ISSDataConcrete : CalculateAndStoreFromInputAndAsyncTermsObservableData<decimal>
        {
            public ISSDataConcrete()
            : base()
            {
            }

            public ISSDataConcrete(NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, NotifyCollectionChangedEventHandler onFetchedIndividualElementsOfTerm1CollectionChanged, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, NotifyCollectionChangedEventHandler onFetchingIndividualElementsOfTerm1CollectionChanged, NotifyCollectionChangedEventHandler onFetchingElementSetsOfTerm1CollectionChanged)
            : base(onResultsCODCollectionChanged,
                   onResultsNestedCODCollectionChanged,
                   onFetchedIndividualElementsOfTerm1CollectionChanged,
                   onSigIsReadyTerm1CollectionChanged,
                   onFetchingIndividualElementsOfTerm1CollectionChanged,
                   onFetchingElementSetsOfTerm1CollectionChanged)
            {
            }

            #region IDisposable Support
            // see https://stackoverflow.com/questions/538060/proper-use-of-the-idisposable-interface
            public new void TearDown()
            {
            }

            bool disposedValue = false; // To detect redundant calls

            protected override void Dispose(bool iAmBeingCalledFromDisposeAndNotFinalize)
            {
                if (!disposedValue)
                {
                    if (iAmBeingCalledFromDisposeAndNotFinalize)
                    {
                        // dispose managed state (managed objects).
                        TearDown();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // Not Needed
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~CalculateAndStoreFromInputAndAsyncTermsOptionsData() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }
            // This code added to correctly implement the disposable pattern.
            public new void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                try
                {
                    Dispose(true);
                    // TODO: uncomment the following line if the finalizer is overridden above.
                    // GC.SuppressFinalize(this);
                }
                finally { base.Dispose(); }
            }
            #endregion
        }
        #endregion Concrete instance of CalculateAndStoreFromInputAndAsyncTermsObservableData
        //public ISSDataConcrete ISSData { get; set; }
    }

    public class CalculateAndStoreFromInputAndAsyncTermsTestsBasic : IClassFixture<Fixture> {
        Fixture _fixture;
        readonly ITestOutputHelper output;

        #region Test Class Constructor
        /// <summary>
        /// ctor. Initializes a new instance of the <see cref="CalculateAndStoreFromInputAndAsyncTermsTestsBasic" /> class.
        /// 
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="fixture">The fixture.</param>
        /// 
        public CalculateAndStoreFromInputAndAsyncTermsTestsBasic(ITestOutputHelper output, Fixture fixture) {
            this.output = output;
            _fixture = fixture;
        }
        #endregion Test Class Constructor
        #region CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableDataTest
        [Theory]
        //[InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}}]")]
        //[InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}}}]")]
        [InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k3\",\"Item3\":{\"C\":13.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k4\",\"Item3\":{\"D\":14.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k5\",\"Item3\":{\"A\":15.0,\"B\":15.1,\"C\":15.2,\"D\":15.3}}},{\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k2\",\"Item3\":{\"A\":22.0,\"B\":22.1}}},{\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k3\",\"Item3\":{\"A\":23.0,\"E\":22.4}}}]")]
        public async void CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableDataTest(string inTestData) {
            // arrange
            // Fixture.Log.Debug("starting test CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableDataTest");
            // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
            _fixture.resultsCODEvents.Clear();
            // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between tests, the list needs to be cleared
            _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
            IEnumerable<IInputMessage<string, double>> _imcoll;

            //            CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsLevel0CODCollectionChanged,

            Fixture.ISSDataConcrete iSSData = new Fixture.ISSDataConcrete(_fixture.onResultsLevel0CODCollectionChanged,
                                                             _fixture.onResultsLevel1CODCollectionChanged,
                                                             _fixture.onFetchedIndividualElementsOfTerm1CollectionChanged,
                                                             _fixture.onSigIsReadyTerm1CollectionChanged,
                                                             _fixture.onFetchingIndividualElementsOfTerm1CollectionChanged,
                                                             _fixture.onFetchingElementSetsOfTerm1CollectionChanged);

            using (iSSData) {
                /*
                var calculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableData = new CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableData(_fixture.iSSData,
                _fixture.mockTerm1.Object,
                CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                */
                var calculateAndStoreFromInputAndAsyncTerms = new CalculateAndStoreFromInputAndAsyncTerms(iSSData,
                                                                                                          _fixture.mockTerm1.Object,
                                                                                                          CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                // act
                try
                {
                    _imcoll = JsonConvert.DeserializeObject<InputMessage<string, double>[]>(inTestData);
                }
                catch
                {
                    ArgumentException e = new ArgumentException($"{inTestData} does not match the needed input pattern");
                    throw e;
                }
                foreach(var im in _imcoll) {
                    var sendAsyncResults = calculateAndStoreFromInputAndAsyncTerms.InputBlock.SendAsync<IInputMessage<string, double>>(im);
                    await sendAsyncResults;
                }
                // inform the head of the network that there is no more data
                calculateAndStoreFromInputAndAsyncTerms.InputBlock.Complete();
                // wait for the network to indicate completion
                await calculateAndStoreFromInputAndAsyncTerms.CompletionTask;
            }
            // assert
            // send the observed events to test output
            _fixture.resultsCODEvents.Keys.OrderBy(x => x)
                .ToList()
                .ForEach(x => output.WriteLine($"{x} : {_fixture.resultsCODEvents[x]}"));

            // Count the number of inner and outer CollectionChanged events that occurred
            var numInnerNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyLevel1CollectionChanged"))
                                                      .ToList()
                                                      .Count;
            var numOuterNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyLevel0CollectionChanged"))
                                                      .ToList()
                                                      .Count;

            iSSData.ResultsCOD.Keys.Count<string>()
                .Should()
                .Be(2);
            iSSData.ResultsCOD.Keys.Should()
                .Contain("k1");
            iSSData.ResultsCOD["k1"].Keys.Should()
                .Contain("k1");
            iSSData.ResultsCOD["k1"]["k1"].Should()
                .Be(0.110M);

            Assert.Equal(_fixture.resultsCODEvents.Keys.Count,
                         numOuterNotifyCollectionChanged +
                numInnerNotifyCollectionChanged);
        }
        #endregion CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableDataTest
        #region ParseSingleInputStringFormattedAsJSONCollectionToInputMessageTest
        [Theory]
        //[InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}}]")]
        //[InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}}}]")]
        [InlineData("[{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k3\",\"Item3\":{\"C\":13.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k4\",\"Item3\":{\"D\":14.0}}},{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k5\",\"Item3\":{\"A\":15.0,\"B\":15.1,\"C\":15.2,\"D\":15.3}}},{\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k2\",\"Item3\":{\"A\":22.0,\"B\":22.1}}},{\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k3\",\"Item3\":{\"A\":23.0,\"E\":22.4}}}]")]
        public async void ParseSingleInputStringFormattedAsJSONCollectionToInputMessageTest(string inTestData) {
            // arrange
            List<InputMessage<string, double>> result = new List<InputMessage<string, double>>();
            /*
            var action = new Action<IEnumerable<IInputMessage<string, double>>>(im => result.AddRange(im));
            var parseSingleInputStringFormattedAsJSONCollectionToAction = new ParseSingleInputStringFormattedAsJSONCollectionToAction(action);
            */
            //IEnumerable<InputMessage<string, double>> _imcoll;
            InputMessage<string, double>[] _imcoll;

            // act
            try
            {
                // _imcoll = JsonConvert.DeserializeObject<IEnumerable<InputMessage<string, double>>>(inTestData);
                _imcoll = JsonConvert.DeserializeObject<InputMessage<string, double>[]>(inTestData);
            }
            catch
            {
                ArgumentException e = new ArgumentException($"{inTestData} does not match the needed input pattern");
                throw e;
            }
            foreach(var im in _imcoll) {
                result.Add(im);
            }
            /*
                        // inform the head of the network that there is no more data
                        parseSingleInputStringFormattedAsJSONCollectionToAction.InputBlock.Complete();
                        // wait for the network to indicate completion
                        await parseSingleInputStringFormattedAsJSONCollectionToAction.CompletionTask;
            */
            // assert

            result.Should()
                .HaveCount(7);
            result[0].Should()
                .BeOfType(typeof(InputMessage<string, double>));
            result[0].Value.k1.Should()
                .Be("k1");
        }
        #endregion ParseSingleInputStringFormattedAsJSONCollectionToInputMessageTest
        #region ParseSingleInputStringFormattedAsJSONToInputMessageTest
        // Ensure that the dataflow ParseSingleInputStringFormattedAsJSONToInputMessage will take in a string and put out an InputMessage

        [Theory]
        [InlineData("{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}}")]
        public async void ParseSingleInputStringFormattedAsJSONToInputMessageTest(string inTestData) {
            // arrange
            // arrange
            //_fixture.m_logger.Debug("starting test");
            InputMessage<string, double> result = default;
            var action = new Action<InputMessage<string, double>>(im => result =
                im);
            var parseSingleInputStringFormattedAsJSONToInputMessage = new ParseSingleInputStringFormattedAsJSONToInputMessage(action,
                                                                                                                              CalculateAndStoreFromInputAndAsyncTermsOptions.Verbose);

            // act
            var sendAsyncResults = parseSingleInputStringFormattedAsJSONToInputMessage.InputBlock.SendAsync(inTestData);
            await sendAsyncResults;
            // inform the head of the network that there is no more data
            parseSingleInputStringFormattedAsJSONToInputMessage.InputBlock.Complete();
            // wait for the network to indicate completion
            await parseSingleInputStringFormattedAsJSONToInputMessage.CompletionTask;

            // assert
            result.Should()
                .BeOfType(typeof(InputMessage<string, double>));
            result.Value.k1.Should()
                .Be("k1");
            result.Value.k2.Should()
                .Be("k1");
            result.Value.terms1.Should()
                .BeOfType(typeof(ReadOnlyDictionary<string, double>));
            result.Value.terms1.Keys.Count<string>()
                .Should()
                .Be(1);
            result.Value.terms1.Keys.Should()
                .Contain("A");
            result.Value.terms1.Values.Should()
                .Contain(11.0);
        }

        public class ParseSingleInputStringFormattedAsJSONToInputMessage : Dataflow<string> {
            ITargetBlock<string> _headBlock;

            public ParseSingleInputStringFormattedAsJSONToInputMessage(Action<InputMessage<string, double>> action, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
                var _bAccepter = new ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms.ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                var _bTerminator = DataflowUtils.FromDelegate<InputMessage<string, double>>(action);
                _bAccepter.Name = "_bAccepter";
                _bTerminator.Name = "_bTerminator";
                this.RegisterChild(_bAccepter);
                this.RegisterChild(_bTerminator);
                _bAccepter.LinkTo(_bTerminator);
                _bTerminator.RegisterDependency(_bAccepter);
                this._headBlock = _bAccepter.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }
        #endregion ParseSingleInputStringFormattedAsJSONToInputMessageTest
        #region CalculateAndStoreSingleInputStringFormattedAsJSONToObservableDataTest
        [Theory]
        [InlineData("{\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}}}")]
        public async void CalculateAndStoreSingleInputStringFormattedAsJSONToObservableDataTest(string inTestData) {
            // arrange
            //Fixture.Log.Debug("starting test");
            // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
            _fixture.resultsCODEvents.Clear();
            // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between tests, the list needs to be cleared
            _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
            //        CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsLevel0CODCollectionChanged,

            Fixture.ISSDataConcrete iSSData = new Fixture.ISSDataConcrete(_fixture.onResultsLevel0CODCollectionChanged,
                                                                         _fixture.onResultsLevel1CODCollectionChanged,
                                                                         _fixture.onFetchedIndividualElementsOfTerm1CollectionChanged,
                                                                         _fixture.onSigIsReadyTerm1CollectionChanged,
                                                                         _fixture.onFetchingIndividualElementsOfTerm1CollectionChanged,
                                                                         _fixture.onFetchingElementSetsOfTerm1CollectionChanged);

             using(iSSData) {
            var calculateAndStoreSingleInputStringFormattedAsJSONToObservableData = new CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData(iSSData,
                                                                                                                                                              _fixture.mockTerm1.Object,
                                                                                                                                                              CalculateAndStoreFromInputAndAsyncTermsOptions.Default);

                // act
                var sendAsyncResults = calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.InputBlock.SendAsync(inTestData);
                await sendAsyncResults;
                // wait a minute to debug
                //await Task.Delay(new TimeSpan(0, 0, 1));
                // inform the head of the network that there is no more data
                calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.InputBlock.Complete();
                // wait for the network to indicate completion
                await calculateAndStoreSingleInputStringFormattedAsJSONToObservableData.CompletionTask;

                // assert
                // send the observed events to test output
                _fixture.resultsCODEvents.Keys.OrderBy(x => x)
                    .ToList()
                    .ForEach(x => output.WriteLine($"{x} : {_fixture.resultsCODEvents[x]}"));

                // Count the number of inner and outer CollectionChanged events that occurred
                var numInnerNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyLevel1CollectionChanged"))
                                                          .ToList()
                                                          .Count;
                var numOuterNotifyCollectionChanged = _fixture.resultsCODEvents.Keys.Where(x => x.Contains("Event: NotifyLevel0CollectionChanged"))
                                                          .ToList()
                                                          .Count;
                iSSData.ResultsCOD.Keys.Count<string>()
                    .Should()
                    .Be(1);
                iSSData.ResultsCOD.Keys.Should()
                    .Contain("k1");
                iSSData.ResultsCOD["k1"].Keys.Should()
                    .Contain("k1");
                iSSData.ResultsCOD["k1"]["k1"].Should()
                    .Be(0.110M);

                Assert.Equal(_fixture.resultsCODEvents.Keys.Count,
                             numOuterNotifyCollectionChanged +
                    numInnerNotifyCollectionChanged);
            }
        }

        public class CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData : Dataflow<string> {
            ITargetBlock<string> _headBlock;

            public CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData(CalculateAndStoreFromInputAndAsyncTermsObservableData<decimal> iSSData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
                var _bAccepter = new ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms.ParseSingleInputStringFormattedAsJSONToInputMessage(CalculateAndStoreFromInputAndAsyncTermsOptions.Verbose);
                var _calculateAndStoreFromInputAndAsyncTerms = new CalculateAndStoreFromInputAndAsyncTerms(iSSData,
                                                                                                           webGet,
                                                                                                           calculateAndStoreFromInputAndAsyncTermsOptions);
                _bAccepter.Name = "_bAccepter";
                _calculateAndStoreFromInputAndAsyncTerms.Name = "_bTerminator";
                this.RegisterChild(_bAccepter);
                this.RegisterChild(_calculateAndStoreFromInputAndAsyncTerms);
                _bAccepter.LinkTo(_calculateAndStoreFromInputAndAsyncTerms);
                _calculateAndStoreFromInputAndAsyncTerms.RegisterDependency(_bAccepter);
                this._headBlock = _bAccepter.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }
        #endregion CalculateAndStoreSingleInputStringFormattedAsJSONToObservableDataTest
        /*
        public class ParseSingleInputStringFormattedAsJSONCollectionToAction : Dataflow<string>
        {
            ITargetBlock<string> _headBlock;

            public ParseSingleInputStringFormattedAsJSONCollectionToAction(Action<IEnumerable<IInputMessage<string, double>>> action) : base(CalculateAndStoreFromInputAndAsyncTermsOptions.Default)
            {
                var _bAccepter = new ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection(CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                var _bTerminator = DataflowUtils.FromDelegate<IEnumerable<IInputMessage<string, double>>>(action);
                _bAccepter.Name = "_bAccepter";
                _bTerminator.Name = "_bTerminator";
                this.RegisterChild(_bAccepter);
                this.RegisterChild(_bTerminator);
                _bAccepter.LinkTo(_bTerminator);
                _bTerminator.RegisterDependency(_bAccepter);
                this._headBlock = _bAccepter.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
        }

        /// <summary>
        /// Class CalculateAndStoreSingleInputStringFormattedAsJSONToObservableData.
        /// </summary>
        /// <seealso cref="Gridsum.DataflowEx.Dataflow{System.String}" />
        public class CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableData : Dataflow<string>
        {
            ITargetBlock<string> _headBlock;

            public CalculateAndStoreSingleInputStringFormattedAsJSONCollectionToObservableData(CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions)
            {
                var _bAccepter = new ParseSingleInputStringFormattedAsJSONCollectionToInputMessageCollection(CalculateAndStoreFromInputAndAsyncTermsOptions.Default);
                var _bTerminator = new CalculateAndStoreFromInputAndAsyncTerms(_fixture.iSSData,
                                                                              webGet,
                                                                              calculateAndStoreFromInputAndAsyncTermsOptions);

                _bAccepter.Name = "_bAccepter";
                _bTerminator.Name = "_bTerminator";
                this.RegisterChild(_bAccepter);
                this.RegisterChild(_bTerminator);
                _bAccepter.LinkTo(_bTerminator);
                _bTerminator.RegisterDependency(_bAccepter);

                // Need a propagator block between the input collection and the CalculateAndStoreFromInputAndAsyncTerms to do the iterating.
                this._headBlock = _bAccepter.InputBlock;
            }

            public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
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
    // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between tests, the list needs to be cleared
    _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
    
    
    //act
    // log start
    // _logger.Debug("Logging");
    // Create the Observable data structures and their event handlers
    using(CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsLevel0CODCollectionChanged,
    _fixture.onResultsNestedCODPropertyChanged,
    _fixture.onFetchedIndividualElementsOfTerm1CollectionChanged,
    _fixture.onSigIsReadyTerm1CollectionChanged,
    _fixture.onFetchingIndividualElementsOfTerm1CollectionChanged)) {
    // Create a new DataFlowEx network that combines the ParseInputStringFormattedAsJSONToInputMessage and CalculateAndStoreFromInputAndAsyncTerms networks
    var parseStringToTupleThenResultsFromInputAnd1Term = new ParseStringToTupleThenResultsFromInputAnd1Term(_fixture.iSSData,
    _fixture.mockTerm1.Object,
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
    // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between test, the list needs to be cleared
    _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
    }
    
    [Theory]
    [InlineData("[\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k1\",\"Item3\":{\"A\":11.0}},\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k2\",\"Item3\":{\"B\":12.0}},\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k3\",\"Item3\":{\"C\":13.0}},\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k4\",\"Item3\":{\"D\":14.0}},\"Value\":{\"Item1\":\"k1\",\"Item2\":\"k5\",\"Item3\":{\"A\":15.0,\"B\":15.1,\"C\":15.2,\"D\":15.3}},\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k2\",\"Item3\":{\"A\":22.0,\"B\":22.1}},\"Value\":{\"Item1\":\"k2\",\"Item2\":\"k3\",\"Item3\":{\"A\":23.0,\"E\":22.4}}]")]
    public async void RFromJSONInputAnd1TermTest1(string testInStr) {
    //arrange
    // since the resultsCODEvents list in the fixture is shared between tests, the list needs to be cleared
    _fixture.resultsCODEvents.Clear();
    // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between tests, the list needs to be cleared
    _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
    
    // act
    // Create the Observable data structures and their event handlers
    using(CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData = new CalculateAndStoreFromInputAndAsyncTermsObservableData(_fixture.onResultsLevel0CODCollectionChanged,
    _fixture.onResultsNestedCODPropertyChanged,
    _fixture.onFetchedIndividualElementsOfTerm1CollectionChanged,
    _fixture.onSigIsReadyTerm1CollectionChanged,
    _fixture.onFetchingIndividualElementsOfTerm1CollectionChanged)) {
    CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions = new CalculateAndStoreFromInputAndAsyncTermsOptions();
    // Create the new DataFlowEx network 
    var rFromJSONInputAnd1Term = new ParseJSONStringCollectionToInputMessage(_fixture.iSSData,
    _fixture.mockTerm1.Object,
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
    // since the fetchedIndividualElementsOfTerm1Events list in the fixture is shared between test, the list needs to be cleared
    _fixture.fetchedIndividualElementsOfTerm1Events.Clear();
    }
    
    
    class ParseStringToTupleThenResultsFromInputAnd1Term : Dataflow<string> {
    // Head
    ITargetBlock<string> _headBlock;
    
    // Constructor
    public ParseStringToTupleThenResultsFromInputAnd1Term(CalculateAndStoreFromInputAndAsyncTermsObservableData _fixture.iSSData, IWebGet webGet, CalculateAndStoreFromInputAndAsyncTermsOptions calculateAndStoreFromInputAndAsyncTermsOptions) : base(calculateAndStoreFromInputAndAsyncTermsOptions) {
    // Create the DataFlowEx network that accepts a long string (test data) and breaks it into individual inputs for the following network
    var _bAccepter = new ParseInputStringFormattedAsJSONToInputMessage();
    // Create the DataFlowEx network that calculates a Results COD from a formula and a term
    // The instance _fixture.iSSData supplies the Results COD and the term1 COD
    CalculateAndStoreFromInputAndAsyncTerms _bTerminator = new CalculateAndStoreFromInputAndAsyncTerms(_fixture.iSSData,
    webGet,
    calculateAndStoreFromInputAndAsyncTermsOptions);
    
    _bAccepter.Name = "_bAccepter";
    _bTerminator.Name = "_bTerminator";
    
    // Link Dataflow 
    // Link _bAccepter to _bTerminator when the message has isReadyToCalculate = true
    _bAccepter.LinkTo(_bTerminator);
    
    // Link completion
    _bTerminator.RegisterDependency(_bAccepter);
    
    this.RegisterChild(_bAccepter);
    this.RegisterChild(_bTerminator);
    
    this._headBlock = _bAccepter.InputBlock;
    }
    
    public override ITargetBlock<string> InputBlock { get { return this._headBlock; } }
    }
    */    }
}