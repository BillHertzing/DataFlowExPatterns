using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Swordfish.NET.Collections;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms
{
    public partial class CalculateAndStoreFromInputAndAsyncTerms  {
        public class InternalMessage<TKeyTerm1> : IInternalMessage<TKeyTerm1>
        {
            (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) _value;

            public InternalMessage((string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) value)
            {
                _value = value;
            }

            public (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, double> terms1, KeySignature<string> sig, bool isReadyToCalculate) Value
            {
                get => _value; set => _value =
value;
            }
        }
    }
}
