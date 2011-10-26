using System.Data.Common;
using System.Data.SqlClient;
using MvcMiniProfiler;
using MvcMiniProfiler.Data;

namespace NHibernate.MvcMini
{
    using SqlCommand = System.Data.SqlClient.SqlCommand;

    public class ProfiledSqlDbCommand : ProfiledDbCommand
    {
        public ProfiledSqlDbCommand(SqlCommand cmd, SqlConnection conn, MiniProfiler profiler)
            : base(cmd, conn, profiler)
        {
            Command = cmd;
        }

        public SqlCommand Command { get; set; }

        private DbTransaction _trans;

        protected override DbTransaction DbTransaction
        {
            get { return _trans; }
            set
            {
                this._trans = value;
                ProfiledSqlDbTransaction awesomeTran = value as ProfiledSqlDbTransaction;
                Command.Transaction = awesomeTran == null ? (SqlTransaction)value : awesomeTran.Transaction;
            }
        }
    }
}