using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Potok.Models;

namespace Potok.Data;

public partial class PotokDbContext : DbContext
{
    public PotokDbContext(DbContextOptions<PotokDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchContact> BranchContacts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<ContactType> ContactTypes { get; set; }

    public virtual DbSet<Counterparty> Counterparties { get; set; }

    public virtual DbSet<CounterpartyContact> CounterpartyContacts { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentItem> DocumentItems { get; set; }

    public virtual DbSet<DocumentStatus> DocumentStatuses { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeContact> EmployeeContacts { get; set; }

    public virtual DbSet<Login> Logins { get; set; }

    public virtual DbSet<Nomenclature> Nomenclatures { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OrganizationContact> OrganizationContacts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StatusEmployee> StatusEmployees { get; set; }

    public virtual DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseContact> WarehouseContacts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone", "recovery_code" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in", "like", "ilike", "is", "match", "imatch", "isdistinct" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("branch_pkey");

            entity.ToTable("branch");

            entity.HasIndex(e => e.Code, "branch_code_key").IsUnique();

            entity.Property(e => e.BranchId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("branch_id");
            entity.Property(e => e.Address)
                .HasColumnType("character varying")
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Branches)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_branch_organization");
        });

        modelBuilder.Entity<BranchContact>(entity =>
        {
            entity.HasKey(e => e.BranchContactId).HasName("branch_contact_pkey");

            entity.ToTable("branch_contact");

            entity.Property(e => e.BranchContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("branch_contact_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchContacts)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_branch_contact_branch");

            entity.HasOne(d => d.Contact).WithMany(p => p.BranchContacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_branch_contact_contact");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("category_pkey");

            entity.ToTable("category");

            entity.HasIndex(e => e.Name, "category_name_key").IsUnique();

            entity.Property(e => e.CategoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("contact_pkey");

            entity.ToTable("contact");

            entity.Property(e => e.ContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("contact_id");
            entity.Property(e => e.ContactTypeId).HasColumnName("contact_type_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Value)
                .HasColumnType("character varying")
                .HasColumnName("value");

            entity.HasOne(d => d.ContactType).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.ContactTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_contact_contact_type");
        });

        modelBuilder.Entity<ContactType>(entity =>
        {
            entity.HasKey(e => e.ContactTypeId).HasName("contact_type_pkey");

            entity.ToTable("contact_type");

            entity.HasIndex(e => e.Name, "contact_type_name_key").IsUnique();

            entity.Property(e => e.ContactTypeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("contact_type_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Counterparty>(entity =>
        {
            entity.HasKey(e => e.CounterpartyId).HasName("counterparty_pkey");

            entity.ToTable("counterparty");

            entity.HasIndex(e => e.RegistrationNumber, "counterparty_registration_number_key").IsUnique();

            entity.HasIndex(e => e.TaxId, "counterparty_tax_id_key").IsUnique();

            entity.Property(e => e.CounterpartyId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("counterparty_id");
            entity.Property(e => e.FullName)
                .HasColumnType("character varying")
                .HasColumnName("full_name");
            entity.Property(e => e.Kpp)
                .HasColumnType("character varying")
                .HasColumnName("kpp");
            entity.Property(e => e.LegalAddress)
                .HasColumnType("character varying")
                .HasColumnName("legal_address");
            entity.Property(e => e.Okpo)
                .HasColumnType("character varying")
                .HasColumnName("okpo");
            entity.Property(e => e.RegistrationNumber)
                .HasColumnType("character varying")
                .HasColumnName("registration_number");
            entity.Property(e => e.ShortName)
                .HasColumnType("character varying")
                .HasColumnName("short_name");
            entity.Property(e => e.TaxId)
                .HasColumnType("character varying")
                .HasColumnName("tax_id");
        });

        modelBuilder.Entity<CounterpartyContact>(entity =>
        {
            entity.HasKey(e => e.CounterpartyContactId).HasName("counterparty_contact_pkey");

            entity.ToTable("counterparty_contact");

            entity.Property(e => e.CounterpartyContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("counterparty_contact_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.CounterpartyId).HasColumnName("counterparty_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.CounterpartyContacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_counterparty_contact_contact");

            entity.HasOne(d => d.Counterparty).WithMany(p => p.CounterpartyContacts)
                .HasForeignKey(d => d.CounterpartyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_counterparty_contact_counterparty");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("document_pkey");

            entity.ToTable("document");

            entity.HasIndex(e => e.DocumentNumber, "document_document_number_key").IsUnique();

            entity.Property(e => e.DocumentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("document_id");
            entity.Property(e => e.DocumentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("document_date");
            entity.Property(e => e.DocumentNumber)
                .HasColumnType("character varying")
                .HasColumnName("document_number");
            entity.Property(e => e.DocumentStatusId).HasColumnName("document_status_id");
            entity.Property(e => e.DocumentType)
                .HasColumnType("character varying")
                .HasColumnName("document_type");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.DocumentStatus).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DocumentStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_document_status");

            entity.HasOne(d => d.Employee).WithMany(p => p.Documents)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_document_employee");

            entity.HasOne(d => d.Order).WithMany(p => p.Documents)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_document_order");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Documents)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_document_warehouse");
        });

        modelBuilder.Entity<DocumentItem>(entity =>
        {
            entity.HasKey(e => e.DocumentItemId).HasName("document_item_pkey");

            entity.ToTable("document_item");

            entity.Property(e => e.DocumentItemId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("document_item_id");
            entity.Property(e => e.BatchNumber)
                .HasColumnType("character varying")
                .HasColumnName("batch_number");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.NomenclatureId).HasColumnName("nomenclature_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SerialNumber)
                .HasColumnType("character varying")
                .HasColumnName("serial_number");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentItems)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_document_item_document");

            entity.HasOne(d => d.Nomenclature).WithMany(p => p.DocumentItems)
                .HasForeignKey(d => d.NomenclatureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_document_item_nomenclature");
        });

        modelBuilder.Entity<DocumentStatus>(entity =>
        {
            entity.HasKey(e => e.DocumentStatusId).HasName("document_status_pkey");

            entity.ToTable("document_status");

            entity.HasIndex(e => e.Name, "document_status_name_key").IsUnique();

            entity.Property(e => e.DocumentStatusId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("document_status_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("employee_pkey");

            entity.ToTable("employee");

            entity.Property(e => e.EmployeeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("employee_id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.FirstName)
                .HasColumnType("character varying")
                .HasColumnName("first_name");
            entity.Property(e => e.Gender)
                .HasColumnType("character varying")
                .HasColumnName("gender");
            entity.Property(e => e.LastName)
                .HasColumnType("character varying")
                .HasColumnName("last_name");
            entity.Property(e => e.LoginId).HasColumnName("login_id");
            entity.Property(e => e.Patronymic)
                .HasColumnType("character varying")
                .HasColumnName("patronymic");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.StatusEmployeeId).HasColumnName("status_employee_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Login).WithMany(p => p.Employees)
                .HasForeignKey(d => d.LoginId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_employee_login");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_employee_role");

            entity.HasOne(d => d.StatusEmployee).WithMany(p => p.Employees)
                .HasForeignKey(d => d.StatusEmployeeId)
                .HasConstraintName("fk_employee_status_employee");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Employees)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_employee_warehouse");
        });

        modelBuilder.Entity<EmployeeContact>(entity =>
        {
            entity.HasKey(e => e.EmployeeContactId).HasName("employee_contact_pkey");

            entity.ToTable("employee_contact");

            entity.Property(e => e.EmployeeContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("employee_contact_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.EmployeeContacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_employee_contact_contact");

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeContacts)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_employee_contact_employee");
        });

        modelBuilder.Entity<Login>(entity =>
        {
            entity.HasKey(e => e.LoginId).HasName("login_pkey");

            entity.ToTable("login");

            entity.HasIndex(e => e.Name, "login_name_key").IsUnique();

            entity.Property(e => e.LoginId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("login_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasDefaultValueSql("''::character varying")
                .HasColumnType("character varying")
                .HasColumnName("password_hash");
        });

        modelBuilder.Entity<Nomenclature>(entity =>
        {
            entity.HasKey(e => e.NomenclatureId).HasName("nomenclature_pkey");

            entity.ToTable("nomenclature");

            entity.Property(e => e.NomenclatureId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("nomenclature_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.Category).WithMany(p => p.Nomenclatures)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_nomenclature_category");

            entity.HasOne(d => d.Unit).WithMany(p => p.Nomenclatures)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_nomenclature_unit");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.HasIndex(e => e.PublicToken, "orders_public_token_key").IsUnique();

            entity.Property(e => e.OrderId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("order_id");
            entity.Property(e => e.AcceptedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("accepted_at");
            entity.Property(e => e.AcceptedByCounterpartyId).HasColumnName("accepted_by_counterparty_id");
            entity.Property(e => e.CounterpartyId).HasColumnName("counterparty_id");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderStatusId).HasColumnName("order_status_id");
            entity.Property(e => e.PublicToken).HasColumnName("public_token");
            entity.Property(e => e.PublishedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("published_at");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.AcceptedByCounterparty).WithMany(p => p.OrderAcceptedByCounterparties)
                .HasForeignKey(d => d.AcceptedByCounterpartyId)
                .HasConstraintName("fk_order_accepted_counterparty");

            entity.HasOne(d => d.Counterparty).WithMany(p => p.OrderCounterparties)
                .HasForeignKey(d => d.CounterpartyId)
                .HasConstraintName("fk_order_counterparty");

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OrderStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_status");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Orders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_warehouse");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("order_item_pkey");

            entity.ToTable("order_item");

            entity.Property(e => e.OrderItemId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("order_item_id");
            entity.Property(e => e.NomenclatureId).HasColumnName("nomenclature_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Nomenclature).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.NomenclatureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_item_nomenclature");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_item_order");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.OrderStatusId).HasName("order_status_pkey");

            entity.ToTable("order_status");

            entity.HasIndex(e => e.Name, "order_status_name_key").IsUnique();

            entity.Property(e => e.OrderStatusId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("order_status_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.OrganizationId).HasName("organization_pkey");

            entity.ToTable("organization");

            entity.HasIndex(e => e.RegistrationNumber, "organization_registration_number_key").IsUnique();

            entity.HasIndex(e => e.TaxId, "organization_tax_id_key").IsUnique();

            entity.Property(e => e.OrganizationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("organization_id");
            entity.Property(e => e.FullName)
                .HasColumnType("character varying")
                .HasColumnName("full_name");
            entity.Property(e => e.Kpp)
                .HasColumnType("character varying")
                .HasColumnName("kpp");
            entity.Property(e => e.LegalAddress)
                .HasColumnType("character varying")
                .HasColumnName("legal_address");
            entity.Property(e => e.Okpo)
                .HasColumnType("character varying")
                .HasColumnName("okpo");
            entity.Property(e => e.RegistrationNumber)
                .HasColumnType("character varying")
                .HasColumnName("registration_number");
            entity.Property(e => e.ShortName)
                .HasColumnType("character varying")
                .HasColumnName("short_name");
            entity.Property(e => e.TaxId)
                .HasColumnType("character varying")
                .HasColumnName("tax_id");
        });

        modelBuilder.Entity<OrganizationContact>(entity =>
        {
            entity.HasKey(e => e.OrganizationContactId).HasName("organization_contact_pkey");

            entity.ToTable("organization_contact");

            entity.Property(e => e.OrganizationContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("organization_contact_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.OrganizationContacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_organization_contact_contact");

            entity.HasOne(d => d.Organization).WithMany(p => p.OrganizationContacts)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_organization_contact_organization");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.Name, "role_name_key").IsUnique();

            entity.Property(e => e.RoleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<StatusEmployee>(entity =>
        {
            entity.HasKey(e => e.StatusEmployeeId).HasName("status_employee_pkey");

            entity.ToTable("status_employee");

            entity.HasIndex(e => e.Name, "status_employee_name_key").IsUnique();

            entity.Property(e => e.StatusEmployeeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("status_employee_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UnitOfMeasurement>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("unit_of_measurement_pkey");

            entity.ToTable("unit_of_measurement");

            entity.HasIndex(e => e.Name, "unit_of_measurement_name_key").IsUnique();

            entity.Property(e => e.UnitId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("unit_id");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("warehouse_pkey");

            entity.ToTable("warehouse");

            entity.HasIndex(e => e.Code, "warehouse_code_key").IsUnique();

            entity.Property(e => e.WarehouseId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("warehouse_id");
            entity.Property(e => e.Address)
                .HasColumnType("character varying")
                .HasColumnName("address");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Code)
                .HasColumnType("character varying")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");

            entity.HasOne(d => d.Branch).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_warehouse_branch");
        });

        modelBuilder.Entity<WarehouseContact>(entity =>
        {
            entity.HasKey(e => e.WarehouseContactId).HasName("warehouse_contact_pkey");

            entity.ToTable("warehouse_contact");

            entity.Property(e => e.WarehouseContactId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("warehouse_contact_id");
            entity.Property(e => e.ContactId).HasColumnName("contact_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Contact).WithMany(p => p.WarehouseContacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_warehouse_contact_contact");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseContacts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_warehouse_contact_warehouse");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
