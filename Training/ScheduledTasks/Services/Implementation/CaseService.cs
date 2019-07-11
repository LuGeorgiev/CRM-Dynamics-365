using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Connection;
using log4net;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Models.Case;
using XrmContext;

namespace Services.Implementation
{
    public class CaseService : ICaseService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ICrmConnection crmConnection;

        public CaseService(ICrmConnection crmConnection)
        {
            this.crmConnection = crmConnection;
        }


        public IEnumerable<CaseRetreiveModel> AllActiveByTypeAndAccountId(HashSet<string> accountIds, int caseTypeCode, string caseTitle)
        {
            log.Info($"{nameof(AllActiveByTypeAndAccountId)} started. Input info is: CaseTypeCode: {caseTypeCode}, CaseTitle: {caseTitle}, Number of Ids for comparing: {accountIds.Count()}");
            try
            {
                using (var context = crmConnection.GetContext())
                {
                    //Create Option set value depending on Input
                    var targetType = new OptionSetValue(caseTypeCode);

                    var casesToSolve = context.IncidentSet
                                    .Where(c => c.CaseTypeCode == targetType
                                            && c.Title == caseTitle)
                                    //&& accountIds.Contains(c.CustomerId.Id.ToString()))
                                    .Select(c => new CaseRetreiveModel
                                    {
                                        Id = c.IncidentId.ToString(),
                                        CaseCustomer = c.CustomerId.Id.ToString(),
                                        Title = c.Title
                                    })
                                    .ToList();
                    log.Info($"Cases retrieved {casesToSolve.Count}");

                    //Check if those casese belongs to Accounts in input
                    //This is something that I donot understand in previous query if uncomment every time there is exception for invalid Where condition
                    // Below the condition is EXACTLY the same and works perfectly
                    casesToSolve = casesToSolve
                        .Where(x => accountIds.Contains(x.CaseCustomer))
                        .ToList();

                    log.Info($"Cases filtered with input customer Ids {casesToSolve.Count}");

                    return casesToSolve;
                }

            }
            catch (Exception ex)
            {
                log.Error($"{nameof(AllActiveByTypeAndAccountId)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return Enumerable.Empty<CaseRetreiveModel>();
            }
        }

        public bool ResolveCase(string incidentId)
        {
            log.Info($"{nameof(ResolveCase)} started. Case to resolve Id is {incidentId}");
            try
            {

                var incidentResolution = new IncidentResolution
                {
                    Subject = "Resolve Request Incident",
                    IncidentId = new EntityReference(Incident.EntityLogicalName, Guid.Parse(incidentId))
                };

                var closeIncidentRequest = new CloseIncidentRequest
                {
                    IncidentResolution = incidentResolution,
                    RequestName = "Resolve Case",
                    Status = new OptionSetValue(5) //resolve case
                };

                var response = crmConnection.Service.Execute(closeIncidentRequest);
                log.Info("Incident Closed");

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(ResolveCase)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return false;
            }
        }
    }
}
