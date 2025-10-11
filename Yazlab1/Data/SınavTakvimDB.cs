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

        public DbSet<Derslik> Derslikler { get; set; }

        public DbSet<Ders> Dersler { get; set; }

        public DbSet<OgretimUyesi> OgretimUyeleri { get; set; }

        public DbSet<Ogrenci> Ogrenciler { get; set; } 
        public DbSet<OgrenciDersKayitlari> OgrenciDersKayitlari { get; set; } 



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

            
            modelBuilder.Entity<Bolum>().HasKey(b => b.BolumID);

            
            modelBuilder.Entity<Kullanici>().HasKey(k => k.KullaniciID);

            modelBuilder.Entity<Derslik>().HasKey(d => d.DerslikID);

            modelBuilder.Entity<Ders>().HasKey(d => d.DersID);

            modelBuilder.Entity<OgretimUyesi>().HasKey(o => o.OgretimUyesiID);

            modelBuilder.Entity<Ogrenci>().HasKey(o => o.OgrenciID); 

         
            modelBuilder.Entity<OgrenciDersKayitlari>().HasKey(ok => new { ok.OgrenciID, ok.DersID });


        }
    }
    
}
