namespace Models
{
    public static class Constats
    {
        public const string TEST = "Test";

        public const string OPEN_REGISTRATION_STATUS = "Open";
        public const string CLOSED_REGISTRATION_STATUS = "Closed";
        public const string CANCELLED_REGISTRATION_STATUS = "Cancelled";
        public const string REGISTERD_REGISTRATION_SUB_STATUS = "Registered";
        public const string CANCELLED_REGISTRATION_SUB_STATUS = "Cancelled by Task";

        public const int CASE_TYPE_REQUEST = 3;
        public const string CASE_TITLE_CANCEL_REGISTRATION = "Cancel one registration";

        public const string REGISTRATION_TASK_DATE_SLUG = "registration-task-run-date";
        public const string REGISTRATION_TASK_OWNERS_SLUG = "registration-task-owners";

        public const string SUCCESS_REPORT_HEAD = @"<table>
                                                          <tr>
                                                            <th>Account name</th>
                                                            <th>Registration name</th> 
                                                            <th>Case name</th>
                                                          </tr>";
        public const string FAIL_REPORT_HEAD = @"<table>
                                                          <tr>
                                                            <th>Account name</th>
                                                            <th>Registration name</th> 
                                                            <th>Description</th>
                                                          </tr>";
        public const string TABLE_ROW = @"<tr>
                                            <td>{0}</td>
                                            <td>{1}</td> 
                                            <td>{2}</td>
                                          </tr>";

        public const string TABLE_END = "</table>";
        public const char SEPARATOR = '~';
        public const string ACTIVITY_PARTY = "activityparty";
        public const string PARTY_ID = "partyid";
        public const string ADDRESS_USED = "addressused";

        public const long DEFAULT_DATETIME = 100;
    }
}
