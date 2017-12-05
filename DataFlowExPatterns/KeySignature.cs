using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms
{
    public partial class CalculateAndStoreFromInputAndAsyncTerms
    {
        public class KeySignature<T> {
            static string defaultSignatureDelimiter = ",";
            IEnumerable<T> _individualElements;
            string _longest;
            Func<IEnumerable<T>, string> _longestKeySignatureFunction;
            string _signatureDelimiter;

            public KeySignature(IEnumerable<T> inputCollection) : this(inputCollection,
                                                                       defaultSignatureDelimiter,
                                                                       defaultLongestKeySignatureFunction) {
            }

            public KeySignature(IEnumerable<T> inputCollection, string signatureDelimiter, Func<IEnumerable<T>, string> longestKeySignatureFunction) {
                _signatureDelimiter = signatureDelimiter;
                _longestKeySignatureFunction = longestKeySignatureFunction;
                _individualElements = inputCollection;
                _longest = _longestKeySignatureFunction(inputCollection);
            }

            static string defaultLongestKeySignatureFunction(IEnumerable<T> _inputCollection) {
                return _inputCollection.OrderBy(x => x)
                           .Aggregate(new StringBuilder(),
                                      (current, next) => current.Append(defaultSignatureDelimiter)
                                                             .Append(next.ToString()))
                           .ToString();
            }

            public string Longest() {
                return _longest;
            }

            public IEnumerable<T> IndividualElements { get => _individualElements; }
        }
    }
}
