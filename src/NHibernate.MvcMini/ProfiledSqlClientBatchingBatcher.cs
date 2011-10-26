using System;
using System.Data;
using System.Data.Common;
using System.Text;
using NHibernate.AdoNet;
using NHibernate.AdoNet.Util;
using NHibernate.Exceptions;
using NHibernate.Util;

namespace NHibernate.MvcMini
{
    public class ProfiledSqlClientBatchingBatcher : AbstractBatcher
    {
        private int batchSize;
        private int totalExpectedRowsAffected;
        private SqlClientSqlCommandSet currentBatch;
        private StringBuilder currentBatchCommandsLog;
        private readonly int defaultTimeout;

        public ProfiledSqlClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
            : base(connectionManager, interceptor)
        {
            batchSize = Factory.Settings.AdoBatchSize;
            defaultTimeout = PropertiesHelper.GetInt32(NHibernate.Cfg.Environment.CommandTimeout, NHibernate.Cfg.Environment.Properties, -1);

            currentBatch = CreateConfiguredBatch();
            //we always create this, because we need to deal with a scenario in which
            //the user change the logging configuration at runtime. Trying to put this
            //behind an if(log.IsDebugEnabled) will cause a null reference exception 
            //at that point.
            currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
        }

        public override int BatchSize
        {
            get { return batchSize; }
            set { batchSize = value; }
        }

        protected override int CountOfStatementsInCurrentBatch
        {
            get { return currentBatch.CountOfCommands; }
        }

        public override void AddToBatch(IExpectation expectation)
        {
            totalExpectedRowsAffected += expectation.ExpectedRowCount;
            IDbCommand batchUpdate = CurrentCommand;

            string lineWithParameters = null;
            var sqlStatementLogger = Factory.Settings.SqlStatementLogger;
            if (sqlStatementLogger.IsDebugEnabled || Log.IsDebugEnabled)
            {
                lineWithParameters = sqlStatementLogger.GetCommandLineWithParameters(batchUpdate);
                var formatStyle = sqlStatementLogger.DetermineActualStyle(FormatStyle.Basic);
                lineWithParameters = formatStyle.Formatter.Format(lineWithParameters);
                currentBatchCommandsLog.Append("command ")
                    .Append(currentBatch.CountOfCommands)
                    .Append(":")
                    .AppendLine(lineWithParameters);
            }
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Adding to batch:" + lineWithParameters);
            }
            currentBatch.Append(((ProfiledSqlDbCommand)batchUpdate).Command);

            if (currentBatch.CountOfCommands >= batchSize)
            {
                ExecuteBatchWithTiming(batchUpdate);
            }
        }

        protected void ProfiledPrepare(IDbCommand cmd)
        {
            try
            {
                IDbConnection sessionConnection = ConnectionManager.GetConnection();

                if (cmd.Connection != null)
                {
                    // make sure the commands connection is the same as the Sessions connection
                    // these can be different when the session is disconnected and then reconnected
                    if (cmd.Connection != sessionConnection)
                    {
                        cmd.Connection = sessionConnection;
                    }
                }
                else
                {
                    cmd.Connection = (sessionConnection as ProfiledSqlDbConnection).Connection;
                }

                ProfiledSqlDbTransaction trans = (ProfiledSqlDbTransaction)typeof(NHibernate.Transaction.AdoTransaction).InvokeMember("trans", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, ConnectionManager.Transaction, null);
                if (trans != null)
                    cmd.Transaction = trans.Transaction;
                Factory.ConnectionProvider.Driver.PrepareCommand(cmd);
            }
            catch (InvalidOperationException ioe)
            {
                throw new ADOException("While preparing " + cmd.CommandText + " an error occurred", ioe);
            }
        }

        protected override void DoExecuteBatch(IDbCommand ps)
        {
            Log.DebugFormat("Executing batch");
            CheckReaders();
            ProfiledPrepare(currentBatch.BatchCommand);
            if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
            {
                Factory.Settings.SqlStatementLogger.LogBatchCommand(currentBatchCommandsLog.ToString());
                currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
            }

            int rowsAffected;
            try
            {
                rowsAffected = currentBatch.ExecuteNonQuery();
            }
            catch (DbException e)
            {
                throw ADOExceptionHelper.Convert(Factory.SQLExceptionConverter, e, "could not execute batch command.");
            }

            Expectations.VerifyOutcomeBatched(totalExpectedRowsAffected, rowsAffected);

            currentBatch.Dispose();
            totalExpectedRowsAffected = 0;
            currentBatch = CreateConfiguredBatch();
        }

        private SqlClientSqlCommandSet CreateConfiguredBatch()
        {
            var result = new SqlClientSqlCommandSet();
            if (defaultTimeout > 0)
            {
                try
                {
                    result.CommandTimeout = defaultTimeout;
                }
                catch (Exception e)
                {
                    if (Log.IsWarnEnabled)
                    {
                        Log.Warn(e.ToString());
                    }
                }
            }

            return result;
        }
    }
}