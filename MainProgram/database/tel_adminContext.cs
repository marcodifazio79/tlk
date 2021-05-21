using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace tlk_core.database
{
    public partial class tel_adminContext : DbContext
    {
        public tel_adminContext()
        {
        }

        public tel_adminContext(DbContextOptions<tel_adminContext> options)
            : base(options)
        {
        }

        public virtual DbSet<SapCashDaemon> SapCashDaemon { get; set; }
        public virtual DbSet<SapCashProducts> SapCashProducts { get; set; }
        public static IConfiguration Configuration;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL(Configuration["ConnectionStrings:DB_Casse"].ToString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SapCashDaemon>(entity =>
            {
                entity.ToTable("sap_cash_daemon");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Bns1)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns10)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns11)
                    .HasColumnName("BNS_1")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns2)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns20)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns21)
                    .HasColumnName("BNS_2")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Bns5)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.CanaleGettone)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.CanaleProve)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Ch9).HasDefaultValueSql("'0'");

                entity.Property(e => e.CodeMa)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Consumabile)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ForceStop)
                    .HasColumnName("force_stop")
                    .HasColumnType("int(11)");

                entity.Property(e => e.HopperGettone)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.MdbInc2).HasColumnType("int(11)");

                entity.Property(e => e.MdbInc3).HasColumnType("int(11)");

                entity.Property(e => e.MdbInc4).HasColumnType("int(11)");

                entity.Property(e => e.MdbInc5).HasColumnType("int(11)");

                entity.Property(e => e.MdbInc6).HasColumnType("int(11)");

                entity.Property(e => e.MdbTub2).HasColumnType("int(11)");

                entity.Property(e => e.MdbTub3).HasColumnType("int(11)");

                entity.Property(e => e.MdbTub4).HasColumnType("int(11)");

                entity.Property(e => e.MdbTub5).HasColumnType("int(11)");

                entity.Property(e => e.MdbTub6).HasColumnType("int(11)");

                entity.Property(e => e.MechValue).HasDefaultValueSql("'0'");

                entity.Property(e => e.Message)
                    .HasColumnName("message")
                    .HasMaxLength(255);

                entity.Property(e => e.OdmTaskPalmare).HasMaxLength(255);

                entity.Property(e => e.Price).HasDefaultValueSql("'0'");

                entity.Property(e => e.Qty1).HasColumnType("int(11)");

                entity.Property(e => e.Qty2).HasColumnType("int(11)");

                entity.Property(e => e.Qty3).HasColumnType("int(11)");

                entity.Property(e => e.Qty4).HasColumnType("int(11)");

                entity.Property(e => e.Qty5).HasColumnType("int(11)");

                entity.Property(e => e.Qty6).HasColumnType("int(11)");

                entity.Property(e => e.Qty7).HasColumnType("int(11)");

                entity.Property(e => e.Qty8).HasColumnType("int(11)");

                entity.Property(e => e.Qty9)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.QtyV1)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.QtyV2)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Sales)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.SapExitCode)
                    .HasColumnName("sap_exit_code")
                    .HasMaxLength(3);

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasMaxLength(10)
                    .HasDefaultValueSql("'000'");

                entity.Property(e => e.Ticket)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TimestampNextTry)
                    .HasColumnName("timestamp_next_try")
                    .HasMaxLength(45);

                entity.Property(e => e.TipoDa)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'2'");

                entity.Property(e => e.Token)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Vend1Prc).HasDefaultValueSql("'0'");

                entity.Property(e => e.Vend2Prc).HasDefaultValueSql("'0'");

                entity.Property(e => e.Visible)
                    .IsRequired()
                    .HasColumnName("visible")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.DateB)
                    .HasColumnName("DateB")
                    .HasColumnType("timestamp");
                entity.Property(e => e.timestamp_try)
                    .HasColumnName("timestamp_try")
                    .HasColumnType("timestamp");
                entity.Property(e => e.timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("timestamp");
            });

            modelBuilder.Entity<SapCashProducts>(entity =>
            {
                entity.ToTable("sap_cash_products");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.CashId)
                    .HasColumnName("cash_id")
                    .HasColumnType("int(11)");
                
                entity.Property(e => e.timestamp)
                    .HasColumnName("timestamp")
                    .HasColumnType("timestamp");
                
                entity.Property(e => e.Prezzo).HasColumnType("int(11)");

                entity.Property(e => e.Product)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Refund).HasColumnType("int(11)");

                entity.Property(e => e.Sales).HasColumnType("int(11)");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Test).HasColumnType("int(11)");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
