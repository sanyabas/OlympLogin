using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OlympLogin.Models
{
    public partial class kladrContext : DbContext
    {
        public virtual DbSet<Street> Street { get; set; }
        public virtual DbSet<Territory> Territory { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<Abbreviation> Abbreviation { get; set; }
        public virtual DbSet<Building> Building { get; set; }

        // Unable to generate entity type for table 'dbo.Name'. Please see the warning messages.

        public kladrContext(DbContextOptions options) : base(options)
        {
        }

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
//                optionsBuilder.UseSqlServer(@"Server=lenovo-g500;Database=kladr;Trusted_Connection=True;");
//            }
//        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Street>(entity =>
            {
                entity.HasKey(e => e.Code);

                entity.Property(e => e.Code)
                    .HasMaxLength(17)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Abbr)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Index)
                    .HasMaxLength(6)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasMaxLength(40)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Territory>(entity =>
            {
                entity.HasKey(e => e.Code);

                entity.Property(e => e.Code)
                    .HasMaxLength(13)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Abbreviation)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Index)
                    .HasMaxLength(6)
                    .IsUnicode(false);

                entity.Property(e => e.Name)
                    .HasMaxLength(40)
                    .IsUnicode(false);

                entity.Property(e => e.Status)
                    .HasMaxLength(1)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(100);

                entity.Property(e => e.LastName).HasMaxLength(100);

                entity.Property(e => e.MiddleName).HasMaxLength(100);

                entity.Property(e => e.StreetCode)
                    .IsRequired()
                    .HasMaxLength(17)
                    .IsUnicode(false);

                entity.Property(e => e.TerritoryCode)
                    .IsRequired()
                    .HasMaxLength(13)
                    .IsUnicode(false);

                entity.HasOne(d => d.StreetCodeNavigation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.StreetCode)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Users__StreetCod__5441852A");

                entity.HasOne(d => d.TerritoryCodeNavigation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.TerritoryCode)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Users__Territory__534D60F1");

                entity.Property(e => e.Login).HasMaxLength(50);

                entity.Property(e => e.Password).HasMaxLength(100);

                entity.Property(e => e.Address).HasMaxLength(200);

                entity.Property(e => e.Index).HasMaxLength(6);

                entity.Property(e => e.BuildingCode).HasMaxLength(25);

                entity.Property(e => e.Building).HasMaxLength(5);

                entity.Property(e => e.Flat).HasMaxLength(5);

            });

            modelBuilder.Entity<Region>(entity => { entity.HasKey(e => e.Code); });

            modelBuilder.Entity<Abbreviation>(entity =>
            {
                entity.HasKey(e => new {e.Level, e.ShortName});

                entity.Property(e => e.Level).HasMaxLength(5);

                entity.Property(e => e.ShortName).HasMaxLength(10);

                entity.Property(e => e.FullName).HasMaxLength(29);

                entity.Property(e => e.TypeCode).HasMaxLength(3);
            });

            modelBuilder.Entity<Building>(entity =>
            {
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Name).HasMaxLength(40);
                entity.Property(e => e.Code).HasMaxLength(19);
                entity.Property(e => e.Index).HasMaxLength(6);
            });
        }
    }
}
