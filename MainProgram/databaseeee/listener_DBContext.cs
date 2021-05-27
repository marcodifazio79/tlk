using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace tlk_core.databaseeee
{
    public partial class listener_DBContext : DbContext
    {
        public listener_DBContext()
        {
        }

        public listener_DBContext(DbContextOptions<listener_DBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRoleClaims> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRoles> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUserTokens> AspNetUserTokens { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
        public virtual DbSet<Attr> Attr { get; set; }
        public virtual DbSet<CashTransaction> CashTransaction { get; set; }
        public virtual DbSet<CommandsMatch> CommandsMatch { get; set; }
        public virtual DbSet<Dump> Dump { get; set; }
        public virtual DbSet<EfmigrationsHistory> EfmigrationsHistory { get; set; }
        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<LogStatus> LogStatus { get; set; }
        public virtual DbSet<LogTargetRole> LogTargetRole { get; set; }
        public virtual DbSet<LogType> LogType { get; set; }
        public virtual DbSet<Machines> Machines { get; set; }
        public virtual DbSet<MachinesAttributes> MachinesAttributes { get; set; }
        public virtual DbSet<MachinesConnectionTrace> MachinesConnectionTrace { get; set; }
        public virtual DbSet<MachinesInMemory> MachinesInMemory { get; set; }
        public virtual DbSet<RemoteCommand> RemoteCommand { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseMySQL("server=10.10.10.71;port=3306;user=bot_user;password=Qwert@#!99;database=listener_DB");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AspNetRoleClaims>(entity =>
            {
                entity.HasIndex(e => e.RoleId);

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.RoleId)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetRoles>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaims>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogins>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey })
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.LoginProvider).HasMaxLength(256);

                entity.Property(e => e.ProviderKey).HasMaxLength(256);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserRoles>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId })
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.Id)
                    .HasName("id")
                    .IsUnique();

                entity.HasIndex(e => e.RoleId);

                entity.Property(e => e.UserId).HasMaxLength(256);

                entity.Property(e => e.RoleId).HasMaxLength(256);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)")
                    .ValueGeneratedOnAdd();

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserTokens>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name })
                    .HasName("PRIMARY");

                entity.Property(e => e.UserId).HasMaxLength(256);

                entity.Property(e => e.LoginProvider).HasMaxLength(256);

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.Value).HasMaxLength(256);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUsers>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(256);

                entity.Property(e => e.AccessFailedCount).HasColumnType("int(11)");

                entity.Property(e => e.EmailConfirmed).HasColumnType("int(1)");

                entity.Property(e => e.LockoutEnabled).HasColumnType("int(1)");

                entity.Property(e => e.PhoneNumberConfirmed).HasColumnType("int(1)");

                entity.Property(e => e.TwoFactorEnabled).HasColumnType("int(1)");
            });

            modelBuilder.Entity<Attr>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Comment).HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<CashTransaction>(entity =>
            {
                entity.HasIndex(e => e.IdMachines)
                    .HasName("index_ID_Machines");

                entity.HasIndex(e => e.IdMachinesConnectionTrace)
                    .HasName("ID_MachinesConnectionTrace");

                entity.HasIndex(e => e.Odm)
                    .HasName("index_ODM");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdMachines)
                    .HasColumnName("ID_Machines")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdMachinesConnectionTrace)
                    .HasColumnName("ID_MachinesConnectionTrace")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Odm)
                    .IsRequired()
                    .HasColumnName("ODM")
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TentativiAutomaticiEseguiti)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.HasOne(d => d.IdMachinesNavigation)
                    .WithMany(p => p.CashTransaction)
                    .HasForeignKey(d => d.IdMachines)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("CashTransaction_ibfk_1");

                entity.HasOne(d => d.IdMachinesConnectionTraceNavigation)
                    .WithMany(p => p.CashTransaction)
                    .HasForeignKey(d => d.IdMachinesConnectionTrace)
                    .HasConstraintName("CashTransaction_ibfk_2");
            });

            modelBuilder.Entity<CommandsMatch>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ExpectedAnswer)
                    .HasColumnName("expectedAnswer")
                    .HasMaxLength(50);

                entity.Property(e => e.ModemCommand).HasMaxLength(100);

                entity.Property(e => e.WebCommand)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<Dump>(entity =>
            {
                entity.ToTable("dump");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Data)
                    .IsRequired()
                    .HasColumnName("data")
                    .HasColumnType("varchar(10000)");
            });

            modelBuilder.Entity<EfmigrationsHistory>(entity =>
            {
                entity.HasKey(e => e.MigrationId)
                    .HasName("PRIMARY");

                entity.ToTable("__EFMigrationsHistory");

                entity.Property(e => e.MigrationId).HasMaxLength(150);

                entity.Property(e => e.ProductVersion)
                    .IsRequired()
                    .HasMaxLength(32);
            });

            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasIndex(e => e.IdLogStatus)
                    .HasName("ID_LogStatus");

                entity.HasIndex(e => e.IdLogType)
                    .HasName("index_ID_LogType");

                entity.HasIndex(e => e.IdMachine)
                    .HasName("index_ID_machine");

                entity.HasIndex(e => e.IdUser)
                    .HasName("index_ID_user");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.IdLogStatus)
                    .HasColumnName("ID_LogStatus")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdLogType)
                    .HasColumnName("ID_LogType")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdMachine)
                    .HasColumnName("ID_machine")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdUser)
                    .HasColumnName("ID_user")
                    .HasMaxLength(256);

                entity.Property(e => e.LinkToRelevantLocation)
                    .HasColumnName("linkToRelevantLocation")
                    .HasMaxLength(1024);

                entity.Property(e => e.LogDescription)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.LogSeggestedActions).HasMaxLength(500);

                entity.HasOne(d => d.IdLogStatusNavigation)
                    .WithMany(p => p.Log)
                    .HasForeignKey(d => d.IdLogStatus)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Log_ibfk_3");

                entity.HasOne(d => d.IdLogTypeNavigation)
                    .WithMany(p => p.Log)
                    .HasForeignKey(d => d.IdLogType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Log_ibfk_1");

                entity.HasOne(d => d.IdMachineNavigation)
                    .WithMany(p => p.Log)
                    .HasForeignKey(d => d.IdMachine)
                    .HasConstraintName("Log_ibfk_2");

                entity.HasOne(d => d.IdUserNavigation)
                    .WithMany(p => p.Log)
                    .HasForeignKey(d => d.IdUser)
                    .HasConstraintName("Log_ibfk_4");
            });

            modelBuilder.Entity<LogStatus>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<LogTargetRole>(entity =>
            {
                entity.HasIndex(e => e.IdAspNetRoles)
                    .HasName("index_ID_AspNetRoles");

                entity.HasIndex(e => e.IdLog)
                    .HasName("index_ID_Log");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdAspNetRoles)
                    .IsRequired()
                    .HasColumnName("ID_AspNetRoles")
                    .HasMaxLength(256);

                entity.Property(e => e.IdLog)
                    .HasColumnName("ID_Log")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.IdAspNetRolesNavigation)
                    .WithMany(p => p.LogTargetRole)
                    .HasForeignKey(d => d.IdAspNetRoles)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("LogTargetRole_ibfk_1");

                entity.HasOne(d => d.IdLogNavigation)
                    .WithMany(p => p.LogTargetRole)
                    .HasForeignKey(d => d.IdLog)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("LogTargetRole_ibfk_2");
            });

            modelBuilder.Entity<LogType>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Machines>(entity =>
            {
                entity.HasIndex(e => e.Imei)
                    .HasName("index_imei");

                entity.HasIndex(e => e.IpAddress)
                    .HasName("index_ip_address");

                entity.HasIndex(e => e.Mid)
                    .HasName("index_mid");

                entity.HasIndex(e => new { e.IpAddress, e.Mid })
                    .HasName("index_ip_mid");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Imei)
                    .HasColumnName("imei")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasColumnName("ip_address")
                    .HasMaxLength(15);

                entity.Property(e => e.IsOnline)
                    .IsRequired()
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Mid)
                    .HasColumnName("mid")
                    .HasMaxLength(50);

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<MachinesAttributes>(entity =>
            {
                entity.HasIndex(e => e.IdAttribute)
                    .HasName("id_Attribute");

                entity.HasIndex(e => e.IdMacchina)
                    .HasName("id_Macchina");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdAttribute)
                    .HasColumnName("id_Attribute")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdMacchina)
                    .HasColumnName("id_Macchina")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.IdAttributeNavigation)
                    .WithMany(p => p.MachinesAttributes)
                    .HasForeignKey(d => d.IdAttribute)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("MachinesAttributes_ibfk_1");

                entity.HasOne(d => d.IdMacchinaNavigation)
                    .WithMany(p => p.MachinesAttributes)
                    .HasForeignKey(d => d.IdMacchina)
                    .HasConstraintName("MachinesAttributes_ibfk_2");
            });

            modelBuilder.Entity<MachinesConnectionTrace>(entity =>
            {
                entity.HasIndex(e => e.IdMacchina)
                    .HasName("index_id_Macchina");

                entity.HasIndex(e => e.IpAddress)
                    .HasName("index_ip_address");

                entity.HasIndex(e => e.TelemetriaStatus)
                    .HasName("index_telemetria_status");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IdMacchina)
                    .HasColumnName("id_Macchina")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasColumnName("ip_address")
                    .HasMaxLength(15);

                entity.Property(e => e.SendOrRecv)
                    .IsRequired()
                    .HasColumnName("send_or_recv")
                    .HasMaxLength(4);

                entity.Property(e => e.TelemetriaStatus)
                    .HasColumnName("telemetria_status")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TransferredData)
                    .IsRequired()
                    .HasColumnName("transferred_data")
                    .HasColumnType("varchar(10000)");

                entity.HasOne(d => d.IdMacchinaNavigation)
                    .WithMany(p => p.MachinesConnectionTrace)
                    .HasForeignKey(d => d.IdMacchina)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("MachinesConnectionTrace_ibfk_1");
            });

            modelBuilder.Entity<MachinesInMemory>(entity =>
            {
                entity.ToTable("Machines_InMemory");

                entity.HasIndex(e => e.IpAddress)
                    .HasName("index_ip_address");

                entity.HasIndex(e => e.Mid)
                    .HasName("mid")
                    .IsUnique();

                entity.HasIndex(e => e.TcpLocalPort)
                    .HasName("index_local_port");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasColumnName("ip_address")
                    .HasMaxLength(15);

                entity.Property(e => e.Mid)
                    .HasColumnName("mid")
                    .HasMaxLength(50);

                entity.Property(e => e.TcpLocalPort)
                    .HasColumnName("tcp_local_port")
                    .HasColumnType("int(11)");
            });

            modelBuilder.Entity<RemoteCommand>(entity =>
            {
                entity.HasIndex(e => e.IdMacchina)
                    .HasName("index_ID_Macchina");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Body)
                    .IsRequired()
                    .HasColumnName("body")
                    .HasColumnType("varchar(10000)");

                entity.Property(e => e.IdMacchina)
                    .HasColumnName("id_Macchina")
                    .HasColumnType("int(11)");

                entity.Property(e => e.LifespanSeconds)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'15'");

                entity.Property(e => e.Sender)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.HasOne(d => d.IdMacchinaNavigation)
                    .WithMany(p => p.RemoteCommand)
                    .HasForeignKey(d => d.IdMacchina)
                    .HasConstraintName("RemoteCommand_ibfk_1");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
