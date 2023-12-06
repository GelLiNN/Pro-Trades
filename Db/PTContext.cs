using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PT.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace PT.Data
{
    public class PTContext : DbContext
    {
        public DbSet<User> User { get; set; }
        public DbSet<UserType> UserType { get; set; }

        // Required for migrations to work
        public PTContext(DbContextOptions<PTContext> contextOptions) : base(contextOptions)
        {
            this.Database.EnsureCreated();
        }
    }

    [Table("users")]
    public class User : DbContext
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("UserId")]
        public int UserId { get; internal set; }

        [Column("UserName")]
        public string UserName { get; internal set; }

        [Column("UserEmail")]
        public string UserEmail { get; internal set; }

        [Column("UserTypeId")]
        public int UserTypeId { get; internal set; }
    }

    [Table("userTypes")]
    public class UserType : DbContext
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("UserTypeId")]
        public int UserTypeId { get; internal set; }

        [Column("UserTypeName")]
        public string UserTypeName { get; internal set; }
    }
}