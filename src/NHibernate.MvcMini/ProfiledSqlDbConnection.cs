using System.Data.Common;
using System.Data.SqlClient;
using MvcMiniProfiler;
using MvcMiniProfiler.Data;

namespace NHibernate.MvcMini
{
    public class ProfiledSqlDbConnection : ProfiledDbConnection
    {
        public ProfiledSqlDbConnection(SqlConnection connection, MiniProfiler profiler)
            : base(connection, profiler)
        {
            Connection = connection;
        }

        public SqlConnection Connection { get; set; }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return new ProfiledSqlDbTransaction(Connection.BeginTransaction(isolationLevel), this);
        }
    }
}