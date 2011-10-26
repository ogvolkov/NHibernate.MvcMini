using NHibernate.AdoNet;
using NHibernate.Engine;

namespace NHibernate.MvcMini
{
    public class ProfiledSqlClientBatchingBatcherFactory : IBatcherFactory
    {
        public virtual IBatcher CreateBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
        {
            return new ProfiledSqlClientBatchingBatcher(connectionManager, interceptor);
        }
    }
}