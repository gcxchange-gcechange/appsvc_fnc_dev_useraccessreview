using Microsoft.WindowsAzure.Storage.Table;

namespace appsvc_fnc_dev_useraccessreview
{
    public class userTable
    {
        public string Id { get; set; }
        public string UPN { get; set; }
        public string LastCall { get; set; }
    }

    public class Person
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
    }



    public class PersonEntity : TableEntity
    {
        public PersonEntity(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }

        public PersonEntity() { }
        public string First_Name_VC { get; set; }
        public string Last_Name_VC { get; set; }
        public string Email_VC { get; set; }
    }

    public class YourEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}
