using System;
using System.Linq;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms {


    public class InputMessage<ITStoreP, ITSolveP> : IInputMessage<ITStoreP, ITSolveP>
    {
        readonly ITSolveP _solveP;
        readonly ITStoreP _storeP;

        // private readonly IOrderedEnumerable<ITerm<TKey, TValue>> _terms;
        //     (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) _value;
        /*
        public InputMessage(ITStoreP store,  IOrderedEnumerable<ITerm<TKey, TValue>> terms)
        {
        _storeKeys = store;
        _terms = terms;
        }
        */
        public InputMessage(ITStoreP storeP, ITSolveP solveP)
        {
            _storeP = storeP;
            _solveP = solveP;
        }

        public ITSolveP SolveP { get { return _solveP; } }

        public ITStoreP StoreP { get { return _storeP; } }
        //        public IOrderedEnumerable<ITerm<TKey, TValue>> Terms { get { return _terms; } }
        #region IEnumerable<T>
        #endregion IEnumerable<T>
    }
    /*
    public class InputTermCollection<ITerm<string, (double,double,double,TimeSpan)>> : IOrderedEnumerable<ITerm<string, (double, double, double, TimeSpan)>>
    {
    #region IEnumerable<T>

    public IEnumerator<ITerm<TKey, TValue>> GetEnumerator()
    {
    return vector.Take(count).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
    {
    throw new NotImplementedException();
    }
    #endregion IEnumerable<T>
    #region IEnumerable Members
    System.Collections.IEnumerator
    System.Collections.IEnumerable.GetEnumerator()
    {
    return GetEnumerator();
    }
    #endregion


    }
    */
}


