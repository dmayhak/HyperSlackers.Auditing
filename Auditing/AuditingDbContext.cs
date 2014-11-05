using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyperSlackers.Auditing.Extensions;
using HyperSlackers.AspNet.Identity.EntityFramework.Entities;
using System.Data.Entity;
using System.Collections;
using System.Reflection;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperSlackers.AspNet.Identity.EntityFramework
{
    public class AuditingDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim, TAudit, TAuditItem> : IdentityDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>
        where TKey : IEquatable<TKey>
        where TUser : Microsoft.AspNet.Identity.EntityFramework.IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>, new()
        where TRole : Microsoft.AspNet.Identity.EntityFramework.IdentityRole<TKey, TUserRole>, new()
        where TUserLogin : Microsoft.AspNet.Identity.EntityFramework.IdentityUserLogin<TKey>, new()
        where TUserRole : Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole<TKey>, new()
        where TUserClaim : Microsoft.AspNet.Identity.EntityFramework.IdentityUserClaim<TKey>, new()
        where TAudit : Audit<TKey>, new()
        where TAuditItem : AuditItem<TKey>, new()
    {
        private bool disableAuditing = false;
        public TKey HostId { get; private set; } // only used for multi-host systems
        public string HostName { get; private set; }
        public TKey UserId { get; private set; }
        public string UserName { get; private set; }

        // table/schema renaming
        public virtual string AuditSchemaName { get { return ""; } }
        public virtual string AuditsTableName { get { return "AspNetAudits"; } }
        public virtual string AuditItemsTableName { get { return "AspNetAuditItems"; } }
        public virtual string AuditPropertiesTableName { get { return "AspNetAuditProperties"; } }

        // audit tracking
        private TAudit currentAudit;
        private List<TAuditItem> currentAuditItems;
        private List<AuditProperty> currentAuditProperties;

        // audit dbsets
        public DbSet<TAudit> Audits { get; set; }
        public DbSet<TAuditItem> AuditItems { get; set; }
        public DbSet<AuditProperty> AuditProperties { get; set; }

        protected AuditingDbContext(bool disableAuditing = false)
            : this("DefaultConnection", disableAuditing)
        {
        }

        protected AuditingDbContext(string nameOrConnectionString, bool disableAuditing = false)
            : base(nameOrConnectionString)
        {
            Contract.Requires<ArgumentNullException>(!nameOrConnectionString.IsNullOrWhiteSpace(), "nameOrConnectionString");

            this.disableAuditing = disableAuditing;
        }

        protected virtual TKey GetHostId()
        {
            return default(TKey);
        }

        protected virtual string GetHostName()
        {
            try
            {
                System.Web.HttpContext context = System.Web.HttpContext.Current;
                if (context != null)
                {
                    // web application
                    return context.Request.Url.Host;
                }
                else
                {
                    // windows application
                    var user = System.Security.Principal.WindowsIdentity.GetCurrent();

                    if (user.Name.Contains("\\"))
                    {
                        return "<" + user.Name.Split('\\')[0] + ">";
                    }
                }
            }
            catch (Exception ex)
            {
                // hopefully we only hit this when creating the migration and db does not exist
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return "<system>";
        }

        protected virtual TKey GetUserId()
        {
            var userName = GetUserName();

            if (!userName.IsNullOrWhiteSpace() && userName != "<system>")
            {
                var user = this.Set<TUser>().SingleOrDefault(u => u.UserName.ToUpper() == userName.ToUpper());
                if (user != null)
                {
                    return user.Id;
                }
            }

            // TODO: windows

            return default(TKey);
        }

        protected virtual string GetUserName()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            if (context != null)
            {
                // web application
                if (context.User != null)
                {
                    return context.User.Identity.Name;
                }
            }
            else
            {
                // windows application
                var user = System.Security.Principal.WindowsIdentity.GetCurrent();

                if (user.Name.Contains("\\"))
                {
                    return user.Name.Substring(user.Name.IndexOf("\\") + 1);
                }

                return user.Name;
            }

            return "<system>";
        }

        /// <summary>
        /// Maps table names, and sets up relationships between the various user entities.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // auditing tables
            modelBuilder.Entity<TAudit>()
                .ToTable((AuditsTableName.IsNullOrWhiteSpace() ? "AspNetAudits" : AuditsTableName), (AuditSchemaName.IsNullOrWhiteSpace() ? "dbo" : AuditSchemaName));
            modelBuilder.Entity<TAuditItem>()
                 .ToTable((AuditItemsTableName.IsNullOrWhiteSpace() ? "AspNetAuditItems" : AuditItemsTableName), (AuditSchemaName.IsNullOrWhiteSpace() ? "dbo" : AuditSchemaName));
            modelBuilder.Entity<AuditProperty>()
                 .ToTable((AuditPropertiesTableName.IsNullOrWhiteSpace() ? "AspNetAuditProperties" : AuditPropertiesTableName), (AuditSchemaName.IsNullOrWhiteSpace() ? "dbo" : AuditSchemaName));
        }

        /// <summary>
        /// Saves all changes made in this context to the underlying database.
        /// </summary>
        /// <returns>
        /// The number of objects written to the underlying database.
        /// </returns>
        public override int SaveChanges()
        {
            // short-circuit if disabled
            if (this.disableAuditing)
            {
                return base.SaveChanges();
            }

            // make sure we have what we need
            if (this.HostId.Equals(default(TKey)))
            {
                this.HostId = GetHostId();
            }
            if (this.HostName.IsNullOrWhiteSpace())
            {
                this.HostName = GetHostName();
            }
            if (this.UserId.Equals(default(TKey)))
            {
                this.UserId = GetUserId();
            }
            if (this.UserName.IsNullOrWhiteSpace())
            {
                this.UserName = GetUserName();
            }

            ChangeTracker.DetectChanges(); //! important to call this prior to auditing, etc.

            UpdateAuditUserAndDateFields(); // last changed date/by, created date/by, etc...

            CreateAuditRecords(); // create audit data and hold it until after we save user's changes

            // save changes
            int changesCount = base.SaveChanges(); // save the change count so our audit stuff does not affect the return value...

            // put audit records into DbContext and save them
            AppendAuditRecordsToContext(); //! this calls base.SaveChanges()! (if audit records exist)

            return changesCount;
        }

        public EntityEditPoint<TKey>[] GetEntityEditPoints(IAuditable<TKey> entity)
        {
            var auditIds = this.AuditItems
                .Where(i => i.Entity1Id.Equals(entity.Id))
                .Select(i => i.AuditId)
                .Distinct()
                .ToArray();

            return this.Audits
                .Where(a => auditIds.Contains(a.Id))
                .Select(a => new EntityEditPoint<TKey>() { EntityId = entity.Id, EditDate = a.AuditDate, UserName = a.UserName, UserId = a.UserId })
                .OrderByDescending(ep => ep.EditDate)
                .ToArray();
        }

        public EntityVersion<TKey, TEntity>[] GetEntityVersions<TEntity>(TEntity entity)
            where TEntity : IAuditable<TKey>
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");

            var versions = new List<EntityVersion<TKey, TEntity>>();

            var auditIds = this.AuditItems
                .Where(i => i.Entity1Id.Equals(entity.Id))
                .Select(i => i.AuditId)
                .Distinct()
                .ToArray();

            var audits = this.Audits
                .Where(a => auditIds.Contains(a.Id))
                .OrderByDescending(a => a.AuditDate)
                .ToArray();

            TEntity lastVersion = entity;
            foreach (var audit in audits)
            {
                TEntity currentVersion = lastVersion.Copy();

                Contract.Assume(currentVersion != null);

                var auditItems = this.AuditItems
                    .Where(i => i.AuditId == audit.Id && i.Entity1Id.Equals(entity.Id));

                // set properties back to pre-edit versions
                foreach (var auditItem in auditItems)
                {
                    SetEntityProperty(currentVersion, auditItem);
                }

                versions.Add(new EntityVersion<TKey, TEntity>() { EditDate = audit.AuditDate, UserName = audit.UserName, UserId = audit.UserId, Entity = currentVersion });

                lastVersion = currentVersion;
            }


            return versions.ToArray();
        }

        public EntityPropertyVersion<TKey>[] GetEntityPropertyVersions<TEntity>(TEntity entity, string propertyName)
            where TEntity : IAuditable<TKey>
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");

            var versions = new List<EntityPropertyVersion<TKey>>();

            var auditIds = this.AuditItems
                .Where(i => i.Entity1Id.Equals(entity.Id))
                .Select(i => i.AuditId)
                .Distinct()
                .ToArray();

            var audits = this.Audits
                .Where(a => auditIds.Contains(a.Id))
                .OrderByDescending(a => a.AuditDate)
                .ToArray();

            foreach (var audit in audits)
            {
                var auditItems = this.AuditItems
                    .Where(i => i.AuditId == audit.Id && i.Entity1Id.Equals(entity.Id) && i.AuditProperty.PropertyName == propertyName);


                // set properties back to pre-edit versions
                foreach (var auditItem in auditItems)
                {
                    versions.Add(new EntityPropertyVersion<TKey>() { EditDate = audit.AuditDate, UserName = audit.UserName, UserId = audit.UserId, PropertyName = propertyName, PropertyValue = auditItem.NewValue });
                }
            }


            return versions.ToArray();
        }

        private void SetEntityProperty(IAuditable<TKey> entity, AuditItem<TKey> auditItem)
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");
            Contract.Requires<ArgumentNullException>(auditItem != null, "auditItem");

            var auditProperty = this.AuditProperties.Single(p => p.Id == auditItem.AuditPropertyId);

            PropertyInfo prop = GetEntityType(entity).GetProperty(auditProperty.PropertyName);

            if (auditItem.NewValue == null)
            {
                prop.SetValue(entity, null);
            }
            else
            {
                var type = prop.PropertyType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
	            {
		            type = Nullable.GetUnderlyingType(type);
	            }

                prop.SetValue(entity, Convert.ChangeType(auditItem.NewValue, type));
            }
        }

        /// <summary>
        /// Updates the audit fields (CreatedDate, CreatedBy, LastChangedDate, and LastChangedBy).
        /// </summary>
        private void UpdateAuditUserAndDateFields()
        {
            DateTime now = DateTime.Now;


            // set created and last changed fields for added entities
            var addedEntities = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added).Where(ose => !ose.IsRelationship);
            //var addedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added);
            foreach (var item in addedEntities)
            {
                IAuditUserAndDate<TKey> entity = item.Entity as IAuditUserAndDate<TKey>;
                if (entity != null)
                {
                    entity.CreatedDate = now;
                    entity.CreatedBy = UserId;
                    entity.LastChangedDate = now;
                    entity.LastChangedBy = UserId;
                }
            }

            // set last changed fields for updated entities
            var changedEntities = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Modified).Where(ose => !ose.IsRelationship);
            //var changedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified);
            foreach (var item in changedEntities)
            {
                IAuditUserAndDate<TKey> entity = item.Entity as IAuditUserAndDate<TKey>;
                if (entity != null)
                {
                    entity.LastChangedDate = now;
                    entity.LastChangedBy = UserId;
                }
            }
        }

        private void CreateAuditRecords()
        {
            this.currentAudit = new TAudit() { AuditDate = DateTime.Now, HostId = this.HostId, HostName = this.HostName, UserId = this.UserId, UserName = this.UserName };
            this.currentAuditProperties = new List<AuditProperty>();
            this.currentAuditItems = new List<TAuditItem>();

            CreateAuditInsertRecords();
            CreateAuditUpdateRecords();
            CreateAuditDeleteRecords();
            CreateAuditAddRelationRecords();
            CreateAuditDeleteRelationRecords();
        }

        private void AppendAuditRecordsToContext()
        {
            if (this.currentAuditItems == null || this.currentAuditItems.Count == 0)
            {
                // nothing audited, no need to add the audit record to the database
                return;
            }

            var autoDetect = this.Configuration.AutoDetectChangesEnabled;
            try
            {
                this.Configuration.AutoDetectChangesEnabled = false;

                this.Audits.Add(this.currentAudit);

                foreach (var item in this.currentAuditProperties)
                {
                    if (item.Id == 0)
                    {
                        this.AuditProperties.Add(item);
                    }
                }

                // need to save changes to get audit and audit property ids
                base.SaveChanges();

                foreach (var item in this.currentAuditItems)
                {
                    // get ids parents (some might have been new)
                    item.AuditId = item.Audit.Id;
                    item.AuditPropertyId = item.AuditProperty.Id;

                    // get entity ids
                    if (item.Entity1 != null)
                    {
                        item.Entity1Id = item.Entity1.Id;
                    }
                    if (item.Entity2 != null)
                    {
                        item.Entity2Id = item.Entity2.Id;
                    }

                    this.AuditItems.Add(item);
                }

                base.SaveChanges();
            }
            finally
            {
                this.Configuration.AutoDetectChangesEnabled = autoDetect;
            }
        }

        private void CreateAuditInsertRecords()
        {
            var entities = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added).Where(ose => !ose.IsRelationship);
            //var entities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added);
            foreach (var item in entities)
            {
                IAuditable<TKey> entity = item.Entity as IAuditable<TKey>;
                if (entity != null)
                {
                    try
                    {
                        string oldValue = null;
                        string newValue;

                        var props = GetEntityType(entity).GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var prop in props)
                        {
                            AuditProperty auditProperty = GetAuditProperty(entity, prop);

                            if (auditProperty != null)
                            {
                                var newObj = prop.GetValue(entity);

                                newValue = newObj == null ? null : newObj.ToString();

                                if (newValue != oldValue)
                                {
                                    TAuditItem auditItem = new TAuditItem() { Audit = this.currentAudit, AuditProperty = auditProperty, Entity1 = entity, Entity2 = null, OperationType = "C", OldValue = oldValue, NewValue = newValue };

                                    this.currentAuditItems.Add(auditItem);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: log the error?
                        System.Diagnostics.Debug.WriteLine("Error auditing an insert: " + ex.Message);
                    }
                }
            }
        }

        private void CreateAuditUpdateRecords()
        {
            var entities = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Modified).Where(ose => !ose.IsRelationship);
            //var entities = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified);
            foreach (var item in entities)
            {
                IAuditable<TKey> entity = item.Entity as IAuditable<TKey>;
                if (entity != null)
                {
                    try
                    {
                        string oldValue;
                        string newValue;

                        var props = GetEntityType(entity).GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var prop in props)
                        {
                            AuditProperty auditProperty = GetAuditProperty(entity, prop);

                            if (auditProperty != null)
                            {
                                var oldObj = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntry(entity).OriginalValues[prop.Name];
                                var newObj = prop.GetValue(entity);

                                oldValue = oldObj == null ? null : oldObj.ToString();
                                newValue = newObj == null ? null : newObj.ToString();

                                if (newValue != oldValue)
                                {
                                    TAuditItem auditItem = new TAuditItem() { Audit = this.currentAudit, AuditProperty = auditProperty, Entity1 = entity, Entity2 = null, OperationType = "U", OldValue = oldValue, NewValue = newValue };

                                    this.currentAuditItems.Add(auditItem);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: log the error?
                        System.Diagnostics.Debug.WriteLine("Error auditing an update: " + ex.Message);
                    }

                }
            }
        }

        private void CreateAuditDeleteRecords()
        {
            // audit deleted items
            var entities = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Deleted).Where(ose => !ose.IsRelationship);
            //var entities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added);
            foreach (var item in entities)
            {
                IAuditable<TKey> entity = item.Entity as IAuditable<TKey>;
                if (entity != null)
                {
                    try
                    {
                        string oldValue;
                        string newValue;

                        var props = GetEntityType(entity).GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var prop in props)
                        {
                            AuditProperty auditProperty = GetAuditProperty(entity, prop);

                            if (auditProperty != null)
                            {
                                var oldObj = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntry(entity).OriginalValues[prop.Name];
                                var newObj = prop.GetValue(entity);

                                oldValue = oldObj == null ? null : oldObj.ToString();
                                newValue = newObj == null ? null : newObj.ToString();

                                if (newValue != oldValue)
                                {
                                    TAuditItem auditItem = new TAuditItem() { Audit = this.currentAudit, AuditProperty = auditProperty, Entity1 = entity, Entity2 = null, OperationType = "D", OldValue = oldValue, NewValue = newValue };

                                    this.currentAuditItems.Add(auditItem);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: log the error?
                        System.Diagnostics.Debug.WriteLine("Error auditing a delete: " + ex.Message);
                    }
                }
            }
        }

        private void CreateAuditAddRelationRecords()
        {
            var relations = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added).Where(ose => ose.IsRelationship);
            foreach (var item in relations)
            {
                try
                {
                    ObjectStateEntry parentEntry = GetEntityEntryFromRelation(item, 0);
                    ObjectStateEntry childEntry;
                    IAuditable<TKey> parent = parentEntry.Entity as IAuditable<TKey>;
                    if (parent != null)
                    {
                        IAuditable<TKey> child;

                        // Find representation of the relation
                        System.Data.Entity.Core.Objects.DataClasses.IRelatedEnd relatedEnd = parentEntry.RelationshipManager.GetAllRelatedEnds().First(r => r.RelationshipSet == item.EntitySet);

                        childEntry = GetEntityEntryFromRelation(item, 1);
                        child = childEntry.Entity as IAuditable<TKey>;

                        if (child != null)
                        {
                            try
                            {
                                AuditProperty auditProperty = GetAuditProperty(relatedEnd);
                                TAuditItem auditItem = new TAuditItem() { Audit = this.currentAudit, AuditProperty = auditProperty, Entity1 = parent, Entity2 = child, OperationType = "+", OldValue = string.Empty, NewValue = string.Empty };

                                this.currentAuditItems.Add(auditItem);
                            }
                            catch (Exception ex)
                            {
                                // TODO: log the error?
                                System.Diagnostics.Debug.WriteLine("Error auditing an add relation: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error auditing added relation: " + ex.Message);
                }
            }
        }

        private void CreateAuditDeleteRelationRecords()
        {
            // audit deleted relationships
            var deletedRelations = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Deleted).Where(ose => ose.IsRelationship);
            foreach (var item in deletedRelations)
            {
                try
                {
                    ObjectStateEntry parentEntry = GetEntityEntryFromRelation(item, 0);
                    ObjectStateEntry childEntry;
                    IAuditable<TKey> parent = parentEntry.Entity as IAuditable<TKey>;
                    if (parent != null)
                    {
                        IAuditable<TKey> child;

                        // Find representation of the relation
                        System.Data.Entity.Core.Objects.DataClasses.IRelatedEnd relatedEnd = parentEntry.RelationshipManager.GetAllRelatedEnds().First(r => r.RelationshipSet == item.EntitySet);

                        childEntry = GetEntityEntryFromRelation(item, 1);
                        child = childEntry.Entity as IAuditable<TKey>;

                        if (child != null)
                        {
                            try
                            {
                                AuditProperty auditProperty = GetAuditProperty(relatedEnd);
                                TAuditItem auditItem = new TAuditItem() { Audit = this.currentAudit, AuditProperty = auditProperty, Entity1 = parent, Entity2 = child, OperationType = "-", OldValue = string.Empty, NewValue = string.Empty };

                                this.currentAuditItems.Add(auditItem);
                            }
                            catch (Exception ex)
                            {
                                // TODO: log the error?
                                System.Diagnostics.Debug.WriteLine("Error auditing a delete relation: " + ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error auditing remove relation: " + ex.Message);
                }
            }
        }

        private static Type GetEntityType(IAuditable<TKey> entity)
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");
            Contract.Ensures(Contract.Result<Type>() != null);

            Type type = entity.GetType();

            if (type.FullName.StartsWith("System.Data.Entity.DynamicProxies."))
            {
                // we don't want the dynamic proxy, we want the actual class the proxy is based on
                return type.BaseType;

            }

            return type;
        }

        private static string GetEntityTypeName(IAuditable<TKey> entity)
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");
            Contract.Ensures(Contract.Result<string>() != null);

            Type type = GetEntityType(entity);

            return type.Name;
        }

        private AuditProperty GetAuditProperty(IAuditable<TKey> entity, PropertyInfo prop)
        {
            Contract.Requires<ArgumentNullException>(entity != null, "entity");
            Contract.Requires<ArgumentNullException>(prop != null, "prop");
            //x Contract.Ensures(Contract.Result<AuditProperty>() != null);

            string entityName = GetEntityTypeName(entity); // ObjectContext.GetObjectType(entity.GetType()).Name;
            string propertyName = prop.Name;

            // only value types are audited (include strings in this...)
            if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
            {
                return null;
            }
            // do not log collections
            if (prop.PropertyType is IEnumerable)
            {
                return null;
            }
            // ignore properties with the ignore attribute
            if (prop.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).FirstOrDefault() != null)
            {
                return null;
            }
            // ignore properties with the not tracked attribute
            if (prop.GetCustomAttributes(typeof(NotMappedAttribute), true).FirstOrDefault() != null)
            {
                return null;
            }

            // fix up the property type (not sure everyone will like this...
            string propertyType = prop.PropertyType.ToString();
            bool nullable = false;
            if (propertyType.StartsWith("System.Nullable"))
            {
                nullable = true;
                propertyType = propertyType.Substring(propertyType.IndexOf("[") + 1).TrimEnd(']');
            }
            if (propertyType.StartsWith("System."))
            {
                propertyType = propertyType.Substring(7);
            }
            if (propertyType == "String") { propertyType = "string"; }
            if (propertyType == "Int32") { propertyType = "int"; }
            if (propertyType == "Int64") { propertyType = "long"; }
            if (propertyType == "Int16") { propertyType = "short"; }
            if (propertyType == "Boolean") { propertyType = "bool"; }
            if (propertyType == "Byte") { propertyType = "byte"; }
            if (propertyType == "Single") { propertyType = "float"; }
            if (propertyType == "Double") { propertyType = "double"; }
            if (propertyType == "Decimal") { propertyType = "decimal"; }
            if (propertyType == "Char") { propertyType = "char"; }
            if (propertyType == "Sbyte") { propertyType = "sbyte"; }

            if (nullable)
            {
                propertyType += "?";
            }

            // check our list
            AuditProperty auditProperty = this.currentAuditProperties.FirstOrDefault(ap => ap.EntityName == entityName && ap.PropertyName == propertyName && ap.IsRelation == false);

            if (auditProperty == null)
            {
                // check the database
                auditProperty = this.AuditProperties.FirstOrDefault(ap => ap.EntityName == entityName && ap.PropertyName == propertyName && ap.IsRelation == false);

                if (auditProperty == null)
                {
                    // create it
                    auditProperty = new AuditProperty() { EntityName = entityName, PropertyName = propertyName, PropertyType = propertyType.ToString(), IsRelation = false };
                }

                this.currentAuditProperties.Add(auditProperty);
            }

            return auditProperty;
        }

        private AuditProperty GetAuditProperty(System.Data.Entity.Core.Objects.DataClasses.IRelatedEnd relation)
        {
            Contract.Requires<ArgumentNullException>(relation != null, "relation");
            Contract.Ensures(Contract.Result<AuditProperty>() != null);

            string relationName = relation.RelationshipSet.Name;
            string propertyName = string.Empty;

            // check our list
            AuditProperty auditProperty = this.currentAuditProperties.FirstOrDefault(ap => ap.EntityName == relationName && ap.PropertyName == propertyName && ap.IsRelation == true);

            if (auditProperty == null)
            {
                // check the database
                auditProperty = this.AuditProperties.FirstOrDefault(ap => ap.EntityName == relationName && ap.PropertyName == propertyName && ap.IsRelation == true);

                if (auditProperty == null)
                {
                    // create it
                    auditProperty = new AuditProperty() { EntityName = relationName, PropertyName = propertyName, PropertyType = string.Empty, IsRelation = true };
                }

                this.currentAuditProperties.Add(auditProperty);
            }

            return auditProperty;
        }

        //private TAuditProperty GetAuditProperty(IAuditable<TKey> entity)
        //{
        //    Contract.Requires<ArgumentNullException>(entity != null, "entity");
        //    Contract.Ensures(Contract.Result<TAuditProperty>() != null);

        //    string entityName = GetEntityTypeName(entity); // ObjectContext.GetObjectType(entity.GetType()).Name;
        //    string propertyName = string.Empty;

        //    // check our list
        //    TAuditProperty auditProperty = this.currentAuditProperties.FirstOrDefault(ap => ap.EntityName == entityName && ap.PropertyName == propertyName && ap.IsRelation == true);

        //    if (auditProperty == null)
        //    {
        //        // check the database
        //        auditProperty = this.AuditProperties.FirstOrDefault(ap => ap.EntityName == entityName && ap.PropertyName == propertyName && ap.IsRelation == true);

        //        if (auditProperty == null)
        //        {
        //            // create it
        //            auditProperty = new TAuditProperty() { EntityName = entityName, PropertyName = propertyName, PropertyType = (prop == null ? "<n/a>" : prop.PropertyType.ToString()), IsRelation = true };
        //        }

        //        this.currentAuditProperties.Add(auditProperty);
        //    }

        //    return auditProperty;
        //}

        private ObjectStateEntry GetEntityEntryFromRelation(ObjectStateEntry relationEntry, int index)
        {
            Contract.Requires<ArgumentNullException>(relationEntry != null, "relationEntry");
            Contract.Ensures(Contract.Result<ObjectStateEntry>() != null);

            System.Data.Entity.Core.EntityKey firstKey;
            if (relationEntry.State == System.Data.Entity.EntityState.Deleted)
            {
                firstKey = (System.Data.Entity.Core.EntityKey)relationEntry.OriginalValues[index];  // added or detached objects cannot have OriginalValues
            }
            else
            {
                firstKey = (System.Data.Entity.Core.EntityKey)relationEntry.CurrentValues[index]; // deleted or detached objects cannot have CurrentValues
            }

            ObjectStateEntry entry = ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.GetObjectStateEntry(firstKey);

            return entry;
        }
    }
}
