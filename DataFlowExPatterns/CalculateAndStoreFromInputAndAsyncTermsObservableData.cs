using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    public class CalculateAndStoreFromInputAndAsyncTermsObservableData : IDisposable {
        // a thread-safe place to keep track of which individual key values of the set of key values of Term1 (sig.IndividualTerms) are FetchingIndividualTermKey
        ConcurrentObservableDictionary<string, Task> _isFetchingIndividualTermKeyCOD;
        ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> _resultsCOD;
        // A thread-safe place to keep track of which complete sets of key values (sig.Longest) of Term1 are ReadyToCalculate
        ConcurrentObservableDictionary<string, byte> _sigIsReadyToCalculateAndStoreCOD;
        ConcurrentObservableDictionary<string, double> _termCOD1;
        NotifyCollectionChangedEventHandler onIsFetchingIndividualTermKeyCODCollectionChanged;
        PropertyChangedEventHandler onIsFetchingIndividualTermKeyCODPropertyChanged;
        NotifyCollectionChangedEventHandler onResultsCODCollectionChanged;
        PropertyChangedEventHandler onResultsCODPropertyChanged;
        NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged;
        PropertyChangedEventHandler onResultsNestedCODPropertyChanged;
        NotifyCollectionChangedEventHandler onSigIsReadyToCalculateAndStoreCODCollectionChanged;
        PropertyChangedEventHandler onSigIsReadyToCalculateAndStoreCODPropertyChanged;
        NotifyCollectionChangedEventHandler onTermCOD1CollectionChanged;
        PropertyChangedEventHandler onTermCOD1PropertyChanged;

        public CalculateAndStoreFromInputAndAsyncTermsObservableData(
                NotifyCollectionChangedEventHandler onResultsCODCollectionChanged,
                NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged,
                NotifyCollectionChangedEventHandler onTermCOD1CollectionChanged,
                NotifyCollectionChangedEventHandler onSigIsReadyToCalculateAndStoreCODCollectionChanged,
                NotifyCollectionChangedEventHandler onIsFetchingIndividualTermKeyCODCollectionChanged
            ) : this(
            new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
            onResultsCODCollectionChanged,
            null,
            onResultsNestedCODCollectionChanged,
            null,
            new ConcurrentObservableDictionary<string, double>(),
            onTermCOD1CollectionChanged,
            null,
            new ConcurrentObservableDictionary<string, byte>(),
            onSigIsReadyToCalculateAndStoreCODCollectionChanged,
            null,
            new ConcurrentObservableDictionary<string, Task>(),
            onIsFetchingIndividualTermKeyCODCollectionChanged,
            null
            )
        { }
        
        
        public CalculateAndStoreFromInputAndAsyncTermsObservableData(
            NotifyCollectionChangedEventHandler onResultsCODCollectionChanged,
            PropertyChangedEventHandler onResultsCODPropertyChanged,
            NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged,
            PropertyChangedEventHandler onResultsNestedCODPropertyChanged,
            ConcurrentObservableDictionary<string, double> termCOD1,
            NotifyCollectionChangedEventHandler onTermCOD1CollectionChanged,
            PropertyChangedEventHandler onTermCOD1PropertyChanged,
            ConcurrentObservableDictionary<string, byte> sigIsReadyToCalculateAndStoreCOD,
            NotifyCollectionChangedEventHandler onSigIsReadyToCalculateAndStoreCODCollectionChanged,
            PropertyChangedEventHandler onSigIsReadyToCalculateAndStoreCODPropertyChanged,
            ConcurrentObservableDictionary<string, Task> isFetchingIndividualTermKeyCOD,
            NotifyCollectionChangedEventHandler onIsFetchingIndividualTermKeyCODCollectionChanged,
            PropertyChangedEventHandler onIsFetchingIndividualTermKeyCODPropertyChanged
            ) : this (
            new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>(),
            onResultsCODCollectionChanged,
            onResultsCODPropertyChanged,
            onResultsNestedCODCollectionChanged,
            onResultsNestedCODPropertyChanged,
            new ConcurrentObservableDictionary<string, double>(),
            onTermCOD1CollectionChanged,
            onTermCOD1PropertyChanged,
            new ConcurrentObservableDictionary<string, byte>(),
            onSigIsReadyToCalculateAndStoreCODCollectionChanged,
            onSigIsReadyToCalculateAndStoreCODPropertyChanged,
            new ConcurrentObservableDictionary<string, Task> (),
            onIsFetchingIndividualTermKeyCODCollectionChanged,
            onIsFetchingIndividualTermKeyCODPropertyChanged
            )
        {        }
        public CalculateAndStoreFromInputAndAsyncTermsObservableData(
            ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> resultsCOD,
            NotifyCollectionChangedEventHandler onResultsCODCollectionChanged,
            PropertyChangedEventHandler onResultsCODPropertyChanged,
            NotifyCollectionChangedEventHandler onResultsNestedCODCollectionChanged,
            PropertyChangedEventHandler onResultsNestedCODPropertyChanged,
            ConcurrentObservableDictionary<string, double> termCOD1,
            NotifyCollectionChangedEventHandler onTermCOD1CollectionChanged,
            PropertyChangedEventHandler onTermCOD1PropertyChanged,
            ConcurrentObservableDictionary<string, byte> sigIsReadyToCalculateAndStoreCOD,
            NotifyCollectionChangedEventHandler onSigIsReadyToCalculateAndStoreCODCollectionChanged,
            PropertyChangedEventHandler onSigIsReadyToCalculateAndStoreCODPropertyChanged,
            ConcurrentObservableDictionary<string, Task> isFetchingIndividualTermKeyCOD, 
            NotifyCollectionChangedEventHandler onIsFetchingIndividualTermKeyCODCollectionChanged, 
            PropertyChangedEventHandler onIsFetchingIndividualTermKeyCODPropertyChanged 
)
        {
            _resultsCOD = resultsCOD;
            this.onResultsCODCollectionChanged = onResultsCODCollectionChanged;
            if (this.onResultsCODCollectionChanged != null) _resultsCOD.CollectionChanged += this.onResultsCODCollectionChanged;
            this.onResultsCODPropertyChanged = onResultsCODPropertyChanged;
            if(this.onResultsCODPropertyChanged != null) _resultsCOD.PropertyChanged += this.onResultsCODPropertyChanged;
            this.onResultsNestedCODCollectionChanged = onResultsNestedCODCollectionChanged;
            this.onResultsNestedCODPropertyChanged = onResultsNestedCODPropertyChanged;
            _termCOD1 = termCOD1;
            this.onTermCOD1CollectionChanged = onTermCOD1CollectionChanged;
            if (this.onTermCOD1CollectionChanged != null) _termCOD1.CollectionChanged += this.onTermCOD1CollectionChanged;
            this.onTermCOD1PropertyChanged = onTermCOD1PropertyChanged;
            if (this.onTermCOD1PropertyChanged != null) _termCOD1.PropertyChanged += this.onTermCOD1PropertyChanged;

            _sigIsReadyToCalculateAndStoreCOD = sigIsReadyToCalculateAndStoreCOD;
            this.onSigIsReadyToCalculateAndStoreCODCollectionChanged = onSigIsReadyToCalculateAndStoreCODCollectionChanged;
            if (this.onSigIsReadyToCalculateAndStoreCODCollectionChanged != null) _sigIsReadyToCalculateAndStoreCOD.CollectionChanged += this.onSigIsReadyToCalculateAndStoreCODCollectionChanged;
            this.onSigIsReadyToCalculateAndStoreCODPropertyChanged = onSigIsReadyToCalculateAndStoreCODPropertyChanged;
            if (this.onSigIsReadyToCalculateAndStoreCODPropertyChanged != null) _sigIsReadyToCalculateAndStoreCOD.PropertyChanged += this.onSigIsReadyToCalculateAndStoreCODPropertyChanged;

            _isFetchingIndividualTermKeyCOD = isFetchingIndividualTermKeyCOD;
            this.onIsFetchingIndividualTermKeyCODCollectionChanged = onIsFetchingIndividualTermKeyCODCollectionChanged;
            if (this.onIsFetchingIndividualTermKeyCODCollectionChanged != null) _isFetchingIndividualTermKeyCOD.CollectionChanged += this.onIsFetchingIndividualTermKeyCODCollectionChanged;
            this.onIsFetchingIndividualTermKeyCODPropertyChanged = onIsFetchingIndividualTermKeyCODPropertyChanged;
            if (this.onIsFetchingIndividualTermKeyCODPropertyChanged != null) _isFetchingIndividualTermKeyCOD.PropertyChanged += this.onIsFetchingIndividualTermKeyCODPropertyChanged;
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

        public ConcurrentObservableDictionary<string, Task> IsFetchingIndividualTermKey { get => _isFetchingIndividualTermKeyCOD; set => _isFetchingIndividualTermKeyCOD =
            value; }

        public NotifyCollectionChangedEventHandler OnIsFetchingIndividualTermKeyCODCollectionChanged
        { get => onIsFetchingIndividualTermKeyCODCollectionChanged; set => onIsFetchingIndividualTermKeyCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnIsFetchingIndividualTermKeyCODPropertyChanged { get => onIsFetchingIndividualTermKeyCODPropertyChanged; set => onIsFetchingIndividualTermKeyCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnResultsCODCollectionChanged { get => onResultsCODCollectionChanged; set => onResultsCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnResultsCODPropertyChanged { get => onResultsCODPropertyChanged; set => onResultsCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnResultsNestedCODCollectionChanged { get => onResultsNestedCODCollectionChanged; set => onResultsNestedCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnResultsNestedCODPropertyChanged { get => onResultsNestedCODPropertyChanged; set => onResultsNestedCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnSigIsReadyToCalculateAndStoreCODCollectionChanged { get => onSigIsReadyToCalculateAndStoreCODCollectionChanged; set => onSigIsReadyToCalculateAndStoreCODCollectionChanged =
            value; }

        public PropertyChangedEventHandler OnSigIsReadyToCalculateAndStoreCODPropertyChanged { get => onSigIsReadyToCalculateAndStoreCODPropertyChanged; set => onSigIsReadyToCalculateAndStoreCODPropertyChanged =
            value; }

        public NotifyCollectionChangedEventHandler OnTermCOD1CollectionChanged { get => onTermCOD1CollectionChanged; set => onTermCOD1CollectionChanged =
            value; }

        public PropertyChangedEventHandler OnTermCOD1PropertyChanged { get => onTermCOD1PropertyChanged; set => onTermCOD1PropertyChanged =
            value; }

        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> ResultsCOD { get => _resultsCOD; set => _resultsCOD =
            value; }

        public ConcurrentObservableDictionary<string, byte> SigIsReadyToCalculateAndStore { get => _sigIsReadyToCalculateAndStoreCOD; set => _sigIsReadyToCalculateAndStoreCOD =
            value; }

        public ConcurrentObservableDictionary<string, double> TermCOD1 { get => _termCOD1; set => _termCOD1 =
            value; }

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
            TermCOD1.CollectionChanged -= onTermCOD1CollectionChanged;
            TermCOD1.PropertyChanged -= onTermCOD1PropertyChanged;
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
