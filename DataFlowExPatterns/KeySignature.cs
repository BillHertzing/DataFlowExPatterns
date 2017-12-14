using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    public abstract partial class SolveAndStoreFromInputAndAsyncTerms<ITStoreP, ITSolveP, TResult>
    {
        public class KeySignature<T> {
            readonly IEnumerable<T> _individualElements;
            readonly ImmutableHashSet<T> _sigHashSet;

            public KeySignature(IEnumerable<T> inputCollection)  {
                _individualElements = inputCollection;
                _sigHashSet = ImmutableHashSet.Create<T>().Union(inputCollection);
            }
            public IEnumerable<T> IndividualElements { get => _individualElements; }
            ImmutableHashSet<T> SigHashSet { get => _sigHashSet; }
        }
    }
}
