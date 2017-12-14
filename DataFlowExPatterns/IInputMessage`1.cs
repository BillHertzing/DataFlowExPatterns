using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    public interface IInputMessage<ITStoreP, ITSolveP>
    {
        ITSolveP SolveP { get; }
        ITStoreP StoreP { get; }
    }
}

    /*
    public interface IInputMessage<ITStoreP, ITTerms, TKey, TValue> :  IOrderedEnumerable<ITerm<TKey, TValue>>
    {
        IOrderedEnumerable<ITerm<TKey, TValue>> Terms { get; }
        //(string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) Value { get; }
    }
}

public interface ITStoreP
{
    (string k1, string k2) StoreKeys { get; }
    
}


public interface ITerm<TKey, TValue>
{
    ImmutableDictionary<TKey, TValue> Elements { get; }
    //(string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) Value { get; }
}
*/
