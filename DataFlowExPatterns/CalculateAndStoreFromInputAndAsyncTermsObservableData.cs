using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using ATAP.Utilities.Logging.Logging;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    public class CalculateAndStoreFromInputAndAsyncTermsObservableData : IDisposable {
        // a thread-safe place to keep track of which individual key values of the set of key values of Term1 (sig.IndividualElements) are FetchingIndividualElementsOfTerm1
        ConcurrentObservableDictionary<string, Task<double>> _fetchingIndividualElementsOfTerm1;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are in process of being fetched
        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> _fetchingElementSetsOfTerm1;
        // A thread-safe place to keep the final results
        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> _resultsCOD;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are ReadyToCalculate
        ConcurrentObservableDictionary<string, byte> _elementSetsOfTerm1Ready;
        // A thread-safe place to keep the values fetched for each element of term1
        ConcurrentObservableDictionary<string, double> _fetchedIndividualElementsOfTerm1;
        NotifyCollectionChangedEventHandler onFetchingIndividualElementsOfTerm1CollectionChanged;
        PropertyChangedEventHandler onFetchingIndividualElementsOfTerm1PropertyChanged;
        NotifyCollectionChangedEventHandler onFetchingElementSetsOfTerm1CollectionChanged;
        PropertyChangedEventHandler onFetchingElementSetsOfTerm1PropertyChanged;
        NotifyCollectionChangedEventHandler onResultsCODCollectionChanged;
        PropertyChangedEventHandler onResultsCODPropertyChanged;
        NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged;
        PropertyChangedEventHandler onResultsNestedCODPropertyChanged;
        NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged;
        PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged;
        NotifyCollectionChangedEventHandler onFetchedIndividualElementsOfTerm1CollectionChanged;
        PropertyChangedEventHandler onFetchedIndividualElementsOfTerm1PropertyChanged;

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, NotifyCollectionChangedEventHandler onFetchedIndividualElementsOfTerm1CollectionChanged, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, NotifyCollectionChangedEventHandler onFetchingIndividualElementsOfTerm1CollectionChanged, NotifyCollectionChangedEventHandler onFetchingElementSetsOfTerm1CollectionChanged) : this(new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onResultsCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onResultsNestedCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, double>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onFetchedIndividualElementsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, byte>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onSigIsReadyTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, Task<double>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onFetchingIndividualElementsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onFetchingElementSetsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null) {
        }

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, PropertyChangedEventHandler onResultsCODPropertyChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, PropertyChangedEventHandler onResultsNestedCODPropertyChanged, ConcurrentObservableDictionary<string, double> FetchedIndividualElementsOfTerm1, NotifyCollectionChangedEventHandler onFetchedIndividualElementsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchedIndividualElementsOfTerm1PropertyChanged, ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged, ConcurrentObservableDictionary<string, Task> fetchingIndividualElementsOfTerm1, NotifyCollectionChangedEventHandler onFetchingIndividualElementsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchingIndividualElementsOfTerm1PropertyChanged, NotifyCollectionChangedEventHandler onFetchingElementSetsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchingElementSetsOfTerm1PropertyChanged) : this(new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsCODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsNestedCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsNestedCODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, double>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchedIndividualElementsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchedIndividualElementsOfTerm1PropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, byte>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onSigIsReadyTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onSigIsReadyTerm1PropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, Task<double>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchingIndividualElementsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchingIndividualElementsOfTerm1PropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchingElementSetsOfTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onFetchingElementSetsOfTerm1PropertyChanged) {
        }

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> resultsCOD, NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, PropertyChangedEventHandler onResultsCODPropertyChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, PropertyChangedEventHandler onResultsNestedCODPropertyChanged, ConcurrentObservableDictionary<string, double> FetchedIndividualElementsOfTerm1, NotifyCollectionChangedEventHandler onFetchedIndividualElementsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchedIndividualElementsOfTerm1PropertyChanged, ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged, ConcurrentObservableDictionary<string, Task<double>> fetchingIndividualElementsOfTerm1, NotifyCollectionChangedEventHandler onFetchingIndividualElementsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchingIndividualElementsOfTerm1PropertyChanged, ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> fetchingElementSetsOfTerm1, NotifyCollectionChangedEventHandler onFetchingElementSetsOfTerm1CollectionChanged, PropertyChangedEventHandler onFetchingElementSetsOfTerm1PropertyChanged) {
            Log.Trace("Constructor starting");
            _resultsCOD = resultsCOD;
            this.onResultsCODCollectionChanged = onResultsCODCollectionChanged;
            if(this.onResultsCODCollectionChanged != null)
            {
                _resultsCOD.CollectionChanged += this.onResultsCODCollectionChanged;
            }

            this.onResultsCODPropertyChanged = onResultsCODPropertyChanged;
            if(this.onResultsCODPropertyChanged != null)
            {
                _resultsCOD.PropertyChanged += this.onResultsCODPropertyChanged;
            }

            this.onResultsNestedCODCollectionChanged = onResultsNestedCODCollectionChanged;
            this.onResultsNestedCODPropertyChanged = onResultsNestedCODPropertyChanged;
            _fetchedIndividualElementsOfTerm1 = FetchedIndividualElementsOfTerm1;
            this.onFetchedIndividualElementsOfTerm1CollectionChanged = onFetchedIndividualElementsOfTerm1CollectionChanged;
            if(this.onFetchedIndividualElementsOfTerm1CollectionChanged != null)
            {
                _fetchedIndividualElementsOfTerm1.CollectionChanged += this.onFetchedIndividualElementsOfTerm1CollectionChanged;
            }

            this.onFetchedIndividualElementsOfTerm1PropertyChanged = onFetchedIndividualElementsOfTerm1PropertyChanged;
            if(this.onFetchedIndividualElementsOfTerm1PropertyChanged != null)
            {
                _fetchedIndividualElementsOfTerm1.PropertyChanged += this.onFetchedIndividualElementsOfTerm1PropertyChanged;
            }

            _elementSetsOfTerm1Ready = SigIsReadyTerm1;
            this.onSigIsReadyTerm1CollectionChanged = onSigIsReadyTerm1CollectionChanged;
            if(this.onSigIsReadyTerm1CollectionChanged != null)
            {
                _elementSetsOfTerm1Ready.CollectionChanged += this.onSigIsReadyTerm1CollectionChanged;
            }

            this.onSigIsReadyTerm1PropertyChanged = onSigIsReadyTerm1PropertyChanged;
            if(this.onSigIsReadyTerm1PropertyChanged != null)
            {
                _elementSetsOfTerm1Ready.PropertyChanged += this.onSigIsReadyTerm1PropertyChanged;
            }

            _fetchingIndividualElementsOfTerm1 = fetchingIndividualElementsOfTerm1;
            this.onFetchingIndividualElementsOfTerm1CollectionChanged = onFetchingIndividualElementsOfTerm1CollectionChanged;
            if(this.onFetchingIndividualElementsOfTerm1CollectionChanged !=
                null)
            {
                _fetchingIndividualElementsOfTerm1.CollectionChanged += this.onFetchingIndividualElementsOfTerm1CollectionChanged;
            }

            this.onFetchingIndividualElementsOfTerm1PropertyChanged = onFetchingIndividualElementsOfTerm1PropertyChanged;
            if(this.onFetchingIndividualElementsOfTerm1PropertyChanged !=
                null)
            {
                _fetchingIndividualElementsOfTerm1.PropertyChanged += this.onFetchingIndividualElementsOfTerm1PropertyChanged;
            }

            _fetchingElementSetsOfTerm1 = fetchingElementSetsOfTerm1;
            this.onFetchingElementSetsOfTerm1CollectionChanged = onFetchingElementSetsOfTerm1CollectionChanged;
            if(this.onFetchingElementSetsOfTerm1CollectionChanged != null)
            {
                _fetchingElementSetsOfTerm1.CollectionChanged += this.onFetchingElementSetsOfTerm1CollectionChanged;
            }

            this.onFetchingElementSetsOfTerm1PropertyChanged = onFetchingElementSetsOfTerm1PropertyChanged;
            if(this.onFetchingElementSetsOfTerm1PropertyChanged != null)
            {
                _fetchingElementSetsOfTerm1.PropertyChanged += this.onFetchingElementSetsOfTerm1PropertyChanged;
            }

            Log.Trace("Constructor Finished");
        }

        public void RecordR(string k1, string k2, decimal pr) {
            if(ResultsCOD.ContainsKey(k1)) {
                var innerCOD = ResultsCOD[k1];
                if(innerCOD.ContainsKey(k2)) {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else {
                    //ToDo: Better understanding/handling of exceptions here
                    try { innerCOD.Add(k2, pr); } catch { new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed"); }
                }
            }
            else {
                var innerCOD = new ConcurrentObservableDictionary<string, decimal>();
                if(this.onResultsNestedCODCollectionChanged != null) {
                    innerCOD.CollectionChanged += this.onResultsNestedCODCollectionChanged;
                }

                if(this.onResultsNestedCODPropertyChanged != null) {
                    innerCOD.PropertyChanged += this.onResultsNestedCODPropertyChanged;
                }

                try { innerCOD.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { ResultsCOD.Add(k1, innerCOD); } catch { new Exception($"adding the new innerDictionary to cODR keyed by {k1} failed"); }
            };
        }

        public ConcurrentObservableDictionary<string, Task<double>> FetchingIndividualElementsOfTerm1 { get => _fetchingIndividualElementsOfTerm1; set => _fetchingIndividualElementsOfTerm1 =
            value; }

        public NotifyCollectionChangedEventHandler OnFetchingIndividualElementsOfTerm1CollectionChanged { get => onFetchingIndividualElementsOfTerm1CollectionChanged; set => onFetchingIndividualElementsOfTerm1CollectionChanged =
            value; }

        public PropertyChangedEventHandler OnFetchingIndividualElementsOfTerm1PropertyChanged { get => onFetchingIndividualElementsOfTerm1PropertyChanged; set => onFetchingIndividualElementsOfTerm1PropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnResultsCODCollectionChanged { get => onResultsCODCollectionChanged; set => onResultsCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnResultsCODPropertyChanged { get => onResultsCODPropertyChanged; set => onResultsCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnResultsNestedCODCollectionChanged { get => onResultsNestedCODCollectionChanged; set => onResultsNestedCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnResultsNestedCODPropertyChanged { get => onResultsNestedCODPropertyChanged; set => onResultsNestedCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnSigIsReadyTerm1CollectionChanged { get => onSigIsReadyTerm1CollectionChanged; set => onSigIsReadyTerm1CollectionChanged =
            value; }

        public PropertyChangedEventHandler OnSigIsReadyTerm1PropertyChanged { get => onSigIsReadyTerm1PropertyChanged; set => onSigIsReadyTerm1PropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnFetchedIndividualElementsOfTerm1CollectionChanged { get => onFetchedIndividualElementsOfTerm1CollectionChanged; set => onFetchedIndividualElementsOfTerm1CollectionChanged =
            value; }

        public PropertyChangedEventHandler OnFetchedIndividualElementsOfTerm1PropertyChanged { get => onFetchedIndividualElementsOfTerm1PropertyChanged; set => onFetchedIndividualElementsOfTerm1PropertyChanged =
            value; }

        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> ResultsCOD { get => _resultsCOD; set => _resultsCOD =
            value; }

        public ConcurrentObservableDictionary<string, byte> ElementSetsOfTerm1Ready { get => _elementSetsOfTerm1Ready; set => _elementSetsOfTerm1Ready =
            value; }

        public ConcurrentObservableDictionary<string, double> FetchedIndividualElementsOfTerm1 { get => _fetchedIndividualElementsOfTerm1; set => _fetchedIndividualElementsOfTerm1 =
            value; }

        #region Configure this class to use ATAP.Utilities.Logging
        internal static ILog Log { get; set; }

        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> FetchingElementSetsOfTerm1 { get => _fetchingElementSetsOfTerm1; set => _fetchingElementSetsOfTerm1 =
            value; }

        public NotifyCollectionChangedEventHandler OnFetchingElementSetsOfTerm1CollectionChanged { get => onFetchingElementSetsOfTerm1CollectionChanged; set => onFetchingElementSetsOfTerm1CollectionChanged =
            value; }

        public PropertyChangedEventHandler OnFetchingElementSetsOfTerm1PropertyChanged { get => onFetchingElementSetsOfTerm1PropertyChanged; set => onFetchingElementSetsOfTerm1PropertyChanged =
            value; }

        static CalculateAndStoreFromInputAndAsyncTermsObservableData() {
            Log = LogProvider.For<CalculateAndStoreFromInputAndAsyncTermsObservableData>();
        }
        #endregion Configure this class to use ATAP.Utilities.Logging
        #region IDisposable Support
        public void TearDown() {
            ResultsCOD.CollectionChanged -= onResultsCODCollectionChanged;
            ResultsCOD.PropertyChanged -= onResultsCODPropertyChanged;
            var enumerator = ResultsCOD.Keys.GetEnumerator();
            try
            {
                while(enumerator.MoveNext()) {
                    var key = enumerator.Current;
                    ResultsCOD[key].CollectionChanged -= this.onResultsNestedCODCollectionChanged;
                    ResultsCOD[key].PropertyChanged -= this.onResultsNestedCODPropertyChanged;
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            FetchedIndividualElementsOfTerm1.CollectionChanged -= onFetchedIndividualElementsOfTerm1CollectionChanged;
            FetchedIndividualElementsOfTerm1.PropertyChanged -= onFetchedIndividualElementsOfTerm1PropertyChanged;
        }

        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
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
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

