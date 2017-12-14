using System.Collections.Generic;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    public abstract partial class SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult>
    {
        internal interface IInternalMessage : IInputMessage<ITStoreP, ITSolveP>
        {

            //(string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToSolve) Value { get; set; }

            KeySignature<string> sig { get; }
            bool IsReadyToSolve { get; }
        }
    }
}
