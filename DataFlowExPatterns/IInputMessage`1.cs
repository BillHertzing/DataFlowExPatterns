using System;
using System.Collections.Generic;


namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {
    public interface IInputMessage<TKeyTerm1> {
        (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1) Value { get; }
    }
}
