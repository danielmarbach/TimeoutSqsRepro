using System;
using System.Data;
using NHibernate.SqlTypes;
using NHibernate.Type;

namespace TimeoutMigration
{
    [Serializable]
    class NSBDateTimeType : AbstractDateTimeType
    {
        public NSBDateTimeType()
            : base(new DateTimeSqlType())
        {
        }

        public override string Name => "DateTime";

        /// <summary>
        /// Describes the details of a <see cref="F:System.Data.DbType.DateTime" />.
        /// </summary>
        [Serializable]
        class DateTimeSqlType : SqlType
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="T:NHibernate.SqlTypes.DateTimeSqlType" /> class.
            /// </summary>
            public DateTimeSqlType()
                : base(DbType.DateTime)
            {
            }
        }
    }
}