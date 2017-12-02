using System;
using System.Collections.Generic;
using System.Linq;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms {

    /// <summary>
    /// Class InputMessage.
    /// </summary>
    /// <typeparam name="TKeyTerm1"></typeparam>
    /// <seealso cref="DataFlowExPatterns.IInputMessage{TKeyTerm1}" />
    public class InputMessage<TKeyTerm1,TValueTerm1> : IInputMessage<TKeyTerm1, TValueTerm1>
    {
        /// <summary>
        /// The value backing field
        /// </summary>
        (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) _value;

        public InputMessage((string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) value)
        {
            _value = value;
        }
        public (string k1, string k2, IReadOnlyDictionary<TKeyTerm1, TValueTerm1> terms1) Value
        {
            get => _value; 
        }
    }
}
