using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Functions.database
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
        public virtual DbSet<Dump> Dump { get; set; }
        public virtual DbSet<EfmigrationsHistory> EfmigrationsHistory { get; set; }
        public virtual DbSet<Modem> Modem { get; set; }
        public virtual DbSet<ModemConnectionTrace> ModemConnectionTrace { get; set; }
        public virtual DbSet<ModemInMemory> ModemInMemory { get; set; }
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

                entity.HasIndex(e => e.RoleId);

                entity.Property(e => e.UserId).HasMaxLength(256);

                entity.Property(e => e.RoleId).HasMaxLength(256);

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

            modelBuilder.Entity<Modem>(entity =>
            {
                entity.HasIndex(e => e.Imei)
                    .HasName("index_imei");

                entity.HasIndex(e => e.IpAddress)
                    .HasName("index_ip_address");

                entity.HasIndex(e => e.Mid)
                    .HasName("mid")
                    .IsUnique();

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

                entity.Property(e => e.KalValue)
                    .HasColumnName("kal_value")
                    .HasColumnType("int(11)");

                entity.Property(e => e.LggValue)
                    .HasColumnName("lgg_value")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Mid)
                    .HasColumnName("mid")
                    .HasMaxLength(50);

                entity.Property(e => e.Version)
                    .HasColumnName("version")
                    .HasMaxLength(10);
            });

            modelBuilder.Entity<ModemConnectionTrace>(entity =>
            {
                entity.HasIndex(e => e.IpAddress)
                    .HasName("index_ip_address");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasColumnName("ip_address")
                    .HasMaxLength(15);

                entity.Property(e => e.SendOrRecv)
                    .IsRequired()
                    .HasColumnName("send_or_recv")
                    .HasMaxLength(4);

                entity.Property(e => e.TransferredData)
                    .IsRequired()
                    .HasColumnName("transferred_data")
                    .HasColumnType("varchar(10000)");
            });

            modelBuilder.Entity<ModemInMemory>(entity =>
            {
                entity.ToTable("Modem_InMemory");

                entity.HasIndex(e => e.IpAddress)
                    .HasName("ip_address")
                    .IsUnique();

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
                    .HasColumnName("ID_Macchina")
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
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
