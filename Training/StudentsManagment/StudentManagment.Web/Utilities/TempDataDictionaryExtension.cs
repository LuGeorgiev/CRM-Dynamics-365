using System.Web.Mvc;

namespace StudentManagment.Web.Utilities
{
    public static class TempDataDictionaryExtension
    {
        public static void AddSuccessMessage(this TempDataDictionary tempData, string msg)
        {
            tempData[WebConstants.TempDataSuccessMsgKey] = msg;
        }

        public static void AddErrorMessage(this TempDataDictionary tempData, string msg)
        {
            tempData[WebConstants.TempDataErrorMsgKey] = msg;
        }
    }
}