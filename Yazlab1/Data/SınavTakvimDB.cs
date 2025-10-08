using Microsoft.EntityFrameworkCore;
using System;
using System.Configuration;
using Yazlab1.Model;
using Yazlab1.Models;

namespace Yazlab1.Data
{
    public class SinavTakvimDbContext : DbContext
    {
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Rol> Roller { get; set; }
        public DbSet<Bolum> Bolumler { get; set; }
      

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

          
            modelBuilder.Entity<Rol>().HasKey(r => r.RolID);

            // Bolum tablosunun birincil anahtarı BolumID'dir.
            modelBuilder.Entity<Bolum>().HasKey(b => b.BolumID);

            // Kullanici tablosunun birincil anahtarı KullaniciID'dir.
            modelBuilder.Entity<Kullanici>().HasKey(k => k.KullaniciID);
        }
    }
}