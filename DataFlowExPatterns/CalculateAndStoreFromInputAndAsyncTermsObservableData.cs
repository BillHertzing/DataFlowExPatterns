using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using ATAP.Utilities.Logging.Logging;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    public class CalculateAndStoreFromInputAndAsyncTermsObservableData : IDisposable {
        // a thread-safe place to keep track of which individual key values of the set of key values of Term1 (sig.IndividualElements) are FetchingIndividualElementsOfTerm1
        ConcurrentObservableDictionary<string, Task> _isFetchingIndividualElementsOfTerm1COD;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are in process of being fetched
        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> _isFetchingSigOfTerm1COD;
        // A thread-safe place to keep the final results
        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> _resultsCOD;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are ReadyToCalculate
        ConcurrentObservableDictionary<string, byte> _sigIsReadyTerm1COD;
        // A thread-safe place to keep the values fetched for each element of term1
        ConcurrentObservableDictionary<string, double> _Term1COD;
        NotifyCollectionChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODCollectionChanged;
        PropertyChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODPropertyChanged;
        NotifyCollectionChangedEventHandler onIsFetchingSigOfTerm1CODCollectionChanged;
        PropertyChangedEventHandler onIsFetchingSigOfTerm1CODPropertyChanged;
        NotifyCollectionChangedEventHandler onResultsCODCollectionChanged;
        PropertyChangedEventHandler onResultsCODPropertyChanged;
        NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged;
        PropertyChangedEventHandler onResultsNestedCODPropertyChanged;
        NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged;
        PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged;
        NotifyCollectionChangedEventHandler onTerm1CODCollectionChanged;
        PropertyChangedEventHandler onTerm1CODPropertyChanged;

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, NotifyCollectionChangedEventHandler onTerm1CODCollectionChanged, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, NotifyCollectionChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODCollectionChanged, NotifyCollectionChangedEventHandler onIsFetchingSigOfTerm1CODCollectionChanged) : this(new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onResultsCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onResultsNestedCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, double>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, byte>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onSigIsReadyTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, Task>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onIsFetchingIndividualElementsOfTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                onIsFetchingSigOfTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                null) {
        }

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, PropertyChangedEventHandler onResultsCODPropertyChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, PropertyChangedEventHandler onResultsNestedCODPropertyChanged, ConcurrentObservableDictionary<string, double> Term1COD, NotifyCollectionChangedEventHandler onTerm1CODCollectionChanged, PropertyChangedEventHandler onTerm1CODPropertyChanged, ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged, ConcurrentObservableDictionary<string, Task> isFetchingIndividualElementsOfTerm1COD, NotifyCollectionChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODCollectionChanged, PropertyChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODPropertyChanged, NotifyCollectionChangedEventHandler onIsFetchingSigOfTerm1CODCollectionChanged, PropertyChangedEventHandler onIsFetchingSigOfTerm1CODPropertyChanged) : this(new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsCODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsNestedCODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onResultsNestedCODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, double>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onTerm1CODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, byte>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onSigIsReadyTerm1CollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onSigIsReadyTerm1PropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, Task>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onIsFetchingIndividualElementsOfTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onIsFetchingIndividualElementsOfTerm1CODPropertyChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>>(),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onIsFetchingSigOfTerm1CODCollectionChanged,
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    onIsFetchingSigOfTerm1CODPropertyChanged) {
        }

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> resultsCOD, NotifyCollectionChangedEventHandler onResultsCODCollectionChanged, PropertyChangedEventHandler onResultsCODPropertyChanged, NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged, PropertyChangedEventHandler onResultsNestedCODPropertyChanged, ConcurrentObservableDictionary<string, double> Term1COD, NotifyCollectionChangedEventHandler onTerm1CODCollectionChanged, PropertyChangedEventHandler onTerm1CODPropertyChanged, ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1, NotifyCollectionChangedEventHandler onSigIsReadyTerm1CollectionChanged, PropertyChangedEventHandler onSigIsReadyTerm1PropertyChanged, ConcurrentObservableDictionary<string, Task> isFetchingIndividualElementsOfTerm1COD, NotifyCollectionChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODCollectionChanged, PropertyChangedEventHandler onIsFetchingIndividualElementsOfTerm1CODPropertyChanged, ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> isFetchingSigOfTerm1COD, NotifyCollectionChangedEventHandler onIsFetchingSigOfTerm1CODCollectionChanged, PropertyChangedEventHandler onIsFetchingSigOfTerm1CODPropertyChanged) {
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
            _Term1COD = Term1COD;
            this.onTerm1CODCollectionChanged = onTerm1CODCollectionChanged;
            if(this.onTerm1CODCollectionChanged != null)
            {
                _Term1COD.CollectionChanged += this.onTerm1CODCollectionChanged;
            }

            this.onTerm1CODPropertyChanged = onTerm1CODPropertyChanged;
            if(this.onTerm1CODPropertyChanged != null)
            {
                _Term1COD.PropertyChanged += this.onTerm1CODPropertyChanged;
            }

            _sigIsReadyTerm1COD = SigIsReadyTerm1;
            this.onSigIsReadyTerm1CollectionChanged = onSigIsReadyTerm1CollectionChanged;
            if(this.onSigIsReadyTerm1CollectionChanged != null)
            {
                _sigIsReadyTerm1COD.CollectionChanged += this.onSigIsReadyTerm1CollectionChanged;
            }

            this.onSigIsReadyTerm1PropertyChanged = onSigIsReadyTerm1PropertyChanged;
            if(this.onSigIsReadyTerm1PropertyChanged != null)
            {
                _sigIsReadyTerm1COD.PropertyChanged += this.onSigIsReadyTerm1PropertyChanged;
            }

            _isFetchingIndividualElementsOfTerm1COD = isFetchingIndividualElementsOfTerm1COD;
            this.onIsFetchingIndividualElementsOfTerm1CODCollectionChanged = onIsFetchingIndividualElementsOfTerm1CODCollectionChanged;
            if(this.onIsFetchingIndividualElementsOfTerm1CODCollectionChanged !=
                null)
            {
                _isFetchingIndividualElementsOfTerm1COD.CollectionChanged += this.onIsFetchingIndividualElementsOfTerm1CODCollectionChanged;
            }

            this.onIsFetchingIndividualElementsOfTerm1CODPropertyChanged = onIsFetchingIndividualElementsOfTerm1CODPropertyChanged;
            if(this.onIsFetchingIndividualElementsOfTerm1CODPropertyChanged !=
                null)
            {
                _isFetchingIndividualElementsOfTerm1COD.PropertyChanged += this.onIsFetchingIndividualElementsOfTerm1CODPropertyChanged;
            }

            _isFetchingSigOfTerm1COD = isFetchingSigOfTerm1COD;
            this.onIsFetchingSigOfTerm1CODCollectionChanged = onIsFetchingSigOfTerm1CODCollectionChanged;
            if(this.onIsFetchingSigOfTerm1CODCollectionChanged != null)
            {
                _isFetchingSigOfTerm1COD.CollectionChanged += this.onIsFetchingSigOfTerm1CODCollectionChanged;
            }

            this.onIsFetchingSigOfTerm1CODPropertyChanged = onIsFetchingSigOfTerm1CODPropertyChanged;
            if(this.onIsFetchingSigOfTerm1CODPropertyChanged != null)
            {
                _isFetchingSigOfTerm1COD.PropertyChanged += this.onIsFetchingSigOfTerm1CODPropertyChanged;
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

        public ConcurrentObservableDictionary<string, Task> IsFetchingIndividualElementsOfTerm1 { get => _isFetchingIndividualElementsOfTerm1COD; set => _isFetchingIndividualElementsOfTerm1COD =
            value; }

        public NotifyCollectionChangedEventHandler OnIsFetchingIndividualElementsOfTerm1CODCollectionChanged { get => onIsFetchingIndividualElementsOfTerm1CODCollectionChanged; set => onIsFetchingIndividualElementsOfTerm1CODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnIsFetchingIndividualElementsOfTerm1CODPropertyChanged { get => onIsFetchingIndividualElementsOfTerm1CODPropertyChanged; set => onIsFetchingIndividualElementsOfTerm1CODPropertyChanged =
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

        public NotifyCollectionChangedEventHandler OnTerm1CODCollectionChanged { get => onTerm1CODCollectionChanged; set => onTerm1CODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnTerm1CODPropertyChanged { get => onTerm1CODPropertyChanged; set => onTerm1CODPropertyChanged =
            value; }

        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> ResultsCOD { get => _resultsCOD; set => _resultsCOD =
            value; }

        public ConcurrentObservableDictionary<string, byte> SigIsReadyTerm1COD { get => _sigIsReadyTerm1COD; set => _sigIsReadyTerm1COD =
            value; }

        public ConcurrentObservableDictionary<string, double> Term1COD { get => _Term1COD; set => _Term1COD =
            value; }

        #region Configure this class to use ATAP.Utilities.Logging
        internal static ILog Log { get; set; }

        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, byte>> IsFetchingSigOfTerm1COD { get => _isFetchingSigOfTerm1COD; set => _isFetchingSigOfTerm1COD =
            value; }

        public NotifyCollectionChangedEventHandler OnIsFetchingSigOfTerm1CODCollectionChanged { get => onIsFetchingSigOfTerm1CODCollectionChanged; set => onIsFetchingSigOfTerm1CODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnIsFetchingSigOfTerm1CODPropertyChanged { get => onIsFetchingSigOfTerm1CODPropertyChanged; set => onIsFetchingSigOfTerm1CODPropertyChanged =
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
            Term1COD.CollectionChanged -= onTerm1CODCollectionChanged;
            Term1COD.PropertyChanged -= onTerm1CODPropertyChanged;
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

