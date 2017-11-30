using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ATAP.DataFlowExPatterns.CalculateAndStoreFromInputAndAsyncTerms
{
    public interface IWebGet
    {
        Task<double> GetHRAsync(string c);
    }
}
