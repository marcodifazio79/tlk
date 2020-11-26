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

        public virtual DbSet<CommandsMatch> CommandsMatch { get; set; }

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
            modelBuilder.Entity<CommandsMatch>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ModemCommand)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.WebCommand)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
