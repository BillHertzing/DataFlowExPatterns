using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ATAP.DataFlowExPatterns.SolveAndStoreFromInputAndAsyncTerms
{
    public interface IWebGet
    {
        Task<T> AsyncWebGet<T>(string reqID);
    }
}
