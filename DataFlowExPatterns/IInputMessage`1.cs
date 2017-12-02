using System;
using System.Collections.Generic;


namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    public interface IInputMessage<TKeyTerm1,IValueTerm1> {
        (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, IValueTerm1> terms1) Value { get; }
    }
}
