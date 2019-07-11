using Models.Case;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public interface ICaseService
    {
        IEnumerable<CaseRetreiveModel> AllActiveByTypeAndAccountId(HashSet<string> accountIds,int caseTypeCode, string caseTitle);

        bool ResolveCase(string incidentId);
    }
}
