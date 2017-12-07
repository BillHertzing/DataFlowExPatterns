using System.Collections.Generic;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms
{
    public partial class CalculateAndStoreFromInputAndAsyncTerms  {
        public interface IInternalMessage<TKeyTerm1>
        {
            (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) Value { get; set; }
        }// The class used as the message between the _acceptor, The _DynamicBuffers, and the _bSolveStore
    }
}
