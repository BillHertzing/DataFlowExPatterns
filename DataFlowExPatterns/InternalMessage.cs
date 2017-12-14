using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    public abstract partial class SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult>
    {

        internal class InternalMessage : InputMessage<ITStoreP, ITSolveP> , IInternalMessage
        {
            private readonly KeySignature<string> _sig;
            private readonly bool _isReadyToSolve;
            //(string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToSolve) _value;

            public InternalMessage(ITStoreP storeP, ITSolveP solveP, KeySignature<string> sig, bool isReadyToSolve) :base (storeP, solveP)
            {
                this._sig=sig;
                this._isReadyToSolve = isReadyToSolve;
            }
            public KeySignature<string> sig { get { return _sig; } }
            public bool IsReadyToSolve { get { return _isReadyToSolve; } }
    }
    }
}
