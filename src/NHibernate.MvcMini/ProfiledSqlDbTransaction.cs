using System.Data.SqlClient;
using MvcMiniProfiler.Data;

namespace NHibernate.MvcMini
{
    public class ProfiledSqlDbTransaction : ProfiledDbTransaction
    {
        public ProfiledSqlDbTransaction(SqlTransaction transaction, ProfiledDbConnection connection)
            : base(transaction, connection)
        {
            Transaction = transaction;
        }

        public SqlTransaction Transaction { get; set; }
    }
}