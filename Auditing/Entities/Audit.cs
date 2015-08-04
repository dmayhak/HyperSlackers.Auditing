using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary>
    /// An audit entry tracks a group of changes that were committed to the database in one
    /// transaction. Each change has an <see cref="AuditItem" /> in the <c>AuditItems</c>
    /// List.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class Audit<TKey>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        [Index("IX_Id_Date", 0)]
        [Index("IX_Date_Id", 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the host identifier. Only used for multi-host systems.
        /// </summary>
        /// <value>
        /// The host identifier.
        /// </value>
        public TKey HostId { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        public TKey UserId { get; set; }
        /// <summary>
        /// Gets or sets the name of the host. Only used for multi-host systems.
        /// Computer name for a windows app, host name for web app
        /// </summary>
        /// <value>
        /// The name of the host.
        /// </value>
        [MaxLength(256)]
        public string HostName { get; set; }
        /// <summary>
        /// Gets or sets the name of the user. From windows identity or web identity.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [MaxLength(256)]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the date the audit record was created.
        /// </summary>
        /// <value>
        /// The audit date.
        /// </value>
        [Required]
        [Index("IX_Id_Date", 1)]
        [Index("IX_Date_Id", 0)]
        public DateTime AuditDate { get; set; }
    }

    public class AuditGuid : Audit<Guid>
    {

    }

    public class AuditInt : Audit<int>
    {

    }

    public class AuditLong : Audit<long>
    {

    }
}
