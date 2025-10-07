using MySql.Data;

using System.Data.Entity;
using Yazlab1.Model;
using Yazlab1.Models; 

namespace Yazlab1.Data
{
 
    public class SinavTakvimDbContext : DbContext
    {
       
        public SinavTakvimDbContext() : base()
        {
           
        }

        
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Rol> Roller { get; set; }
        public DbSet<Bolum> Bolumler { get; set; }
       
    }
}