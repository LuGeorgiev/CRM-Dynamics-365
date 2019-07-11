using Connection;
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using XrmContext;

using static Models.Constats;
using log4net;

namespace Services.Implementation
{
    public class EmailService : IEmailService
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICrmConnection crmConnection;

        public EmailService(ICrmConnection crmConnection)
        {
            this.crmConnection = crmConnection;
        }

        public bool SentInformation(Guid toContactGuid)
        {
            //TODO
            throw new NotImplementedException();
        }

        public bool SentRegistrationReport(IEnumerable<string> reportingEmails, IEnumerable<string> successfulOperations, IEnumerable<string> failedOperations)
        {
            log.Info($"{nameof(SentRegistrationReport)} Started");
            try
            {
                using (var context = crmConnection.GetContext())
                {
                    //Success TABLE Creation
                    string successTable = SUCCESS_REPORT_HEAD;
                    successTable = SplitLine(successfulOperations, successTable);

                    //Fail TABLE Creation
                    string failTable = FAIL_REPORT_HEAD;
                    failTable = SplitLine(failedOperations, failTable);


                    //Forming TO party
                    //EntityCollection toActivityParty = new EntityCollection();
                    var toActivityParty = new List<ActivityParty>();
                    foreach (var currentEmail in reportingEmails)
                    {
                        var partyTosentMail = new ActivityParty();
                        partyTosentMail.AddressUsed = currentEmail;

                        toActivityParty.Add(partyTosentMail);
                    }

                    //Form FROM party                
                    var fromActivityParty = new ActivityParty();
                    fromActivityParty.PartyId = new EntityReference("systemuser", crmConnection.GetUserId);
                    fromActivityParty.AddressUsed = "admin@plugin.com";

                    //Sent Email
                    var email = new Email();
                    email.From = new List<ActivityParty>() { fromActivityParty };
                    email.To = toActivityParty;
                    email.Subject = "Report on Request task Id:";
                    email.Description = $"Dear Colleagues, " +
                        $"{Environment.NewLine} " +
                        $"Registrations updated: {successfulOperations.Count()} " +
                        $"{Environment.NewLine} " +
                        $"{successTable} " +
                        $"{Environment.NewLine}{Environment.NewLine} " +
                        $"Issues found: {failedOperations.Count()} " +
                        $"{Environment.NewLine} {Environment.NewLine} " +
                        $"{failTable} " +
                        $"{Environment.NewLine} " +
                        $"Yeahhhhh, UGLY but sent !!";
                    email.DirectionCode = true;

                    Guid emailId = crmConnection.Service.Create(email);
                    if (emailId == Guid.Empty)
                    {
                        log.Error("Reporting email was NOT created sucessfully");
                        return false;
                    }
                    else
                    {
                        log.Info("Reporting email created sucessfully");
                    }

                    SendEmailRequest sendEmailRequest = new SendEmailRequest
                    {
                        EmailId = emailId,
                        TrackingToken = "",
                        IssueSend = true
                    };

                    SendEmailResponse sendEmailresp = (SendEmailResponse)crmConnection.Service.Execute(sendEmailRequest);
                    log.Info("Report email sent sucessfully");

                    return true;
                }
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(SentRegistrationReport)} throws exception: {ex.Message}");
                Console.WriteLine(ex.Message);

                return false;
            }

        }

        private static string SplitLine(IEnumerable<string> lines, string table)
        {
            foreach (var line in lines)
            {
                var tokens = line.Split(SEPARATOR)
                    .Where(x => x.Length > 0)
                    .ToArray();

                table += string.Format(TABLE_ROW, tokens[0], tokens[1], tokens[2]);
            }

            table += TABLE_END;

            return table;
        }
    }
}
