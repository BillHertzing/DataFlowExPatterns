using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using Swordfish.NET.Collections;
using System.Collections.Generic;

namespace DataObjects
{

    public class Rand1Term : IDisposable
    {
        NotifyCollectionChangedEventHandler onRCollectionChanged;
        NotifyCollectionChangedEventHandler onRNestedCollectionChanged;
        PropertyChangedEventHandler onRNestedPropertyChanged;
        PropertyChangedEventHandler onRCollectionPropertyChanged;
        public ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>> rCOD;
        NotifyCollectionChangedEventHandler onTerm1CollectionChanged;
        PropertyChangedEventHandler onTerm1PropertyChanged;
        public ConcurrentObservableDictionary<string, double> term1COD;

        public Rand1Term(NotifyCollectionChangedEventHandler OnRCollectionChanged, PropertyChangedEventHandler OnRCollectionPropertyChanged, NotifyCollectionChangedEventHandler OnRNestedCollectionChanged, PropertyChangedEventHandler OnNestedPropertyChanged, NotifyCollectionChangedEventHandler OnTerm1CollectionChanged, PropertyChangedEventHandler OnTerm1PropertyChanged)
        {
            rCOD = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onRCollectionChanged = OnRCollectionChanged;
            this.onRCollectionPropertyChanged = OnRCollectionPropertyChanged;
            this.onRNestedPropertyChanged = OnNestedPropertyChanged;
            rCOD.CollectionChanged += onRCollectionChanged;
            rCOD.PropertyChanged += onRCollectionPropertyChanged;
            term1COD = new ConcurrentObservableDictionary<string, double>();
            this.onTerm1CollectionChanged = OnTerm1CollectionChanged;
            this.onTerm1PropertyChanged = OnTerm1PropertyChanged;
            term1COD.CollectionChanged += OnTerm1CollectionChanged;
            term1COD.PropertyChanged += OnTerm1PropertyChanged;
        }
        public Rand1Term(NotifyCollectionChangedEventHandler OnRCollectionChanged, NotifyCollectionChangedEventHandler OnRNestedCollectionChanged, NotifyCollectionChangedEventHandler OnTerm1CollectionChanged)
        {
            rCOD = new ConcurrentObservableDictionary<string, ConcurrentObservableDictionary<string, decimal>>();
            this.onRCollectionChanged = OnRCollectionChanged;
            this.onRNestedCollectionChanged = OnRNestedCollectionChanged;
            rCOD.CollectionChanged += onRCollectionChanged;
            term1COD = new ConcurrentObservableDictionary<string, double>();
            this.onTerm1CollectionChanged = OnTerm1CollectionChanged;
            term1COD.CollectionChanged += OnTerm1CollectionChanged;
        }

        public void RecordR(string k1, string k2, decimal pr)
        {
            if (rCOD.ContainsKey(k1))
            {
                var innerCOD = rCOD[k1];
                if (innerCOD.ContainsKey(k2))
                {
                    throw new NotSupportedException("This pattern expects only one entry per k1k2 pair");
                }
                else
                {
                    //ToDo: Better understanding/handling of exceptions here
                    try { innerCOD.Add(k2, pr); } catch { new Exception($"adding {pr} to {k1}'s innerDictionary keyed by {k2} failed"); }
                }
            }
            else
            {
                var innerCOD = new ConcurrentObservableDictionary<string, decimal>();
                if (this.onRNestedCollectionChanged != null) innerCOD.CollectionChanged += this.onRNestedCollectionChanged;
                if (this.onRNestedPropertyChanged != null) innerCOD.PropertyChanged += this.onRNestedPropertyChanged;
                try { innerCOD.Add(k2, pr); } catch { new Exception($"adding {pr} to the new innerDictionary keyed by {k2} failed"); }
                try { rCOD.Add(k1, innerCOD); } catch { new Exception($"adding the new innerDictionary to cODR keyed by {k1} failed"); }
            };
        }



        #region IDisposable Support
        public void TearDown()
        {
            rCOD.CollectionChanged -= onRCollectionChanged;
            rCOD.PropertyChanged -= onRCollectionPropertyChanged;
            var enumerator = rCOD.Keys.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var key = enumerator.Current;
                    rCOD[key].CollectionChanged -= this.onRNestedCollectionChanged;
                    rCOD[key].PropertyChanged -= this.onRNestedPropertyChanged;
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            term1COD.CollectionChanged -= onTerm1CollectionChanged;
            term1COD.PropertyChanged -= onTerm1PropertyChanged;
        }

        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
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
        // ~Rand1Term() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
