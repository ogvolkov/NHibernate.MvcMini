using System.Data;
using System.Data.SqlClient;
using MvcMiniProfiler;
using NHibernate.AdoNet;
using NHibernate.Driver;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.SqlTypes;

namespace NHibernate.MvcMini
{
    /// <summary>
    /// We use this sql driver when enable profiling with MVC Mini Profiler
    /// Initial source code was taken from http://stackoverflow.com/questions/5676812/is-it-possibel-to-use-fluentnhibernate-with-an-odbc-connection
    /// </summary>
    public class ProfiledSqlClientDriver : DriverBase, IEmbeddedBatcherFactoryProvider
    {
        public override IDbConnection CreateConnection()
        {
            return new ProfiledSqlDbConnection(
                new SqlConnection(),
                MiniProfiler.Current);
        }

        public override IDbCommand CreateCommand()
        {
            return new ProfiledSqlDbCommand(
                new System.Data.SqlClient.SqlCommand(),
                null,
                MiniProfiler.Current);
        }

        public override bool UseNamedPrefixInSql
        {
            get { return true; }
        }

        public override bool UseNamedPrefixInParameter
        {
            get { return true; }
        }

        public override string NamedPrefix
        {
            get { return "@"; }
        }

        public override bool SupportsMultipleOpenReaders
        {
            get { return true; }
        }

        public static void SetParameterSizes(IDataParameterCollection parameters, SqlType[] parameterTypes)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                SetVariableLengthParameterSize((IDbDataParameter)parameters[i], parameterTypes[i]);
            }
        }

        private const int MaxAnsiStringSize = 8000;
        private const int MaxBinarySize = MaxAnsiStringSize;
        private const int MaxStringSize = MaxAnsiStringSize / 2;
        private const int MaxBinaryBlobSize = int.MaxValue;
        private const int MaxStringClobSize = MaxBinaryBlobSize / 2;
        private const byte MaxPrecision = 28;
        private const byte MaxScale = 5;
        private const byte MaxDateTime2 = 8;
        private const byte MaxDateTimeOffset = 10;

        private static void SetDefaultParameterSize(IDbDataParameter dbParam, SqlType sqlType)
        {
            switch (dbParam.DbType)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                    dbParam.Size = MaxAnsiStringSize;
                    break;

                case DbType.Binary:
                    if (sqlType is BinaryBlobSqlType)
                    {
                        dbParam.Size = MaxBinaryBlobSize;
                    }
                    else
                    {
                        dbParam.Size = MaxBinarySize;
                    }
                    break;
                case DbType.Decimal:
                    dbParam.Precision = MaxPrecision;
                    dbParam.Scale = MaxScale;
                    break;
                case DbType.String:
                case DbType.StringFixedLength:
                    dbParam.Size = IsText(dbParam, sqlType) ? MaxStringClobSize : MaxStringSize;
                    break;
                case DbType.DateTime2:
                    dbParam.Size = MaxDateTime2;
                    break;
                case DbType.DateTimeOffset:
                    dbParam.Size = MaxDateTimeOffset;
                    break;
            }
        }

        private static bool IsText(IDbDataParameter dbParam, SqlType sqlType)
        {
            return (sqlType is StringClobSqlType) || (sqlType.LengthDefined && sqlType.Length > SqlClientDriver.MaxSizeForLengthLimitedString &&
                                                      (DbType.String == dbParam.DbType || DbType.StringFixedLength == dbParam.DbType));
        }

        private static void SetVariableLengthParameterSize(IDbDataParameter dbParam, SqlType sqlType)
        {
            SetDefaultParameterSize(dbParam, sqlType);

            // Override the defaults using data from SqlType.
            if (sqlType.LengthDefined && !IsText(dbParam, sqlType))
            {
                dbParam.Size = sqlType.Length;
            }

            if (sqlType.PrecisionDefined)
            {
                dbParam.Precision = sqlType.Precision;
                dbParam.Scale = sqlType.Scale;
            }
        }

        public override IDbCommand GenerateCommand(CommandType type, SqlString sqlString, SqlType[] parameterTypes)
        {
            IDbCommand command = base.GenerateCommand(type, sqlString, parameterTypes);
            //if (IsPrepareSqlEnabled)
            {
                SetParameterSizes(command.Parameters, parameterTypes);
            }
            return command;
        }

        public override bool SupportsMultipleQueries
        {
            get { return true; }
        }

        #region IEmbeddedBatcherFactoryProvider Members

        System.Type IEmbeddedBatcherFactoryProvider.BatcherFactoryClass
        {
            get { return typeof(ProfiledSqlClientBatchingBatcherFactory); }
        }

        #endregion

        public override IResultSetsCommand GetResultSetsCommand(ISessionImplementor session)
        {
            return new BasicResultSetsCommand(session);
        } 
    }
}